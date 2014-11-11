using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using EDSDKLib;
using System.IO;

namespace VVVV.Nodes.EDSDK.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Shoot", Category = "EDSDK", Help = "Takes a photo using a Canon camera", Tags = "Canon", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ShootNode : IPluginEvaluate, IDisposable
    {
        #region fields & pins
        [Input("Device")]
		IDiffSpread<Camera> FInCamera;

		[Input("Save On Camera")]
		IDiffSpread<bool> FInSaveOnCamera;

		[Input("Save On Computer")]
		IDiffSpread<bool> FInSaveOnComputer;

		[Input("Save Location", StringType=StringType.Directory)]
		IDiffSpread<string> FInSaveLocation;

		[Input("Shoot", IsBang=true)]
		ISpread<bool> FInShoot;

        [Output("Progress")]
        ISpread<double> FOutProgress;

		[Output("Status")]
		ISpread<string> FOutStatus;

        List<SDKHandler> CameraHandlers;
        List<int> AvList;
        List<int> TvList;
        List<int> ISOList;
        List<bool> isInit;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutStatus.SliceCount = SpreadMax;
            FOutProgress.SliceCount = SpreadMax;

            if (FInCamera.IsChanged)
            {
                if (CameraHandlers != null)
                {
                    foreach (SDKHandler hdl in CameraHandlers)
                    {
                        CloseSession(hdl);
                        hdl.Dispose();
                    }
                    CameraHandlers.Clear();
                }

                

                for (int i = 0; i < SpreadMax; i++)
                {
                    FOutProgress[i] = 0;

                    try
                    {
                        CameraHandlers.Add(new SDKHandler());
                        CameraHandlers[i].OpenSession(FInCamera[i]);
                        CameraHandlers[i].ProgressChanged += new SDKHandler.ProgressHandler(SDK_ProgressChanged);
                        CameraHandlers[i].CameraHasShutdown += SDK_CameraHasShutdown;

                        
                    }
                    catch (Exception e)
                    {
                        FOutStatus[i] = "init : " + e.Message;
                        return;
                    }
                }

                if (FInSaveOnCamera.IsChanged || FInSaveOnComputer.IsChanged || FInSaveLocation.IsChanged || FInShoot.IsChanged)
                {

                    for (int i = 0; i < SpreadMax; i++)
                    {
                        try
                        {

                            if (FInSaveOnComputer[i] && FInShoot[i])
                            {
                                Directory.CreateDirectory(FInSaveLocation[i]);
                                CameraHandlers[i].ImageSaveDirectory = FInSaveLocation[i];
                            }

                            if (FInShoot[i]) CameraHandlers[i].TakePhoto();

                            //if (FInSaveOnCamera[i] && FInSaveOnComputer[i])
                            //    camera.SavePicturesToHostAndCamera(FInSaveLocation[i]);
                            //else if (FInSaveOnCamera[i])
                            //    camera.SavePicturesToCamera();
                            //else if (FInSaveOnComputer[i])
                            //    camera.SavePicturesToHost(FInSaveLocation[i]);
                            //else if (FInSaveOnCamera.IsChanged || FInSaveOnComputer.IsChanged && FFirstRun)
                            //    throw(new Exception("Canon.Eos.Framework requires to you turn on Save To Camera or Save To Computer. But you can actually turn it off again afterwards and ReceivePhoto will continue to work."));

                            FOutStatus[i] = "OK";
                        }
                        catch (Exception e)
                        {
                            FOutStatus[i] = e.Message;
                        }
                    }
                }

            }
        }

        /*
        private void OpenSession()
        {

            CameraHandler.OpenSession(CamList[0]);   // not spreadable yet :)


            if (CameraHandler.GetSetting(EDSDKLib.EDSDK.PropID_AEMode) != EDSDKLib.EDSDK.AEMode_Manual) FLogger.Log(LogType.Message, "Camera is not in manual mode. Some features might not work!");
            AvList = CameraHandler.GetSettingsList(( uint )EDSDKLib.EDSDK.PropID_Av);
            TvList = CameraHandler.GetSettingsList(( uint )EDSDKLib.EDSDK.PropID_Tv);
            ISOList = CameraHandler.GetSettingsList(( uint )EDSDKLib.EDSDK.PropID_ISOSpeed);


            // transform available setting to enum

            
            original code from sample was:
            
            //foreach (int Av in AvList) AvCoBox.Items.Add(CameraValues.AV(( uint )Av));
            //foreach (int Tv in TvList) TvCoBox.Items.Add(CameraValues.TV(( uint )Tv));
            //foreach (int ISO in ISOList) ISOCoBox.Items.Add(CameraValues.ISO(( uint )ISO));
            


            //select current enum

            
            original code from sample was:
            
            //AvCoBox.SelectedIndex = AvCoBox.Items.IndexOf(CameraValues.AV(( uint )CameraHandler.GetSetting(( uint )EDSDKLib.EDSDK.PropID_Av)));
            //TvCoBox.SelectedIndex = TvCoBox.Items.IndexOf(CameraValues.TV(( uint )CameraHandler.GetSetting(( uint )EDSDKLib.EDSDK.PropID_Tv)));
            //ISOCoBox.SelectedIndex = ISOCoBox.Items.IndexOf(CameraValues.ISO(( uint )CameraHandler.GetSetting(( uint )EDSDKLib.EDSDK.PropID_ISOSpeed)));
            


            // same with the whitebalance

            
            original code from sample was:
            
            int wbidx = ( int )CameraHandler.GetSetting(( uint )EDSDK.PropID_WhiteBalance);
            switch (wbidx)
            {
                case EDSDK.WhiteBalance_Auto: WBCoBox.SelectedIndex = 0; break;
                case EDSDK.WhiteBalance_Daylight: WBCoBox.SelectedIndex = 1; break;
                case EDSDK.WhiteBalance_Cloudy: WBCoBox.SelectedIndex = 2; break;
                case EDSDK.WhiteBalance_Tangsten: WBCoBox.SelectedIndex = 3; break;
                case EDSDK.WhiteBalance_Fluorescent: WBCoBox.SelectedIndex = 4; break;
                case EDSDK.WhiteBalance_Strobe: WBCoBox.SelectedIndex = 5; break;
                case EDSDK.WhiteBalance_WhitePaper: WBCoBox.SelectedIndex = 6; break;
                case EDSDK.WhiteBalance_Shade: WBCoBox.SelectedIndex = 7; break;
                default: WBCoBox.SelectedIndex = -1; break;
            }
            SettingsGroupBox.IsEnabled = true;
            LVGroupBox.IsEnabled = true;
            

        }
        */
    
        private void SDK_ProgressChanged(int Progress)
        {
            if (Progress == 100) Progress = 0;
            FOutProgress[0] = Progress / 100;  // can that ever be spreadable? how to know which eventhandler fired? need a list of handlers?
        }

        private void SDK_CameraHasShutdown(object sender, EventArgs e)
        {
            CloseSession(sender as SDKHandler); // wild guess
        }

        private void CloseSession(SDKHandler handler)
        {
            handler.CloseSession();
        }

        public void Dispose()
        {
            foreach (SDKHandler hdl in CameraHandlers)
            { 
                CloseSession(hdl);
                hdl.Dispose();
            }
        }


	}
}
