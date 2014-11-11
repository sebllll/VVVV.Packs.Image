#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;
using System.Collections.Generic;
using EDSDKLib;

#endregion usings

namespace VVVV.Nodes.EDSDKNodes
{
	#region PluginInfo
	[PluginInfo(Name = "Camera", Category = "EDSDK", Help = "List connected Canon cameras using EDSDK", Tags = "Canon", Author = "elliotwoods/sebl", Credits = "", AutoEvaluate = false)]
	#endregion PluginInfo
	public class CameraCanon : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FInRefresh;

        [Input("Camera ID", IsSingle = true)]
        IDiffSpread<int> FInID;

        //[Input("Save On Camera")]
        //IDiffSpread<bool> FInSaveOnCamera;

        [Input("Save On Computer")]
        IDiffSpread<bool> FInSaveOnComputer;

        [Input("Save Location", StringType = StringType.Directory)]
        IDiffSpread<string> FInSaveLocation;

        [Input("Shoot", IsBang = true)]
        ISpread<bool> FInShoot;

        [Output("Description")]
        ISpread<string> FOutDescription;

        [Output("Port Name")]
        ISpread<string> FOutPortName;

        [Output("Progress")]
        ISpread<double> FOutProgress;

		[Output("Status")]
		ISpread<string> FOutStatus;

		[Import]
		ILogger FLogger;

        private SDKHandler CameraHandler;
        private List<Camera> CamList;
        private Camera activeCam;
        private List<int> AvList;
        private List<int> TvList;
        private List<int> ISOList;
        private int WhiteBalance;
        private bool isInit = false;
        
		#endregion fields & pins


		[ImportingConstructor]
        public CameraCanon(IPluginHost host)
		{
            // why is that needed?
		}


		public void Evaluate(int SpreadMax)
		{
			if (!isInit || FInRefresh[0])
			{
                if (CameraHandler != null) Dispose();
				init();
			}

            if (isInit)
            {
                if (FInID.IsChanged)
                {
                    activeCam = CamList[FInID[0] % CamList.Count];
                    OpenSession(activeCam);
                }

                if (FInSaveLocation.IsChanged)
                {
                    if (!Directory.Exists(FInSaveLocation[0]) )
                    {
                        Directory.CreateDirectory(FInSaveLocation[0]);
                    }
                }

                if (FInShoot.IsChanged)
                {
                    if (FInShoot[0])
                    {
                        try
                        {
                            if (FInSaveOnComputer[0]) CameraHandler.ImageSaveDirectory = FInSaveLocation[0];
                            CameraHandler.TakePhoto();
  
                            //if (FInSaveOnCamera[i] && FInSaveOnComputer[i])
                            //    camera.SavePicturesToHostAndCamera(FInSaveLocation[i]);
                            //else if (FInSaveOnCamera[i])
                            //    camera.SavePicturesToCamera();
                            //else if (FInSaveOnComputer[i])
                            //    camera.SavePicturesToHost(FInSaveLocation[i]);
                            //else if (FInSaveOnCamera.IsChanged || FInSaveOnComputer.IsChanged && FFirstRun)
                            //    throw(new Exception("Canon.Eos.Framework requires to you turn on Save To Camera or Save To Computer. But you can actually turn it off again afterwards and ReceivePhoto will continue to work."));

                            FOutStatus[0] = "OK";
                        }
                        catch (Exception e)
                        {
                            FOutStatus[0] = e.Message;
                        }
                    }

                }
            }
		}


        private void init()
        {
            try
            {
                if (CameraHandler != null) CloseSession();
                CameraHandler = new SDKHandler();
            }
            catch (Exception e)
            {
                FOutStatus[0] = "init : " + e.Message;
                return;
            }

            RefreshCamera();

            isInit = true;
        }


        private void SDK_CameraAdded()
        {
            RefreshCamera();
        }

        private void RefreshCamera()
        {
            FLogger.Log(LogType.Debug, "RefreshCamera()");
            try
            {
                FOutDescription.SliceCount = 0;
                FOutPortName.SliceCount = 0;

                CloseSession();
                CamList = CameraHandler.GetCameraList();
                foreach (Camera cam in CamList)
                {
                    FOutDescription.Add(cam.Info.szDeviceDescription);
                    FOutPortName.Add(cam.Info.szPortName);
                }

                FOutStatus[0] = "OK";
            }
            catch (Exception e)
            {
                FOutStatus[0] = "Refresh : " + e.Message;
            }
        } //RefreshCamera()

        private void SDK_CameraHasShutdown(object sender, EventArgs e)
        {
            RefreshCamera();
        }

