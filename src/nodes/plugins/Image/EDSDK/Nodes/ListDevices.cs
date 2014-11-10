#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;
using System.Collections.Generic;
using EDSDKLib;

#endregion usings

namespace VVVV.Nodes.EDSDKNodes
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "EDSDK", Help = "List connected Canon cameras using EDSDK", Tags = "Canon", Author = "elliotwoods/sebl", Credits = "", AutoEvaluate = false)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FPinInRefresh;

		[Output("Device")]
		ISpread<Camera> FPinOutDevices;

        [Output("Description")]
        ISpread<string> FPinOutDescription;

        [Output("Port Name")]
        ISpread<string> FPinOutOutPortName;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;


        SDKHandler CameraHandler;
        List<Camera> CamList;
		bool isInit = false;

		#endregion fields & pins

		[ImportingConstructor]
		public ListDevicesNode(IPluginHost host)
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (!isInit || FPinInRefresh[0])
			{
                if (CameraHandler != null) Dispose();
				init();
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
                FPinOutStatus[0] = "init : " + e.Message;
                return;
            }

            CameraHandler.CameraAdded += new SDKHandler.CameraAddedHandler(SDK_CameraAdded);
            CameraHandler.CameraHasShutdown += SDK_CameraHasShutdown;

            /*
             * unfortunately, there seems to be no event (in the canon dll) that monitors camera removal :(
             * SDK_CameraHasShutdown only triggers, if the session is already opened
             */

            RefreshCamera();

            isInit = true;
        }



        private void SDK_CameraAdded()
        {
            RefreshCamera();
        }

        private void RefreshCamera()
        {
            try
            {
                FPinOutDevices.SliceCount = 0;
                FPinOutDescription.SliceCount = 0;
                FPinOutOutPortName.SliceCount = 0;

                CloseSession();
                CamList = CameraHandler.GetCameraList();
                foreach (Camera cam in CamList)
                {
                    FPinOutDevices.Add(cam);
                    FPinOutDescription.Add(cam.Info.szDeviceDescription);
                    FPinOutOutPortName.Add(cam.Info.szPortName);
                }

                FPinOutStatus[0] = "OK";
            }
            catch (Exception e)
            {
                FPinOutStatus[0] = "Refresh : " + e.Message;
            }
        }

        private void SDK_CameraHasShutdown(object sender, EventArgs e)
        {
            RefreshCamera();
        }

        

        private void CloseSession()
        {
            CameraHandler.CloseSession();

            FPinOutDevices.SliceCount = 0;
            FPinOutDescription.SliceCount = 0;

            isInit = false;
        }

        public void Dispose()
        {
            CameraHandler.Dispose();
        }
        

	}
}