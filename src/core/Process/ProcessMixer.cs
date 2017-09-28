﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.CV.Core
{
	public class ProcessMixer<T> : IProcess<T>, IDisposable where T : IMixerInstance, new()
	{
		CVImageInputSpread FInput1;
		public CVImageInputSpread Input1 { get { return FInput1; } }

        CVImageInputSpread FInput2;
        public CVImageInputSpread Input2 { get { return FInput2; } }

        CVImageOutputSpread FOutput;
		public CVImageOutputSpread Output { get { return FOutput; } }

		public ProcessMixer(ISpread<CVImageLink> inputPin1, ISpread<CVImageLink> inputPin2, ISpread<CVImageLink> outputPin)
		{
			FInput1 = new CVImageInputSpread(inputPin1);
            FInput2 = new CVImageInputSpread(inputPin2);

            FOutput = new CVImageOutputSpread(outputPin);
			
			StartThread();
		}

		private void ThreadedFunction()
		{
			while (FThreadRunning)
			{
				if (FInput1.Connected && FInput2.Connected)
				{
					lock (FLockProcess)
					{
						try
						{
							for (int i = 0; i < SliceCount; i++)
							{
                                //if (FInput1[i].Allocated)
                                //{

                                //}

                                if (!FInput1[i].Allocated || !FInput2[i].Allocated)
								continue;

								if (FInput1[i].ImageAttributesChanged || FInput2[i].ImageAttributesChanged || FProcess[i].NeedsAllocate)
								{
									FInput1[i].ClearImageAttributesChanged();
                                    FInput2[i].ClearImageAttributesChanged();
                                    FProcess[i].ClearNeedsAllocate();

									FProcess[i].ClearNeedsAllocate();
									FProcess[i].Allocate();
								}

								if (FInput1[i].ImageChanged || FInput2[i].ImageChanged || FProcess[i].FlaggedForProcess)
								{
									FInput1[i].ClearImageChanged();
                                    FInput2[i].ClearImageChanged();
                                    FProcess[i].ClearFlagForProcess();
									FProcess[i].TransferTimestamp();
									FProcess[i].Process();
								}
							}

						}		
						catch (Exception e)
						{
							ImageUtils.Log(e);
						}
					}

					Thread.Sleep(1);
				}
				else
				{
					Thread.Sleep(10);
				}
			}
		}

		private void StartThread()
		{
			FThreadRunning = true;
			FThread = new Thread(ThreadedFunction);
            FThread.Name = "OpenCV Filter";
            FThread.Start();
		}

		private void StopThread()
		{
			if (FThreadRunning)
			{
				FThreadRunning = false;
				FThread.Join();
			}
		}

		#region Spread access

		public T GetProcessor(int index)
		{
			return FProcess[index];
		}


        // never used !?
		//public CVImageInput GetInput(int index)
		//{
		//	return FInput1[index];
		//}

		public CVImageOutput GetOutput(int index)
		{
			return FOutput[index];
		}

		public int SliceCount
		{
			get
			{
				return FProcess.SliceCount;
			}
		}
		#endregion

		public bool CheckInputSize()
		{
			return CheckInputSize(Math.Max(FInput1.SliceCount, FInput2.SliceCount));
		}

		/// <summary>
		/// Check the SliceCount
		/// </summary>
		/// <param name="SpreadMax">New SliceCount</param>
		/// <returns>true if changes were made</returns>
		public bool CheckInputSize(int SpreadMax)
		{
			if ((!FInput1.CheckInputSize() && !FInput2.CheckInputSize())  && FOutput.SliceCount == SpreadMax)
				return false;

			lock (FLockProcess)
			{
				if (FInput1.SliceCount == 0 || FInput2.SliceCount == 0)
					SpreadMax = 0;
				else if (FInput1[0] == null || FInput2[0] == null)
					SpreadMax = 0;

				for (int i = FProcess.SliceCount; i < SpreadMax; i++)
                {
                    Add(FInput1[i], FInput2[i]);
                }
					

				if (FProcess.SliceCount > SpreadMax)
				{
					for (int i = SpreadMax; i < FProcess.SliceCount; i++)
						Dispose(i);

					FProcess.SliceCount = SpreadMax;
					FOutput.SliceCount = SpreadMax;
				}

				FOutput.AlignOutputPins();
			}

			return true;
		}

		private void Add(CVImageInput input1, CVImageInput input2)
		{
			CVImageOutput output = new CVImageOutput();
			T addition = new T();

			addition.SetInput(input1, input2);
			addition.SetOutput(output);

			FProcess.Add(addition);
			FOutput.Add(output);
		}

		public T this[int index]
		{
			get
			{
				return FProcess[index];
			}
		}

		protected void Resize(int count)
		{
			FProcess.SliceCount = count;
			FOutput.AlignOutputPins();
		}

		public void Dispose()
		{
			StopThread();

			foreach (var process in FProcess)
			{
				var disposableContainer = process as IDisposable;
				if (disposableContainer != null)
				{
					disposableContainer.Dispose();
				}
			}

			FInput1.Dispose();
            FInput2.Dispose();
            FOutput.Dispose();
		}

		protected void Dispose(int i)
		{
			var disposableContainer = FProcess[i] as IDisposable;
			if (disposableContainer != null)
			{
				disposableContainer.Dispose();
			}
			FInput1[i].Dispose();
            FInput2[i].Dispose();
            FOutput[i].Dispose();
		}
	}
}
