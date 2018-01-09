#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;
using VVVV.CV.Core;

#endregion

namespace VVVV.CV.Nodes
{
	public class SplitInstance : IDestinationInstance
	{
        public CVImage Red { get { return R; } }
        public CVImage Green { get { return G; } }
        public CVImage Blue { get { return B; } }

        private CVImage R = new CVImage();
        private CVImage G = new CVImage();
        private CVImage B = new CVImage();

        public override void Allocate()
		{
            R.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
            G.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
            B.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
        }

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;

            CvInvoke.cvSplit(FInput.CvMat, R.CvMat, G.CvMat, B.CvMat, IntPtr.Zero);

            FInput.ReleaseForReading();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Split", Category = "CV.Image", Help = "Splits an RGB image into its channels", Author = "sebl", Credits = "", Tags = "mean, standard deviation")]
    #endregion PluginInfo
    public class SplitNode : VVVV.CV.Core.INode, IDisposable
    {
        #region fields & pins
        [Config("Thread mode")]
        IDiffSpread<ThreadMode> FConfigThreadMode;


        [Input("Input", Order = -1)]
        private ISpread<CVImageLink> FPinInInputImage;


        [Output("Output R")]
        ISpread<CVImageLink> FPinOutputR;

        [Output("Output G")]
        ISpread<CVImageLink> FPinOutputG;

        [Output("Output B")]
        ISpread<CVImageLink> FPinOutputB;


        protected ProcessDestination<SplitInstance> FProcessor;

        CVImageOutput FOutputR = new CVImageOutput();
        CVImageOutput FOutputG = new CVImageOutput();
        CVImageOutput FOutputB = new CVImageOutput();

        bool FFirstRun = true;


        #endregion fields&pins

        protected override bool OneInstancePerImage()
        {
            return true;
        }

        public override void Evaluate(int SpreadMax)
        {
            if (FProcessor == null)
                FProcessor = new ProcessDestination<SplitInstance>(FPinInInputImage);

            if (FConfigThreadMode.IsChanged)
                FProcessor.ThreadMode = FConfigThreadMode[0];

            bool countChanged = FProcessor.CheckInputSize(this.OneInstancePerImage() ? FPinInInputImage.SliceCount : SpreadMax);

            if (FFirstRun)
            {
                FPinOutputR[0] = FOutputR.Link;
                FPinOutputG[0] = FOutputG.Link;
                FPinOutputB[0] = FOutputB.Link;

                FFirstRun = false;
            }


            //for (int i = 0; i < FProcessor.SliceCount; i++)
            //{
            //    FOutputR.Image = FProcessor[i].Red;
            //}


            //FOutputR.Send();
            //FOutputG.Send();
            //FOutputB.Send();

            Update(FProcessor.SliceCount, countChanged);
        }


        protected override void Update(int InstanceCount, bool SpreadChanged)
        {

            for (int i = 0; i < InstanceCount; i++)
            {
                FOutputR.Image = FProcessor[i].Red;
                FOutputG.Image = FProcessor[i].Green;
                FOutputB.Image = FProcessor[i].Blue;
            }

            FOutputR.Send();
            FOutputG.Send();
            FOutputB.Send();
        }


        public void Dispose()
        {
            FOutputR.Dispose();

            if (FProcessor != null)
                FProcessor.Dispose();
        }
    }
}
