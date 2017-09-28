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

        private bool FNeedsConversion = false;
        private bool FNeedsResize = false;
        CVImage convertedInput2 = new CVImage();
        CVImage resizedInput2 = new CVImage();

        public override void Allocate()
        {
            FNeedsConversion = FInput1.ImageAttributes.ColorFormat != FInput2.ImageAttributes.ColorFormat;
            FNeedsResize = FInput1.ImageAttributes.Size != FInput2.ImageAttributes.Size;

            if (FNeedsConversion) convertedInput2.Initialise(FInput1.ImageAttributes.Size, FInput1.ImageAttributes.ColorFormat);
            if (FNeedsResize) resizedInput2.Initialise(FInput1.ImageAttributes.Size, FInput1.ImageAttributes.ColorFormat);


            // use FInput1 to initialise FOutput
            FOutput.Image.Initialise(FInput1.ImageAttributes.Size, FInput1.ImageAttributes.ColorFormat);
        }

        public override void Process()
        {
            if ( !FNeedsConversion && !FNeedsResize)
            {
                // no conversion or resizing at all
                if (!FInput1.LockForReading() || !FInput2.LockForReading())
                    return;

                CvInvoke.cvAddWeighted(FInput1.CvMat, 1 - FInFactor, FInput2.CvMat, FInFactor, FInGamma, FOutput.CvMat);

                FInput1.ReleaseForReading(); 
                FInput2.ReleaseForReading();
            }
            else 
            {
                if (FNeedsConversion)
                {
                    // needs conversion

                    if (!FInput2.LockForReading())
                        return;

                    FInput2.GetImage(convertedInput2);

                    FInput2.ReleaseForReading();

                    if (FNeedsResize)
                    {
                        // and resizing
                        CvInvoke.cvResize(convertedInput2.CvMat, resizedInput2.CvMat, INTER.CV_INTER_LINEAR);

                        if (!FInput1.LockForReading())
                            return;

                        CvInvoke.cvAddWeighted(FInput1.CvMat, 1 - FInFactor, resizedInput2.CvMat, FInFactor, FInGamma, FOutput.CvMat);

                        FInput1.ReleaseForReading();
                    }
                    else // only conversion
                    {
                        if (!FInput1.LockForReading())
                            return;

                        CvInvoke.cvAddWeighted(FInput1.CvMat, 1 - FInFactor, convertedInput2.CvMat, FInFactor, FInGamma, FOutput.CvMat);

                        FInput1.ReleaseForReading();
                    }

                }
                else // doesn't need conversion but resizing 
                {
                    if (!FInput1.LockForReading() || !FInput2.LockForReading())
                        return;

                    CvInvoke.cvResize(FInput2.Image.CvMat, resizedInput2.CvMat, INTER.CV_INTER_LINEAR);
                    CvInvoke.cvAddWeighted(FInput1.CvMat, 1 - FInFactor, resizedInput2.CvMat, FInFactor, FInGamma, FOutput.CvMat);


                    FInput1.ReleaseForReading(); 
                    FInput2.ReleaseForReading();
                }
            }
            
            FOutput.Send();
        }


    }
}
