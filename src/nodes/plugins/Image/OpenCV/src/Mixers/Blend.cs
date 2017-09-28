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
    [MixerInstance("Blend", Help = "Blend 2 images", Author = "sebl")]
    public class BlendInstance : IMixerInstance
    {
        [Input("Factor")]
        public double FInFactor;

        [Input("Gamma")]
        public double FInGamma;

        public override void Allocate()
        {
            // use FInput1 to initialise FOutput
            FOutput.Image.Initialise(FInput1.ImageAttributes.Size, FInput1.ImageAttributes.ColorFormat);
        }

        public override void Process()
        {

            if (!FInput1.LockForReading() || !FInput2.LockForReading())
                return;

            CvInvoke.cvAddWeighted(FInput1.CvMat, FInFactor, FInput2.CvMat, 1 - FInFactor, FInGamma, FOutput.CvMat);

            FInput1.ReleaseForReading(); //and  this after you've finished with FImage
            FInput2.ReleaseForReading();

            FOutput.Send();
        }

    }
}
