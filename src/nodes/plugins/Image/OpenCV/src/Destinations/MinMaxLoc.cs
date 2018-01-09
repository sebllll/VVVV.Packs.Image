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
	public class MinMaxLocInstance : IDestinationMaskInstance
	{
        public double Min
        {
            get
            {
                return retMin;
            }
        }

        public double Max
        {
            get
            {
                return retMax;
            }
        }

        public Vector2D MinLoc
        {
            get
            {
                return new Vector2D(retMinLoc.X, retMinLoc.Y);
            }
        }

        public Vector2D MaxLoc
        {
            get
            {
                return new Vector2D(retMaxLoc.X, retMaxLoc.Y);
            }
        }

 

        double retMin, retMax;
        Point retMinLoc, retMaxLoc;


		public override void Allocate()
		{
			
		}


		public override void Process()
		{
			if (!FInput.LockForReading())
				return;

            if (!FInputMask.LockForReading())
                return;

            CvInvoke.cvMinMaxLoc(FInput.CvMat, ref retMin, ref retMax, ref retMinLoc, ref retMaxLoc, FInputMask.CvMat);
            //CvInvoke.CvMinMaxIdx(FInput.CvMat, out retMin, out retMax, ref retMinLoc, ref retMaxLoc, IntPtr.Zero);
            //CvInvoke.cvAvgSdv(FInput.CvMat, ref FAverage, ref FStandardDeviation, IntPtr.Zero);

            FInput.ReleaseForReading();
            FInputMask.ReleaseForReading();
        }

	}

	#region PluginInfo
	[PluginInfo(Name = "MinMaxLoc", Category = "CV.Image", Help = "Returns the min, max and their location of the pixel values within an image.", Author = "sebl", Credits = "", Tags = "bounds")]
	#endregion PluginInfo
	public class MinMaxLocNode : IDestinationMaskNode<MinMaxLocInstance>
	{
        [Input("Input", Order = -1)]
        private ISpread<CVImageLink> FPinInInputMask;

        [Output("Min")]
		ISpread<double> FMin;

		[Output("Max")]
		ISpread<double> FMax;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
            FMin.SliceCount = InstanceCount;
			FMax.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
                FMin[i] = FProcessor[i].Min;
                FMax[i] = FProcessor[i].Max;
			}
		}
	}
}
