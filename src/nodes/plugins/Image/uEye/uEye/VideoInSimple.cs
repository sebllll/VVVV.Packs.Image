using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using uEye;
using uEye.Defines;
using uEye.Types;
using System.Drawing;
using Emgu.CV;
using System.ComponentModel.Composition;
using System;
using System.IO;

namespace VVVV.Nodes.OpenCV.IDS
{
    #region PluginInfo
    [PluginInfo(Name = "VideoIn", Category = "uEye", Version = "Simple", Help = "Capture from camera devices", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo
    public class VideoInSimpleNode : IGeneratorNode<VideoInSimpleInstance>, IDisposable
    {
        #region fields and pins

        [Input("Camera Id")]
        IDiffSpread<int> FInCamId;

        [Input("Parameter File", StringType = StringType.Filename)]
        IDiffSpread<string> FInFInParameterFile;

        [Input("FPS", DefaultValue = 30, MinValue = 0, MaxValue = 1024)]
        IDiffSpread<int> FInFps;

        [Input("Exposure", DefaultValue = 0.1, MinValue = 0)]
        IDiffSpread<double> FInExposure;

        [Input("Gain", DefaultValue = 0.1, MinValue = 0, MaxValue = 1)]
        IDiffSpread<double> FInGain;

        [Input("Gamma", DefaultValue = 1.0, MinValue = 0.0, MaxValue = 10.0)]
        IDiffSpread<double> FInGamma;

        [Input("Color Temperature", DefaultValue = 2800, MinValue = 2600, MaxValue = 10000)]
        IDiffSpread<int> FInColorTemp;

        // ------------------------------------------------------------

        #endregion fields and pins



        protected override void Update(int InstanceCount, bool SpreadCountChanged)
        {
            if (SpreadCountChanged || FInCamId.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].CamId = FInCamId[i];
            }

            if (SpreadCountChanged || FInFInParameterFile.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].ParameterFile = FInFInParameterFile[i];
            }

            if (SpreadCountChanged || FInFps.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].Fps = FInFps[i];
            }

            if (SpreadCountChanged || FInExposure.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].Exposure = FInExposure[i];
            }

