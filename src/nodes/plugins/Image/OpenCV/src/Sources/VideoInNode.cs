#region usings

using System;
using System.ComponentModel.Composition;
using Emgu.CV.CvEnum;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using VVVV.CV.Core;

#endregion usings

namespace VVVV.CV.Nodes
{
	public class CaptureVideoInstance : IGeneratorInstance
	{
		private int FRequestedWidth = 0;
		private int FRequestedHeight = 0;

		Capture FCapture;
        Dictionary<CAP_PROP, float> FPropertySet;

        private int FDeviceID = 0;
		public int DeviceID
		{
			get
			{
				return FDeviceID;
			}
			set
			{
				FDeviceID = value;
				Restart();
			}
		}

		private int FWidth = 640;
		public int Width
		{
			get
			{
				return FWidth;
			}
			set
			{
				FWidth = value;
				Restart();
			}
		}

		private int FHeight = 480;
		public int Height
		{
			get
			{
				return FHeight;
			}
			set
			{
				FHeight = value;
				Restart();
			}
		}

		private int FFramerate = 30;
		public int Framerate
		{
			get
			{
				return FFramerate;
			}
			set
			{
				FFramerate = value;
				Restart();
			}
		}

        public override bool Open()
		{
			Close();

			try
			{
				FCapture = new Capture(FDeviceID);
				FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, FWidth);
				FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, FHeight);
				FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FPS, FFramerate);

				Status = "OK";
				return true;
			}
			catch (Exception e)
			{
				Status = e.Message;
				return false;
			}
		}

        public override void Close()
		{
            if (FCapture == null)
                return;

			try
			{
				FCapture.Dispose();
				Status = "Closed";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
		}

        public override void Allocate()
        {
            FOutput.Image.Initialise(new Size(FCapture.Width, FCapture.Height), TColorFormat.RGB8);
        }

		protected override void Generate()
		{
			IImage capbuffer = FCapture.QueryFrame();
			if (ImageUtils.IsIntialised(capbuffer))
			{
				FOutput.Image.SetImage(capbuffer);
				FOutput.Send();
			}
		}

        public void SetProperties(Dictionary<CAP_PROP, float> PropertySet)
        {
            if (PropertySet == null)
                return;

            FPropertySet = PropertySet;

            foreach (var property in PropertySet)
            {
                FCapture.SetCaptureProperty(property.Key, property.Value);
            }
        }

    }

	#region PluginInfo
	[PluginInfo(Name = "VideoIn",
			  Category = "CV.Image",
			  Version = "VfW",
			  Help = "Captures from DShow device to IPLImage",
			  Tags = "source")]
	#endregion PluginInfo
	public class CaptureVideoNode : IGeneratorNode<CaptureVideoInstance>
	{
		#region fields & pins
		[Input("Device ID", MinValue = 0)]
		IDiffSpread<int> FPinInDeviceID;

		[Input("Width", MinValue = 32, MaxValue = 8192, DefaultValue = 640)]
		IDiffSpread<int> FPinInWidth;

		[Input("Height", MinValue = 32, MaxValue = 8192, DefaultValue = 480)]
		IDiffSpread<int> FPinInHeight;

        [Input("Properties")]
        IDiffSpread<Dictionary<CAP_PROP, float>> FPinInProperties;

        [Import]
		ILogger FLogger;

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public CaptureVideoNode(IPluginHost host)
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInDeviceID.IsChanged || SpreadChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].DeviceID = FPinInDeviceID[i];

            if (FPinInWidth.IsChanged || SpreadChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Width = FPinInWidth[i];

            if (FPinInHeight.IsChanged || SpreadChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Height = FPinInHeight[i];

            if (FPinInProperties.IsChanged || SpreadChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].SetProperties(FPinInProperties[i]);
            }
        }
	}

    #region PluginInfo
    [PluginInfo(Name = "CaptureProperty", Category = "CV.Image", Version = "VfW", Help = "Set properties for VfW video", Tags = "", Author = "elliotwoods", AutoEvaluate = true)]
    #endregion PluginInfo
    public class CapturePropertyNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Property")]
        IDiffSpread<CAP_PROP> FPinInProperty;

        [Input("Value", MinValue = 0.0, MaxValue = 1.0)]
        IDiffSpread<float> FPinInValue;

        [Output("PropertyPair", IsSingle = true)]
        ISpread<Dictionary<CAP_PROP, float>> FPinOutOutput;

        [Import]
        ILogger FLogger;

        Dictionary<CAP_PROP, float> FOutput = new Dictionary<CAP_PROP, float>();

        #endregion fields & pins

        [ImportingConstructor]
        public CapturePropertyNode(IPluginHost host)
        {

        }

        bool FFirstRun = true;
        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FPinInProperty.IsChanged || FPinInValue.IsChanged)
            {
                FOutput.Clear();
                for (int i = 0; i < SpreadMax; i++)
                {
                    if (!FOutput.ContainsKey(FPinInProperty[i]))
                        FOutput.Add(FPinInProperty[i], FPinInValue[i]);
                }
                FPinOutOutput[0] = FOutput;
            }
        }
    }
}
