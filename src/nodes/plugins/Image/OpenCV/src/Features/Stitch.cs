using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Stitching;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes.Features
{
    #region PluginInfo
    [PluginInfo(Name = "Stitch", Category = "CV.Features", Help = "Stitch together a spread of Images", Tags = "match")]
    #endregion PluginInfo
    public class Stitch : IPluginEvaluate
    {
        [Input("Input")]
        ISpread<CVImageLink> FInput;

        [Input("Do", IsBang=true, IsSingle = true)]
        ISpread<bool> FDo;

        //[Output("Output")]
        //ISpread<CVImageLink> FOutput;

        [Output("Output")]
        ISpread<CVImageLink> FOutput;

        [Output("Status")]
        ISpread<string> FStatus;

        Stitcher FStitcher = new Stitcher(false);

        public void Evaluate(int SpreadMax)
        {
            //for (int i = SpreadMax; i < FOutput.SliceCount; i++)
            //    FOutput[i].Dispose();
            //while (FOutput.SliceCount < SpreadMax)
            //    FOutput.Add(new CVImageLink());
            //while (FOutput.SliceCount < 1)
            //        FOutput.Add(new CVImageLink());


            FStatus.SliceCount = 1;

            if (FDo[0])
            {
                // setup image array for stitcher
                int size = FInput.SliceCount;
                Image<Bgr, Byte>[] images = new Image<Bgr, byte>[size];

                // setup output
                CVImage result = new CVImage();

                foreach (var image in FInput)
                {
                    image.LockForReading();
                }

                for (int i = 0; i < SpreadMax; i++)
                {

                    var tempImage = FInput[i].FrontImage.GetImage() as Image<Rgb, byte>;

                    images[i] = tempImage.Convert<Bgr, byte>();
                }


                
                try
                {
                    result.SetImage(FStitcher.Stitch(images));
                    //FOutput[0].Send(FStitcher.Stitch(images));

                    foreach (var image in images)
                    {
                        image.Dispose();
                    }

                    FStatus[0] = "OK";
                }
                catch (Exception e)
                {
                    FStatus[0] = e.Message;
                }
                finally
                {
                    foreach(var image in FInput)
                    {
                        image.ReleaseForReading();
                    }
                }

                //output.Send(result);

                var outImg = new CVImage();
                outImg.Initialise(result.ImageAttributes);
                outImg.SetImage(result);

                FOutput[0].Initialise(result.ImageAttributes);
                FOutput[0].Send(outImg);

                //vara = FOutput[0].Allocated;

            }

        }
    }
}
