using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Core
{
	public abstract class IDestinationMaskNode<T> : INode, IDisposable where T : IDestinationMaskInstance, new()
	{
		[Config("Thread mode")]
		IDiffSpread<ThreadMode> FConfigThreadMode;

		[Input("Input", Order = -2)]
		private ISpread<CVImageLink> FPinInInputImage;

        [Input("Mask", Order = -1)]
        private ISpread<CVImageLink> FPinInInputMask;

        protected ProcessMaskDestination<T> FProcessor;

		public override void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessMaskDestination<T>(FPinInInputImage, FPinInInputMask);

			if (FConfigThreadMode.IsChanged)
				FProcessor.ThreadMode = FConfigThreadMode[0];

			bool countChanged = FProcessor.CheckInputSize(this.OneInstancePerImage() ? FPinInInputImage.SliceCount : SpreadMax);
			Update(FProcessor.SliceCount, countChanged);
		}

		public void Dispose()
		{
			// sometimes we get a double dispose from vvvv on quit
			if (FProcessor != null)
				FProcessor.Dispose();
		}
	}
}
