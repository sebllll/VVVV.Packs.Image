#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;
using VVVV.CV.Core;

#endregion usings

namespace VVVV.CV.Nodes
{
	public enum ContourApproximation { None, Simple, TehChinL1, TehChinKCOS, LinkRuns, Poly };

	public class ContourPerimeter : ICloneable
	{
		public ContourPerimeter(ContourPerimeter other)
		{
			this.Points = new PointF[other.Points.Length];
			Array.Copy(other.Points, this.Points, other.Points.Length);
			this.Length = other.Length;
			this.Size = other.Size;
		}
        
        //public ContourPerimeter(Contour<Point> contour, int imageWidth, int imageHeight)
        public ContourPerimeter(MCvContour contour, int imageWidth, int imageHeight)
        {
            Points = new PointF[contour.total];

            for (int i = 0; i < contour.total; i++)
			{
				this.Points[i].X = contour[i].X / (float)imageWidth * 2.0f - 1.0f;
				this.Points[i].Y = 1.0f - contour[i].Y / (float)imageHeight * 2.0f;
			}

            //this.Length = contour.Perimeter;
            this.Length = contour.total;
            this.Size.Width = imageWidth;
			this.Size.Height = imageHeight;
		}

		public PointF[] Points;
		public double Length;
		public Size Size;

		public object Clone()
		{
			return new ContourPerimeter(this);
		}
	}

	public class ContourInstance : IDestinationInstance
	{
		public bool Enabled = true;
		public ContourApproximation Approximation = ContourApproximation.Simple;

		double FPolyAccuracy = 3;
		public double PolyAccuracy
		{
			set
			{
				if (value < 1)
					value = 1;
				if (value > 1000)
					value = 1000;

				FPolyAccuracy = value;
			}
		}

		CVImage FGrayscale = new CVImage();

		Object FLockResults = new Object();
		Spread<Vector4D> FBoundingBox = new Spread<Vector4D>(0);
		Spread<double> FArea = new Spread<double>(0);
		Spread<ContourPerimeter> FPerimeter = new Spread<ContourPerimeter>(0);
		public string FStatus = "";

		public ISpread<Vector4D> BoundingBox
		{
			get
			{
				lock (FLockResults)
					return FBoundingBox.Clone();
			}
		}

		public ISpread<double> Area
		{
			get
			{
				lock (FLockResults)
					return FArea.Clone();
			}
		}

		public ISpread<ContourPerimeter> Perimeter
		{
			get
			{
				lock (FLockResults)
				{
					Spread<ContourPerimeter> value = new Spread<ContourPerimeter>(FPerimeter.SliceCount);
					for (int i = 0; i < FPerimeter.SliceCount; i++)
						value[i] = FPerimeter[i].Clone() as ContourPerimeter;
					return value;
				}
			}
		}

		public string Status
		{
			get
			{
				lock (FLockResults)
					return FStatus.Clone() as string;
			}
		}

		public override void Allocate()
		{
			FGrayscale.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
		}

		private struct ContourTempData
		{
			public Rectangle Bounds;
			public double Area;
            //public ContourPerimeter Perimeter;
            //public VectorOfPoint Perimeter;
            public VectorOfVectorOfPoint Perimeter;
        }

