using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Canon.Eos.Framework;
using VVVV.CV.Core;
using VVVV.CV.Core;
using VVVV.PluginInterfaces.V2;


namespace VVVV.Nodes.EDSDK
{
	#region PluginInfo
	[PluginInfo(Name = "LiveView", Category = "EDSDK", Help = "Bring in a live view stream from the camera", Tags = "Canon", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion Plugin
    public class LiveViewNode : IPluginEvaluate, IDisposable
	{
		[Input("Device", IsSingle=true)]
        IDiffSpread<EosCamera> FInDevices;

		[Input("Enabled", IsSingle = true)]
		IDiffSpread<bool> FInEnabled;

		[Output("Output")]
		ISpread<CVImageLink> FOutImage;

		[Output("Status")]
		ISpread<string> FOutStatus;

        [Output("On Receive", IsBang=true)]
		ISpread<bool> FOutOnReceive;

		EosCamera FCamera;
		bool FFirstRun = true;

        bool liveMode = false;

        //private ProcessDestination<AsTextureDX11Instance> FProcessor;

        Dictionary<int, EosCamera> FListeningTo = new Dictionary<int, EosCamera>();
        HashSet<int> FPictureTaken = new HashSet<int>();

        public void Evaluate(int SpreadMax)
		{
			if (FInDevices.IsChanged || FInEnabled.IsChanged)
			{
                if (FInEnabled[0] && !liveMode)
                {
                    AddListeners();
                    liveMode = true;
                }
			}

            FOutOnReceive.SliceCount = SpreadMax;
			for (int i = 0; i < SpreadMax; i++)
			{
				if (FPictureTaken.Contains(i))
					FOutOnReceive[i] = true;
				else
					FOutOnReceive[i] = false;
			}
			FPictureTaken.Clear();
		}

        void AddListeners()
		{
			RemoveListeners();

			int count = FInDevices.SliceCount;
			SetupOutput(count);

			for (int i = 0; i < count; i++)
			{
				var camera = FInDevices[i];
				if (camera == null)
					continue;

                FListeningTo.Add(i, camera);
                camera.LiveViewUpdate += FCamera_LiveViewUpdate;

                //if (!camera.IsInHostLiveViewMode)
                //{
                    camera.StartLiveView();
                //}
				
			}
		}



        void RemoveListeners()
		{
			foreach (var camera in FListeningTo)
			{
				camera.Value.LiveViewUpdate -= FCamera_LiveViewUpdate;

                if (!camera.Value.IsInHostLiveViewMode)
                {
                    camera.Value.StopLiveView();
                }
			}

			FListeningTo.Clear();
		}


        /*
         *  NOTE:
         *  
         * getBitmap() is not available in EosLiveImageEventArgs. ssems we need aproper c# wrapper that exposes something like:
         *
         * err = EDSDK.EdsCreateEvfImageRef(stream, out EvfImageRef);
         * if (err == EDSDK.EDS_ERR_OK) err = EDSDK.EdsDownloadEvfImage(MainCamera.Ref, EvfImageRef);
         * if (err == EDSDK.EDS_ERR_OBJECT_NOTREADY) continue;
         * else Error = err;
         * 
         */




        //void FCamera_LiveViewUpdate(object sender, Canon.Eos.Framework.Eventing.EosImageEventArgs e)
        void FCamera_LiveViewUpdate(object sender, Canon.Eos.Framework.Eventing.EosLiveImageEventArgs e)
		{
			var camera = sender as EosCamera;
			if (camera == null)
				return;

			if (FListeningTo.ContainsValue(camera))
			{
				foreach (var key in FListeningTo.Keys)
				{
					if (FListeningTo[key] == camera)
					{
						FPictureTaken.Add(key);
                        var bitmap = e.GetBitmap();   // not available >> exception in Canon.Eos.framework.dll  
						FOutImage[key].Send(bitmap);
						bitmap.Dispose();
					}
				}
			}
		}


        void SetupOutput(int count)
		{
			FOutImage.SliceCount = count;
			for (int i = 0; i < count; i++)
			{
				FOutImage[i] = new CVImageLink();
			}
		}

		public void Dispose()
		{
			RemoveListeners();
		}


        /*
		public void Evaluate(int SpreadMax)
		{
			if (isInit)
			{
				FOutImage[0] = new CVImageLink();
				isInit = false;
			}

			if (FInDevices[0] != FCamera || FInEnabled.IsChanged)
			{
				try
				{
                    //throw (new Exception("Actually, this node doesn't work yet, sorry"));
                    //if (FCamera != null)
                    //{
                    //    Disconnect();
                    //}
					if (FInEnabled[0])
					{
						FCamera = FInDevices[0];
						if (FCamera != null)
						{
                            //if (!FCamera.IsInLiveViewMode)
                                Connect();
                            
						}
					}
                    else
                    {
                        Disconnect();

                    }
					FOutStatus[0] = "OK";
				}
				catch (Exception e)
				{
					FOutStatus[0] = e.Message;
				}
			}
		}


		void Connect()
		{

                
                if (!liveMode)
                {
                    FCamera.LiveViewUpdate += FCamera_LiveViewUpdate;
                    FCamera.LiveViewDevice = EosLiveViewDevice.Host;

                    FCamera.StartLiveView();
                    liveMode = true;
                }

		}

		void FCamera_LiveViewUpdate(object sender, Canon.Eos.Framework.Eventing.EosLiveImageEventArgs e)
		{

            var bitmap = e.GetBitmap();
            FOutImage[0].Send(bitmap);
            bitmap.Dispose();


		}

		void Disconnect()
		{
            //if (FCamera != null)
            //{
            //FCamera.LiveViewUpdate -= FCamera_LiveViewUpdate;
            FCamera.StopLiveView();
            liveMode = false;
            //}
		}*/
    }
}
