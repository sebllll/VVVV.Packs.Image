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
	[FilterInstance("Undistort2", Version = "", Tags = "filter", Help = "Undistort inmages with (pre)calculated intrinsics", Author = "sebl")]
	public class Undistort2 : IFilterInstance
	{
        [Input("Intrinsics Source")]
        public Intrinsics FPinInIntrinsics;

        [Input("Intrinsics Target")]
        public Intrinsics FPinInIntrinsicsTarget;


		public override void Allocate()
		{

			FOutput.Image.Initialise(FInput.Image.ImageAttributes);
		}

		public override void Process()
		{
            FInput.LockForReading();
            try
            {
                CvInvoke.cvUndistort2( FInput.CvMat, FOutput.CvMat, FPinInIntrinsics.intrinsics.IntrinsicMatrix.Ptr,
                                       FPinInIntrinsics.intrinsics.DistortionCoeffs, FPinInIntrinsicsTarget.intrinsics.IntrinsicMatrix);
            }
            finally
            {
                FInput.ReleaseForReading();
            }


   //         if (!FInput.LockForReading())
			//	return;

   //         CvInvoke.cvUndistort2( FInput.CvMat, FOutput.CvMat, FPinInIntrinsics[0].intrinsics.IntrinsicMatrix.Ptr, 
   //                                FPinInIntrinsics[0].intrinsics.DistortionCoeffs, FPinInIntrinsicsTarget[0].intrinsics.IntrinsicMatrix);
			//FInput.ReleaseForReading();

            FOutput.Send();
        }
	}
}