            if (SpreadCountChanged || FInGain.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].Gain = FInGain[i];
            }

            if (SpreadCountChanged || FInGamma.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].Gamma = FInGamma[i];
            }

            if (SpreadCountChanged || FInColorTemp.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].ColorTemp = FInColorTemp[i];
            }

        }

    }




    // ------------------------------------------------------------
    // ------------------------------------------------------------
    // ------------------------------------------------------------



    public class VideoInSimpleInstance : IGeneratorInstance
    {

        private int FCamId;
        public int CamId
        {
            set
            {
                FCamId = value;
                Restart();  // restart here, because new instance, camera and all?
            }
        }

        private string FParameterFile;
        public string ParameterFile
        {
            set
            {
                FParameterFile = value;
                LoadParameterFile();
                configureOutput();
            }
        }

        private double Ffps;
        public double Fps
        {
            set
            {
                Ffps = value;

                SetTimingParams();
            }
        }

        private double FExposure;
        public double Exposure
        {
            set
            {
                FExposure = value;

                SetTimingParams();
            }
        }

        private int FGain;
        public double Gain
        {
            set
            {
                FGain = (int)(value * 100); // range is 0 - 100
                Status = Camera.Gain.Hardware.Scaled.SetMaster(FGain).ToString(); 
            }
        }

        private int FGamma;
        public double Gamma
        {
            set
            {
                FGamma = (int)(value * 100); // range is 0 - 1000
                Status = Camera.Gamma.Software.Set(FGamma).ToString();
            }
        }

        private uint FColorTemp;
        public int ColorTemp
        {
            set
            {
                FColorTemp = (uint)value; // in KELVIN 2600 - 10000
                Status = Camera.Color.Temperature.Set(FColorTemp).ToString();
            }
        }


        private uEye.Camera Camera;

        private bool bLive = false;

        private bool frameAvailable = false;


        // ------------------------------------------------------------


        public override bool Open()
        {
            bool initSuccess = InitCamera();

            if (initSuccess)
            {
                LoadParameterFile();
                configureOutput();

                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Close()
        {
            Camera.Acquisition.Stop();
            //Camera.Memory.Free();
            Camera.Exit();
        }

        protected override void Generate()
        {
            if (frameAvailable)
            {
                Int32 s32MemID;
                Camera.Memory.GetActive(out s32MemID);

                //Camera.Memory.Lock(s32MemID);

                IntPtr memPtr;
                //Camera.Memory.ToIntPtr(out memPtr);
                Camera.Memory.ToIntPtr(s32MemID, out memPtr);
                Camera.Memory.CopyImageMem(memPtr, s32MemID, FOutput.Data);

                //Camera.Memory.Unlock(s32MemID);

                FOutput.Send();
            }
        }

        private bool InitCamera()
        {
            bLive = false;

            Camera = new uEye.Camera();

            uEye.Defines.Status statusRet = 0;

            // Open Camera
            statusRet = Camera.Init();
            if (statusRet != uEye.Defines.Status.Success)
            {
                Status = "Camera initializing failed";
                return false;
            }

            // Allocate Memory
            statusRet = Camera.Memory.Allocate();
            if (statusRet != uEye.Defines.Status.Success)
            {
                Status = "Allocate Memory failed";
                return false;
            }

            // Start Live Video
            statusRet = Camera.Acquisition.Capture();
            if (statusRet != uEye.Defines.Status.Success)
            {
                Status = "Start Live Video failed";
                return false;
            }
            else
            {
                bLive = true;

                Camera.EventFrame += onFrameEvent;
                Camera.EventDeviceRemove += onDeviceRemove;
                Camera.EventDeviceUnPlugged += onDeviceRemove;

                return true;
            }

            // Connect Event
            


            //Path.GetFullPath(FParameterFile);

            //Camera.EventAutoBrightnessFinished += onAutoShutterFinished;

            //CB_Auto_Gain_Balance.Enabled = Camera.AutoFeatures.Software.Gain.Supported;
            //CB_Auto_White_Balance.Enabled = Camera.AutoFeatures.Software.WhiteBalance.Supported;
        }

        // set some Parameters that depend on each other... like fps and exposure
        private void SetTimingParams()
        {
            Status = Camera.Timing.Framerate.Set(Ffps).ToString();
            Status = Camera.Timing.Exposure.Set(FExposure).ToString();
        }

        private void LoadParameterFile()
        {
            if (FParameterFile.Length > 2)
            {
                uEye.Defines.Status statusRet = 0;

                Camera.Acquisition.Stop();

                Int32[] memList;
                statusRet = Camera.Memory.GetList(out memList);
                if (statusRet != uEye.Defines.Status.Success)
                {
                    Status = "Get memory list failed: " + statusRet;
                    //Environment.Exit(-1);
                }

                statusRet = Camera.Memory.Free(memList);
                if (statusRet != uEye.Defines.Status.Success)
                {
                    Status = "Free memory list failed: " + statusRet;
                    //Environment.Exit(-1);
                }

                //statusRet = Camera.Parameter.Load("C:\\dev\\dhmd_Vogel_x64\\Assets\\ueye50p.ini");
                statusRet = Camera.Parameter.Load(FParameterFile);
                if (statusRet != uEye.Defines.Status.Success)
                {
                    Status = "Loading parameter failed: " + statusRet;
                }

                statusRet = Camera.Memory.Allocate();
                if (statusRet != uEye.Defines.Status.SUCCESS)
                {
                    Status = "Allocate Memory failed";
                    //Environment.Exit(-1);
                }

                if (bLive == true)
                {
                    Camera.Acquisition.Capture();
                }
            }
        }

        private void configureOutput()
        {
            uEye.Defines.Status statusRet = 0;

            uEye.Defines.ColorMode pixFormat;
            statusRet = Camera.PixelFormat.Get(out pixFormat);

            TColorFormat format = GetColor(pixFormat);

            Rectangle a;
            statusRet = Camera.Size.AOI.Get(out a);

            FOutput.Image.Initialise(a.Width, a.Height, format);
        }

        private void onFrameEvent(object sender, EventArgs e)
        {
            frameAvailable = true;

            // --> everything happens in Generate()
        }

        private void onDeviceRemove(object sender, EventArgs e)
        {
            Camera.Exit();
        }

        private TColorFormat GetColor(uEye.Defines.ColorMode color)
        {
            switch (color)
            {
                case uEye.Defines.ColorMode.Mono8:
                    return TColorFormat.L8;

                case uEye.Defines.ColorMode.Mono16:
                    return TColorFormat.L16;

                case uEye.Defines.ColorMode.RGB8Packed:
                case uEye.Defines.ColorMode.RGB8Planar:
                case uEye.Defines.ColorMode.BGR8Packed:
                    return TColorFormat.RGB8;

                case uEye.Defines.ColorMode.RGBA8Packed:
                case uEye.Defines.ColorMode.BGRA8Packed:
                    return TColorFormat.RGBA8;

                default:
                    throw (new Exception("Color mode unsupported"));
            }
        }

        // TODO:
        //private void Button_Live_Video_Click(object sender, EventArgs e)
        //{
        //    // Open Camera and Start Live Video
        //    if (uEye.Camera.Acquisition.Capture() == uEye.Defines.Status.Success)
        //    {
        //        bLive = true;
        //    }
        //}

        //private void Button_Stop_Video_Click(object sender, EventArgs e)
        //{
        //    // Stop Live Video
        //    if (uEye.Camera.Acquisition.Stop() == uEye.Defines.Status.Success)
        //    {
        //        bLive = false;
        //    }
        //}

    }
}
