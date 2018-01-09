using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.CV.Core
{
	public class ProcessMaskDestination<T> : IProcess<T>, IDisposable where T : IDestinationMaskInstance, new()
	{
		CVImageInputSpread FInput;
		public CVImageInputSpread Input { get { return FInput; } }

        CVImageInputSpread FInputMask;
        public CVImageInputSpread InputMask { get { return FInputMask; } }


        public ProcessMaskDestination(ISpread<CVImageLink> inputPin, ISpread<CVImageLink> inputMaskPin)
		{
            FInput = new CVImageInputSpread(inputPin);
            FInputMask = new CVImageInputSpread(inputMaskPin);

            StartThread();
		}

		private void ThreadedFunction()
		{
			while (FThreadRunning)
			{
				if (FInput.Connected && FInputMask.Connected)
				{
					lock (FLockProcess)
					{
						for (int i = 0; i < SliceCount; i++)
						{
							if (!FInput[i].Allocated || !FInputMask[i].Allocated)
								continue;

							if (FInput[i].ImageAttributesChanged || FInputMask[i].ImageAttributesChanged || FProcess[i].NeedsAllocate)
							{
                                FInput[i].ClearImageAttributesChanged();
                                FInputMask[i].ClearImageAttributesChanged();
                                FProcess[i].ClearNeedsAllocate();
								for (int iProcess = i; iProcess < SliceCount; iProcess += (FInput.SliceCount > 0 ? FInput.SliceCount : int.MaxValue))
									FProcess[iProcess].Allocate();
							}

							try
							{
								if (FInput[i].ImageChanged || FInputMask[i].ImageChanged)
								{
                                    FInput[i].ClearImageChanged();
                                    FInputMask[i].ClearImageChanged();
                                    for (int iProcess = i; iProcess < SliceCount; iProcess += (FInput.SliceCount > 0 ? FInput.SliceCount : int.MaxValue))
										FProcess[iProcess].Process();
								}
							}
							catch (Exception e)
							{
								ImageUtils.Log(e);
							}
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
            FThread.Name = "OpenCV Destination";
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


		ThreadMode FThreadMode = ThreadMode.Independant;
		public ThreadMode ThreadMode
		{
			set
			{
				if (value == FThreadMode)
					return;

				FThreadMode = value;
				if (FThreadMode == ThreadMode.Independant)
				{
					RemoveDirectListeners();
					StartThread();
				}
				else
				{
					StopThread();
					AddDirectListeners();
				}
			}
		}

		void AddDirectListeners()
		{
			RemoveDirectListeners();

			for (int i = 0; i < SliceCount; i++)
			{
                FInput[i].ImageUpdate += new EventHandler(FProcess[i].UpstreamDirectUpdate);
                FInput[i].ImageAttributesUpdate += new EventHandler<ImageAttributesChangedEventArgs>(FProcess[i].UpstreamDirectAttributesUpdate);

                // what if both images change?
                FInputMask[i].ImageUpdate += new EventHandler(FProcess[i].UpstreamDirectUpdate);
                FInputMask[i].ImageAttributesUpdate += new EventHandler<ImageAttributesChangedEventArgs>(FProcess[i].UpstreamDirectAttributesUpdate);
            }
        }

		void RemoveDirectListeners()
		{
			for (int i = 0; i < SliceCount; i++)
			{
                FInput[i].ImageUpdate -= new EventHandler(FProcess[i].UpstreamDirectUpdate);
                FInput[i].ImageAttributesUpdate -= new EventHandler<ImageAttributesChangedEventArgs>(FProcess[i].UpstreamDirectAttributesUpdate);

                FInputMask[i].ImageUpdate -= new EventHandler(FProcess[i].UpstreamDirectUpdate);
                FInputMask[i].ImageAttributesUpdate -= new EventHandler<ImageAttributesChangedEventArgs>(FProcess[i].UpstreamDirectAttributesUpdate);
            }
        }

		#region Spread access

		public T GetProcessor(int index)
		{
			return FProcess[index];
		}

		public CVImageInput GetInput(int index)
		{
			return FInput[index];
		}

        public CVImageInput GetInputMask(int index)
        {
            return FInputMask[index];
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
			//return CheckInputSize(FInput.SliceCount);
            return CheckInputSize(Math.Max(FInput.SliceCount, FInputMask.SliceCount));
        }

        /// <summary>
        /// Check the SliceCount
        /// </summary>
        /// <param name="SpreadMax">New SliceCount</param>
        /// <returns>true if changes were made</returns>
        //public bool CheckInputSize(int SpreadMax)
        //{
        //	if (!FInput.CheckInputSize() && FProcess.SliceCount==SpreadMax)
        //		return false;

        //	lock (FLockProcess)
        //	{
        //		if (FInput.SliceCount == 0)
        //			SpreadMax = 0;
        //		else if (FInput[0] == null)
        //			SpreadMax = 0;

        //		for (int i = FProcess.SliceCount; i < SpreadMax; i++)
        //			Add(FInput[i], FInputMask[i]);

        //		if (FProcess.SliceCount > SpreadMax)
        //		{
        //			for (int i = SpreadMax; i < FProcess.SliceCount; i++)
        //				Dispose(i);

        //			FProcess.SliceCount = SpreadMax;
        //		}
        //	}

        //	return true;
        //}
        public bool CheckInputSize(int SpreadMax)
        {

            var InputChanged = FInput.CheckInputSize();
            var MaskChanged = FInputMask.CheckInputSize();

            var InputConnected = FInput.Connected;
            var MaskConnected = FInputMask.Connected;

            var s1 = FInput.SliceCount != 0;
            var s2 = FInputMask.SliceCount != 0;

            if (!InputConnected || !MaskConnected || !s1 || !s2) // one or both inputs are not connected
                return false;

            if (!InputChanged && !MaskChanged) // inputs didn't change
                return false;

            lock (FLockProcess)
            {
                if (FInput.SliceCount == 0 && FInputMask.SliceCount == 0)
                {
                    SpreadMax = 0;
                    return false; // do nothing if one of the inputs is emptyTexture
                }
                else if (FInput[0] == null && FInputMask[0] == null)
                {
                    SpreadMax = 0;
                    return false; // do nothing if one of the inputs is empty
                }


                // re-add inputs when only one input is connected
                //if (!c1 || !c2 /*&& c1 != c2*/)
                //{
                //    for (int i = 0; i < FProcess.SliceCount; i++)
                //        Dispose(i); //Dispose();

                //    FProcess.SliceCount = 0;
                //    FOutput.SliceCount = 0;
                //}

                if (FProcess.SliceCount > SpreadMax)
                {
                    for (int i = SpreadMax; i < FProcess.SliceCount; i++)
                        Dispose(i);

                    FProcess.SliceCount = SpreadMax;
                    //FOutput.SliceCount = SpreadMax;
                }

                for (int i = FProcess.SliceCount; i < SpreadMax; i++)
                {
                    // check if only 1 input is present
                    if (FInput.SliceCount == 0 || FInput[0] == null)
                        Add(FInputMask[i], FInputMask[i]);
                    else if (FInputMask.SliceCount == 0 || FInputMask[0] == null)
                        Add(FInput[i], FInput[i]);
                    else
                        Add(FInput[i], FInputMask[i]); // the 'normal' case
                }




                //FOutput.AlignOutputPins();
            }

            return true;
        }


        private void Add(CVImageInput input, CVImageInput mask)
		{
			T addition = new T();

			addition.SetInput(input, mask);

			FProcess.Add(addition);
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

            FInput.Dispose();
            FInputMask.Dispose();
        }

        protected void Dispose(int i)
		{
			var disposableContainer = FProcess[i] as IDisposable;
			if (disposableContainer != null)
			{
				disposableContainer.Dispose();
			}
			if (i < FInput.SliceCount)
				FInput[i].Dispose();
            if (i < FInputMask.SliceCount)
                FInputMask[i].Dispose();
        }

	}
}