        #region Settings
        private void SetAperture()
        {
            string sv = CameraValues.AV(( uint )AvList[0]);
            CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_Av, CameraValues.AV(sv));
        }

        private void SetTime()
        {
            string tv = CameraValues.TV(( uint )TvList[0]);
            CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_Tv, CameraValues.TV(tv));
            
            //if (( string )EDSDKLib.TvCoBox.SelectedItem == "Bulb")
            //{
            //    BulbBox.IsEnabled = true;
            //    BulbSlider.IsEnabled = true;
            //}
            //else
            //{
            //    BulbBox.IsEnabled = false;
            //    BulbSlider.IsEnabled = false;
            //}
        }

        private void SetIso()
        {
            string iso = CameraValues.ISO(( uint )ISOList[0]);
            CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_ISOSpeed, CameraValues.ISO(( string )iso));

        }

        private void SetWhitebalance(int index = 0)
        {
            switch (index)
            {
                case 0: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_Auto); break;
                case 1: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_Daylight); break;
                case 2: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_Cloudy); break;
                case 3: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_Tangsten); break;
                case 4: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_Fluorescent); break;
                case 5: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_Strobe); break;
                case 6: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_WhitePaper); break;
                case 7: CameraHandler.SetSetting(EDSDKLib.EDSDK.PropID_WhiteBalance, EDSDKLib.EDSDK.WhiteBalance_Shade); break;
            }
        }
        #endregion Settings

        #region Session
        private void OpenSession(Camera cam)
        {
            FLogger.Log(LogType.Debug, "OpenSession()");
            CameraHandler.OpenSession(cam);

            CameraHandler.CameraAdded += new SDKHandler.CameraAddedHandler(SDK_CameraAdded);
            CameraHandler.ProgressChanged += new SDKHandler.ProgressHandler(SDK_ProgressChanged);
            CameraHandler.CameraHasShutdown += SDK_CameraHasShutdown;

            /*
             * unfortunately, there seems to be no event (in the canon dll) that monitors camera removal :(
             * SDK_CameraHasShutdown only triggers, if the session is already opened
             */


            if (CameraHandler.GetSetting(EDSDKLib.EDSDK.PropID_AEMode) != EDSDKLib.EDSDK.AEMode_Manual)
            {
                FLogger.Log(LogType.Message, "Camera is not in manual mode. Some features might not work!");
            }
            AvList = CameraHandler.GetSettingsList(( uint )EDSDKLib.EDSDK.PropID_Av);
            TvList = CameraHandler.GetSettingsList(( uint )EDSDKLib.EDSDK.PropID_Tv);
            ISOList = CameraHandler.GetSettingsList(( uint )EDSDKLib.EDSDK.PropID_ISOSpeed);


            //foreach (int Av in AvList) AvCoBox.Items.Add(CameraValues.AV(( uint )Av));
            //foreach (int Tv in TvList) TvCoBox.Items.Add(CameraValues.TV(( uint )Tv));
            //foreach (int ISO in ISOList) ISOCoBox.Items.Add(CameraValues.ISO(( uint )ISO));
            


            //select current enum

            /*
            original code from sample was:
            
            AvCoBox.SelectedIndex = AvCoBox.Items.IndexOf(CameraValues.AV(( uint )CameraHandler.GetSetting(( uint )EDSDKLib.EDSDK.PropID_Av)));
            TvCoBox.SelectedIndex = TvCoBox.Items.IndexOf(CameraValues.TV(( uint )CameraHandler.GetSetting(( uint )EDSDKLib.EDSDK.PropID_Tv)));
            ISOCoBox.SelectedIndex = ISOCoBox.Items.IndexOf(CameraValues.ISO(( uint )CameraHandler.GetSetting(( uint )EDSDKLib.EDSDK.PropID_ISOSpeed)));
            */

            int wbidx = ( int )CameraHandler.GetSetting(( uint )EDSDKLib.EDSDK.PropID_WhiteBalance);
            switch (wbidx)
            {
                case EDSDKLib.EDSDK.WhiteBalance_Auto: WhiteBalance = 0; break;
                case EDSDKLib.EDSDK.WhiteBalance_Daylight: WhiteBalance= 1; break;
                case EDSDKLib.EDSDK.WhiteBalance_Cloudy: WhiteBalance = 2; break;
                case EDSDKLib.EDSDK.WhiteBalance_Tangsten: WhiteBalance = 3; break;
                case EDSDKLib.EDSDK.WhiteBalance_Fluorescent: WhiteBalance = 4; break;
                case EDSDKLib.EDSDK.WhiteBalance_Strobe: WhiteBalance = 5; break;
                case EDSDKLib.EDSDK.WhiteBalance_WhitePaper: WhiteBalance = 6; break;
                case EDSDKLib.EDSDK.WhiteBalance_Shade: WhiteBalance = 7; break;
                default: WhiteBalance = -1; break;
            }
            

        } // OpenSession()

        private void CloseSession()
        {
            FLogger.Log(LogType.Debug, "CloseSession()");
            if (CameraHandler.CameraSessionOpen)
            {
                FLogger.Log(LogType.Debug, "closing already opened session");
                CameraHandler.CloseSession();
            }

            FOutDescription.SliceCount = 0;

            isInit = false;
        }
        #endregion Session

        private void SDK_ProgressChanged(int Progress)
        {
            if (Progress == 100) Progress = 0;
            FOutProgress[0] = Progress / 100;  // can that ever be spreadable? how to know which eventhandler fired? need a list of handlers?
        }

        public void Dispose()
        {
            FLogger.Log(LogType.Debug, "Dispose()");
            CameraHandler.Dispose();
        }
        

	}
}