using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public abstract class IMixerInstance : IInstance, IInstanceDualInput, IInstanceOutput
	{
		protected CVImageInput FInput1;
        protected CVImageInput FInput2;
        protected CVImageOutput FOutput;

		public void SetInput(CVImageInput input1, CVImageInput input2)
		{
			FInput1 = input1;
            FInput2 = input2;
            ReAllocate();
		}

		public bool HasInput(CVImageInput input)
		{
			return FInput1 == input || FInput2 == input;
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		/// <summary>
		/// You should call this inside your filter if your filter takes some time (> 1/framerate)
		/// before pulling the frame from FInput. Otherwise you can rely on the value that was called automatically.
		/// </summary>
		public void TransferTimestamp()
		{
			FOutput.Image.Timestamp = FInput1.Image.Timestamp;
		}

		/// <summary>
		/// Override this with false if your filter
		/// doesn't need to run every frame
		/// </summary>
		/// <returns></returns>
		virtual public bool IsFast()
		{
			return true;
		}

		bool FFlaggedForProcess = false;
		public bool FlaggedForProcess
		{
			get
			{
				return FFlaggedForProcess;
			}
		}
		public void FlagForProcess()
		{
			FFlaggedForProcess = true;
		}
		public void ClearFlagForProcess()
		{
			FFlaggedForProcess = false;
		}
	}
}
