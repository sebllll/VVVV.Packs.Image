#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using System.ComponentModel.Composition;
using VVVV.Utils.VColor;
using VVVV.CV.Core;

#endregion

namespace VVVV.CV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Limit", Category = "CV", Help = "Limit the framreate of the opencv graph to the Mainloop", Tags = "")]
    #endregion PluginInfo
    public class VVVVMainloopEventNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input")]
        ISpread<CVImageLink> FInput;

        [Output("Output")]
        ISpread<CVImageLink> FOutput;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = FInput.SliceCount;

            for (int i = 0; i < FInput.SliceCount; i++)
            {
                if (FInput[i].Allocated)
                { 
                    if (FOutput[i] == null)
                        FOutput[i] = new CVImageLink();

                    if (!FOutput[i].Allocated)
                        FOutput[i].Initialise(FInput[i].ImageAttributes);

                    // copy it to output
                    FOutput[i].Send(FInput[i].FrontImage);
                }
            }
        }
    }
}
