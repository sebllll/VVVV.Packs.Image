#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;
using System.Linq;
using VVVV.CV.Core;

#endregion

namespace VVVV.CV.Nodes
{
	[FilterInstance("Rotate", Help = "Rotate an image in 1/4 cycle increments.", Author = "elliotwoods")]
    public class RotateStepInstance : IFilterInstance
    {
		[Input("Rotations")]
		public int Rotations = 0;

        public override void Allocate()
        {
            //we presume that the output is allocated during process
        }

        public override void Process()
        {
            //we have an integer number of steps
            int anticlockwiseSteps = VVVV.Utils.VMath.VMath.Zmod(Rotations, 4);

            bool transpose = anticlockwiseSteps % 2 == 1;
            Size outputSize = transpose ? new Size(FInput.Image.Size.Height, FInput.Image.Size.Width) : FInput.Image.Size;

            if (FOutput.Image.Size != outputSize || FOutput.Image.NativeFormat != FInput.Image.NativeFormat)
            {
                FOutput.Image.Initialise(outputSize, FInput.Image.NativeFormat);
            }

            switch (anticlockwiseSteps)
            {
                case 0:
                    FInput.GetImage(FOutput.Image);
                    break;

                case 1:
                    if (FInput.LockForReading())
                    {
                        try
                        {
                            //CvInvoke.cvTranspose(FInput.CvMat, FOutput.CvMat);
                            CvInvoke.Transpose(FInput.Image.GetImage(), FOutput.Image.GetImage());
                        }
                        finally
                        {
                            FInput.ReleaseForReading();
                        }
                        //CvInvoke.cvFlip(FOutput.CvMat, FOutput.CvMat, FLIP.VERTICAL);
                        CvInvoke.Flip(FOutput.Image.GetImage(), FOutput.Image.GetImage(), FlipType.Vertical);
                    }
                    break;

                case 2:
                    if (FInput.LockForReading())
                    {
                        try
                        {
                            CvInvoke.Flip(FInput.Image.GetImage(), FOutput.Image.GetImage(), FlipType.Horizontal);
                        }
                        finally
                        {
                            FInput.ReleaseForReading();
                        }
                        CvInvoke.Flip(FOutput.Image.GetImage(), FOutput.Image.GetImage(), FlipType.Vertical);
                    }
                    break;

                case 3:
                    if (FInput.LockForReading())
                    {
                        try
                        {
                            CvInvoke.Transpose(FInput.Image.GetImage(), FOutput.Image.GetImage());
                        }
                        finally
                        {
                            FInput.ReleaseForReading();
                        }
                        CvInvoke.Flip(FOutput.Image.GetImage(), FOutput.Image.GetImage(), FlipType.Horizontal);
                    }
                    break;
            }
            FOutput.Send();
        }
    }
}
