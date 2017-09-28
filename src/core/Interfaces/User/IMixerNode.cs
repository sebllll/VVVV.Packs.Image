using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Core
{
	public abstract class IMixerNode<T> : INode, IDisposable where T : IMixerInstance, new()
	{
		[Input("Input1", Order = -1)]
		private ISpread<CVImageLink> FInput1;

        [Input("Input2", Order = -1)]
        private ISpread<CVImageLink> FInput2;

        [Output("Output", Order = -1)]
		private ISpread<CVImageLink> FOutput;

		protected ProcessMixer<T> FProcessor;

		public override void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
                FProcessor = new ProcessMixer<T>(FInput1, FInput2, FOutput);

            bool changed = FProcessor.CheckInputSize(SpreadMax);
			Update(FProcessor.SliceCount, changed);
		}

		public void Dispose()
		{
			// sometimes we get a double dispose from vvvv on quit
			if (FProcessor != null)
				FProcessor.Dispose();
		}
	}
}
