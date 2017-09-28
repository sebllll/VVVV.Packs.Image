using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public interface IInstanceInput
	{
		void SetInput(CVImageInput input);
	}

    public interface IInstanceDualInput
    {
        void SetInput(CVImageInput input1, CVImageInput input2);
    }
}
