using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Canon.Eos.Framework;
using VVVV.CV.Core;
using VVVV.CV.Core;
using VVVV.PluginInterfaces.V2;

using System.Drawing;
using System.IO;

using EDSDKWrapper.Framework.Managers;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EDSDKWrapper.Framework.Objects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


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

        //EosCamera FCamera;
        bool FFirstRun = true;

        bool liveMode = false;

        public FrameworkManager FrameworkManager { get; set; }
        public Task LiveViewCapturingTask { get; set; }
        public Camera Camera { get; set; }


        //this.FrameworkManager = new FrameworkManager();

        //private ProcessDestination<AsTextureDX11Instance> FProcessor;

        //Dictionary<int, EosCamera> FListeningTo = new Dictionary<int, EosCamera>();
        //HashSet<int> FPictureTaken = new HashSet<int>();

        public void Evaluate(int SpreadMax)
		{

            if (FFirstRun)
            {
                this.FrameworkManager = new FrameworkManager();
                this.Camera = this.FrameworkManager.Cameras.First();
            }



			if (FInDevices.IsChanged || FInEnabled.IsChanged)
			{
                if (FInEnabled[0])
                {
                    if (!liveMode)
                    {
                        //AddListeners();
                        liveMode = true;
                        this.Camera.StartLiveView();
                    }

                    

                    while (Camera.LiveViewEnabled)
                    {
                        int exceptionCount = 0;
                        try
                        {
                            var stream = this.Camera.GetLiveViewImage();

                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = stream;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();

                            
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                FOutImage[0].Send(BitmapImage2Bitmap(bitmapImage));
                                //this.ImageSource = bitmapImage;
                            }));
                            

                            FOutImage[0].Send(BitmapImage2Bitmap(bitmapImage));

                            exceptionCount = 0;
                        }
                        catch
                        {
                            Thread.Sleep(100);
                            if (++exceptionCount > 10)
                            {
                                throw;
                            }
                        }
                    }


                }
                else // if not enabled
                {
                    this.Camera.StopLiveView();
                    liveMode = false;
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


        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
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
			if (FFirstRun)
			{
				FOutImage[0] = new CVImageLink();
				FFirstRun = false;
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
