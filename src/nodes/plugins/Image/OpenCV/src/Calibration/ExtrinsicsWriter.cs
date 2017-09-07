using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Nodes.Calibration
{
    #region PluginInfo
    [PluginInfo(Name = "Writer",
                Category = "CV.Transform",
                Version = "Extrinsics",
                Help = "Write extrinsics to a file",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class ExtrinsicsWriterNode : IPluginEvaluate
    {
        [Input("Extrinsics")]
        ISpread<Extrinsics> FInExtrinsics;

        [Input("Filename", StringType = StringType.Filename)]
        ISpread<string> FInFilename;

        [Input("Write", IsBang = true)]
        ISpread<bool> FInWrite;

        [Output("Status")]
        ISpread<string> FOutStatus;

        IFormatter FFormatter = new BinaryFormatter();

        public void Evaluate(int SpreadMax)
        {
            FOutStatus.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                if (FInWrite[i] && FInExtrinsics[i] != null)
                {
                    try
                    {
                        var file = new FileStream(FInFilename[i], FileMode.Create);
                        FFormatter.Serialize(file, FInExtrinsics[i]);
                        file.Close();
                        FOutStatus[i] = "OK";
                    }
                    catch (Exception e)
                    {
                        FOutStatus[i] = e.Message;
                    }
                }
            }
        }
    }
}
