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
	public class CountNonZeroInstance : IDestinationInstance
	{
        public int Count
        {
            get
            {
                return count;
            }
        }

        int count = 0;

        public override void Allocate()
		{
		}

		public override void Process()
		{

			if (!FInput.LockForReading())
				return;

            count = CvInvoke.cvCountNonZero(FInput.CvMat);

            FInput.ReleaseForReading();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "CountNonZero", Category = "CV.Image", Help = "Counts all Non-Zero pixels.", Author = "elliotwoods", Credits = "", Tags = "mean, standard deviation")]
	#endregion PluginInfo
	public class CountNonZeroNode : IDestinationNode<CountNonZeroInstance>
	{
		[Output("Count")]
		ISpread<int> FCount;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
            FCount.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FCount[i] = FProcessor[i].Count;
			}
		}
	}
}
