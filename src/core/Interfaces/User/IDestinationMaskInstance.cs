using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public abstract class IDestinationMaskInstance : IInstance, IInstanceDualInput
    {
        protected CVImageInput FInput;
        protected CVImageInput FInputMask;

        public void SetInput(CVImageInput input, CVImageInput mask)
		{
			FInput = input;
            FInputMask = mask;

            ReAllocate();
		}

		public bool HasInput(CVImageInput input, CVImageInput mask)
		{
			return FInput == input && FInputMask == mask;
		}

		public void UpstreamDirectUpdate(object sender, EventArgs e)
		{
			Process();
		}

		public void UpstreamDirectAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate();
			Process();
		}
	}
}
