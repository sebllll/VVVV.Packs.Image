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
	public class CountNonZeroRGBInstance : IDestinationInstance
	{
        public int[] Count
        {
            get
            {
                return count;
            }
        }

        int[] count = new int[3];


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

            count[0] = CvInvoke.cvCountNonZero(R.CvMat);
            count[1] = CvInvoke.cvCountNonZero(G.CvMat);
            count[2] = CvInvoke.cvCountNonZero(B.CvMat);

            FInput.ReleaseForReading();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "CountNonZero", Version = "RGB", Category = "CV.Image", Help = "Counts all Non-Zero pixels.", Author = "elliotwoods", Credits = "", Tags = "mean, standard deviation")]
	#endregion PluginInfo
	public class CountNonZeroRGBNode : IDestinationNode<CountNonZeroRGBInstance>
	{
		[Output("Count")]
		ISpread<int> FCount;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
            FCount.SliceCount = InstanceCount * 3;

			for (int i = 0; i < InstanceCount * 3; i++)
			{
                FCount[i * 3] = FProcessor[i].Count[0];
                FCount[i * 3 + 1] = FProcessor[i].Count[1];
                FCount[i * 3 + 2] = FProcessor[i].Count[2];

            }
        }
	}
}
