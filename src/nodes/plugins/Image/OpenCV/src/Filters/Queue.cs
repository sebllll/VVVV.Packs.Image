using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Emgu.CV.Util;

using VVVV.CV.Core;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Nodes.Generic;



namespace VVVV.CV.Nodes
{
    #region PluginInfo
    //[FilterInstance("Queue", Category = "CV.Image", Help = "", AutoEvaluate = true, Author = "sebl")]
    [FilterInstance("Queue", Version = "", Tags = "", Help = "A Queue for CV Images", Author = "sebl")]
    #endregion PluginInfo
    public class QueueNode : IFilterInstance
    {
        #region fields and pins

        [Input("Frame Count")]
        public int FFrameCount = 1;

        [Input("Insert")]
        public bool FDoInsert = false;

        [Input("Reset")]
        public bool FReset = false;

        [Input("Index")]
        public int FInIndex = 0;


        [Output("Queue Count")]
        public int FOutCount;
        //{
        //    set
        //    {
        //        FOutCount = QueueCount;
        //    }
        //}


        int QueueCount = 0;

        private List<CVImage> FrameQueue = new List<CVImage>();

        #endregion fields and pins


        public override void Allocate()
        {
            FrameQueue = new List<CVImage>();
            FOutput.Image.Initialise(FInput.Image.ImageAttributes);
        }


        public override void Process()
        {

            if (FReset)
            {
                foreach (CVImage img in FrameQueue)
                    img.Dispose();
                
                FrameQueue.Clear();
            }

            // push frames
            
            if (!FInput.LockForReading()) // not sure if needed
                return;    

            if (FDoInsert)
            {

                CVImage copy = new CVImage();
                copy.Initialise(FInput.ImageAttributes);
                ImageUtils.CopyImage(FInput.Image, copy);
                //FrameQueue.Add(copy);
                FrameQueue.Insert(0,copy);
            }

            FInput.ReleaseForReading(); // not sure if needed


            // trim queue if full

            if (FFrameCount >= 0 && FrameQueue.Count > FFrameCount)
            {
                var tooMuch = FrameQueue.Count - FFrameCount;
                var toDispose = FrameQueue.GetRange(FFrameCount, tooMuch);
                foreach (CVImage img in toDispose)
                    img.Dispose();

                FrameQueue.RemoveRange(FFrameCount, tooMuch);
            }
            
            QueueCount = FrameQueue.Count();

            FOutCount = QueueCount; // outputs no workey ?

            //FPr
            
            FOutput.Send(FrameQueue[FInIndex % QueueCount]);

        }

    }
}