        override public void Process()
		{
			if (!Enabled)
				return;

			FInput.Image.GetImage(TColorFormat.L8, FGrayscale);
			Image<Gray, byte> img = FGrayscale.GetImage() as Image<Gray, byte>;
			if (img != null)
			{
				//Seriously EmguCV? what the fuck is up with your syntax?
				//both ways of skinning this cat involve fucking a moose

				List<ContourTempData> results = new List<ContourTempData>();
				ContourTempData c;

				try
				{
                    ChainApproxMethod cam;

					switch (Approximation) {
						case ContourApproximation.None:
							cam = ChainApproxMethod.ChainApproxNone;
							break;
						
						case ContourApproximation.TehChinKCOS:
							cam = ChainApproxMethod.ChainApproxTc89Kcos;
							break;

						case ContourApproximation.TehChinL1:
							cam = ChainApproxMethod.ChainApproxTc89L1;
							break;

						case ContourApproximation.LinkRuns:
							cam = ChainApproxMethod.LinkRuns;
							break;

						case ContourApproximation.Simple:
						case ContourApproximation.Poly:
						default:
							cam = ChainApproxMethod.ChainApproxSimple;
							break;
					}

                    //Contour<Point> contour = img.FindContours(cam, RETR_TYPE.CV_RETR_LIST);
                    VectorOfVectorOfPoint contour = new VectorOfVectorOfPoint();
                    //MCvContour contour = new MCvContour();
                    Mat hierarchy = new Mat();

                    CvInvoke.FindContours(img, contour, hierarchy, RetrType.List, cam);

                    List<Rectangle> segmentRectangles = new List<Rectangle>();
                    int contCount = contour.Size;
                    for (int i = 0; i < contCount; i++)
                    {
                        using (VectorOfPoint con = contour[i])
                        {
                            segmentRectangles.Add(CvInvoke.BoundingRectangle(con));
                        }
                    }

                    // need to iterate through all contours in a foreach or similar
                    for(int i = 0; i < contour.Size; i++)
                    {
                        c = new ContourTempData();
                        c.Area = CvInvoke.ContourArea(contour[i]);
                        c.Bounds = CvInvoke.BoundingRectangle(contour[i]);


                        if (Approximation == ContourApproximation.Poly)
                        {
                            VectorOfPoint poly = new VectorOfPoint();
                            //c.Perimeter = new ContourPerimeter(contour.ApproxPoly(FPolyAccuracy), img.Width, img.Height);
                            CvInvoke.ApproxPolyDP(contour, poly, FPolyAccuracy, true);

                            c.Perimeter.Push(poly);
                                
                        }
                        else
                        {
                            c.Perimeter.Push(contour[i]);
                        }

                    }

                    /*
                    for (; contour != null; contour = contour.HNext)
                        {
                            c = new ContourTempData();
                        //c.Area = contour.Area;
                        //c.Area = contour.GetOutputArray();     // needs to be calculated by hand?
                        //c.Bounds = contour.BoundingRectangle;
                        c.Bounds = segmentRectangles[0];

                        if (Approximation == ContourApproximation.Poly)
                        {
                            VectorOfVectorOfPoint poly = new VectorOfVectorOfPoint();
                            //c.Perimeter = new ContourPerimeter(contour.ApproxPoly(FPolyAccuracy), img.Width, img.Height);
                            CvInvoke.ApproxPolyDP(contour, poly, FPolyAccuracy, true);
                        }
                        else
                            c.Perimeter = new ContourPerimeter(contour, img.Width, img.Height);

						results.Add(c);
					}
                    */
                    


                    lock (FLockResults)
						FStatus = "OK";
				}
				catch (Exception e)
				{
					lock (FLockResults)
						FStatus = e.Message;
				}

				lock (FLockResults)
				{
					FBoundingBox.SliceCount = results.Count;
					FArea.SliceCount = results.Count;
					FPerimeter.SliceCount = results.Count;

					for (int i = 0; i < results.Count; i++)
					{
						c = results[i];

						FBoundingBox[i] = new Vector4D(((double)c.Bounds.X / (double)img.Width) * 2.0d - 1.0d,
							 1.0d - ((double)c.Bounds.Y / (double)img.Height) * 2.0d,
							 (double)c.Bounds.Width * 2.0d / (double)img.Width,
							 (double)c.Bounds.Height * 2.0d / (double)img.Height);

						FArea[i] = (double)c.Area*  (4.0d / (double)(img.Width * img.Height));

						FPerimeter[i] = c.Perimeter;
					}
				}

			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Contour", Category = "CV.Image", Version = "", Help = "Finds contours in binary image", Tags = "analysis")]
	#endregion PluginInfo
	public class ContourNode : IDestinationNode<ContourInstance>
	{
		#region fields & pins
		[Input("Approximation", DefaultEnumEntry = "Simple")]
		IDiffSpread<ContourApproximation> FPinInApproximation;

		[Input("Poly approximation accuracy", DefaultValue=3, MinValue=1, MaxValue=1000)]
		IDiffSpread<double> FPinInPolyAccuracy;

		[Input("Enabled", DefaultValue = 1)]
		IDiffSpread<bool> FPinInEnabled;

		[Output("Bounding box")]
		ISpread<ISpread<Vector4D>> FPinOutBounds;

		[Output("Area")]
		ISpread<ISpread<double>> FPinOutArea;

		[Output("Perimeter")]
		ISpread<ISpread<ContourPerimeter>> FPinOutPerimeter;

		[Output("Status")]
		ISpread<string> FStatus;

		#endregion fields & pins

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			CheckParams(InstanceCount);
			Output(InstanceCount);
		}

		void CheckParams(int InstanceCount)
		{
			if (FPinInEnabled.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Enabled = FPinInEnabled[0];
				
			if (FPinInApproximation.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Approximation = FPinInApproximation[i];

			if (FPinInPolyAccuracy.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].PolyAccuracy = FPinInPolyAccuracy[i];

		}

		void Output(int InstanceCount)
		{
			FPinOutArea.SliceCount = InstanceCount;
			FPinOutBounds.SliceCount = InstanceCount;
			FPinOutPerimeter.SliceCount = InstanceCount;
			FStatus.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FPinOutArea[i] = FProcessor[i].Area;
				FPinOutBounds[i] = FProcessor[i].BoundingBox;
				FPinOutPerimeter[i] = FProcessor[i].Perimeter;
				FStatus[i] = FProcessor[i].Status;
			}
		}
	}
}
