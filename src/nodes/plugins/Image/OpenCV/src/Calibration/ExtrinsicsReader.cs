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
    [PluginInfo(Name = "Reader",
                Category = "CV.Transform",
                Version = "Extrinsics",
                Help = "Read extrinsics from a file",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class ExtrinsicsReaderNode : IPluginEvaluate
    {
        [Input("Filename", StringType = StringType.Filename)]
        ISpread<string> FInFilename;

        [Input("Read", IsBang = true)]
        ISpread<bool> FInRead;

        [Output("Extrinsics")]
        ISpread<Extrinsics> FOutExtrinsics;

        [Output("Status")]
        ISpread<string> FOutStatus;

        IFormatter FFormatter = new BinaryFormatter();

        public void Evaluate(int SpreadMax)
        {
            FOutStatus.SliceCount = SpreadMax;
            FOutExtrinsics.SliceCount = SpreadMax;
            for (int i = 0; i < SpreadMax; i++)
            {
                if (FInRead[i])
                {
                    try
                    {
                        var file = new FileStream(FInFilename[i], FileMode.Open);
                        FOutExtrinsics[i] = (Extrinsics)FFormatter.Deserialize(file);
                        FOutStatus[i] = "OK";
                        file.Close();
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
