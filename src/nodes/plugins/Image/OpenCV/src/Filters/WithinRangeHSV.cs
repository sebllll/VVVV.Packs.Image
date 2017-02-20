using Emgu.CV;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
    [FilterInstance("WithinRange", Version = "HSV", Author = "alg", Help = "Check if value is in target HSV range")]
    public class WithinRangeHsvInstance : IFilterInstance
    {
        [Input("Minimum", DefaultValues = new double[] {0, 0, 0}, MinValue = 0, MaxValue = 1)] 
        public Vector3D Minimum;

        [Input("Maximum", DefaultValues = new double[] {1, 1, 1}, MinValue = 0, MaxValue = 1)] 
        public Vector3D Maximum;

        [Input("Pass Original", DefaultBoolean = false, IsToggle = true, IsSingle = true)] 
        public bool PassOriginal;

        private readonly CVImage FHsvImage = new CVImage();
        private readonly CVImage FBuffer = new CVImage();


        private double FMult = byte.MaxValue;

        public override void Allocate()
        {
            FHsvImage.Initialise(FInput.ImageAttributes.Size, TColorFormat.HSV32F);
            FBuffer.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
            
            FOutput.Image.Initialise(FInput.Image.ImageAttributes);

            FMult = FInput.ImageAttributes.BytesPerPixel > 4 ? float.MaxValue : byte.MaxValue;
        }

        public override void Process()
        {
            if (!FInput.LockForReading()) return;

            FInput.GetImage(FHsvImage);
            
            FInput.ReleaseForReading();

            //CvInvoke.cvInRangeS(FHsvImage.CvMat, new MCvScalar(Minimum.x * FMult, Minimum.y * FMult, Minimum.z * FMult),
            CvInvoke.InRange(FHsvImage.GetImage(), new MCvScalar(Minimum.x * FMult, Minimum.y * FMult, Minimum.z * FMult),
                    new MCvScalar(Maximum.x * FMult, Maximum.y * FMult, Maximum.z * FMult), FBuffer.CvMat);

            if (PassOriginal)
            {
                FOutput.Image.SetImage(FInput.Image);

                CvInvoke.cvNot(FBuffer.CvMat, FBuffer.CvMat);
                CvInvoke.cvSet(FOutput.Image.CvMat, new MCvScalar(0.0), FBuffer.CvMat);

                FOutput.Send();
            }
            else
            {
                FOutput.Send(FBuffer);
            }
        }

        // maybe a simplifying method like this might help in those cases
        //private Mat CheckRangeInvoked(Mat img, Hsv lower, Hsv upper)
        //{
        //    Mat result = new Mat();

        //    using (Mat hsvImg = new Mat())
        //    using (Mat mask1 = new Mat())
        //    using (Mat mask2 = new Mat())
        //    using (Mat minHsv = new Image<Hsv, byte>(1, 1, new Hsv(0, lower.Satuation, lower.Value)).Mat)
        //    using (Mat lowerHsv = new Image<Hsv, byte>(1, 1, lower).Mat)
        //    using (Mat upperHsv = new Image<Hsv, byte>(1, 1, upper).Mat)
        //    using (Mat maxHsv = new Image<Hsv, byte>(1, 1, new Hsv(180, upper.Satuation, upper.Value)).Mat)
        //    {
        //        //CvInvoke.CvtColor(img, hsvImg,  ColorConversion.Bgr2Hsv);

        //        CvInvoke.InRange(hsvImg, minHsv, upperHsv, mask1);
        //        CvInvoke.InRange(hsvImg, lowerHsv, maxHsv, mask2);

        //        CvInvoke.BitwiseOr(mask1, mask2, result);
        //    }
        //    return result;
        //}
    }
}