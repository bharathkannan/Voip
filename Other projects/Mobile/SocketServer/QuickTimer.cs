/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SocketServer
{
   public class PeriodicTimerWatch : IComparable<PeriodicTimerWatch>
   {
      public PeriodicTimerWatch(long nMilliseconds)
      {
         Period = nMilliseconds;
         watch.Reset();
         watch.Start();
      }

      Stopwatch watch = new Stopwatch();
      long Period = 0;
      long nNumberTimesFired = 0;

      public static int AccuracyAndLag = 2; /// How accurate we want our timer to be, in ms


      protected List<QuickTimer> TimerList = new List<QuickTimer>();
      protected object TimerLock = new object();

      public int Count
      {
         get
         {
            return TimerList.Count;
         }
      }

      public void AddTimer(QuickTimer timer)
      {
         lock (TimerLock)
         {
            TimerList.Add(timer);
         }
      }

      public void RemoveTimer(QuickTimer timer)
      {
         lock (TimerLock)
         {
            TimerList.Remove(timer);
         }
      }

      public void Fire()
      {
         nNumberTimesFired++;
         List<QuickTimer> TempTimerList = new List<QuickTimer>();
         lock (TimerLock)
         {
            foreach (QuickTimer timer in TimerList)
            {
               if (timer.Canceled == false)
                  TempTimerList.Add(timer);
            }
         }

         foreach (QuickTimer timer in TempTimerList)
         {
            timer.Fire();
         }
      }

      public bool Expired
      {
         get
         {
            if (GetTimeToNextFireMs() <= AccuracyAndLag) /// if we are within n ms, we're expired
               return true;
            else
               return false;
         }
      }

      private int m_nMissedFires = 0;

      public int MissedFires
      {
         get { return m_nMissedFires; }
         set { m_nMissedFires = value; }
      }
   
      public long GetTimeToNextFireMs()
      {
         long nMs = watch.ElapsedMilliseconds;
         long nTime =  Period * (nNumberTimesFired + MissedFires + 1) - nMs;

         while (nTime < (-Period))  /// If we've missed our time to fire increment our missed count so we don't start running 100% by firing every 0 ms
         {
            MissedFires++;
            nTime = Period * (nNumberTimesFired + MissedFires + 1) - nMs;
         }
       
         return nTime;
      }

       /// <summary>
       ///  Must call before sorting so we don't get inconsistent times when the thread is switched
       /// </summary>
      public void LockTimeForSort()
      {
          LockedTimeToNextFire = GetTimeToNextFireMs();
      }

      public long LockedTimeToNextFire = 0;
    
      #region IComparable<PeriodicTimerWatch> Members

      public int CompareTo(PeriodicTimerWatch other)
      {
         //long nMsTimer2 = other.GetTimeToNextFireMs();
         //long nMsTimer = GetTimeToNextFireMs();
         //return nMsTimer.CompareTo(nMsTimer2);
          return LockedTimeToNextFire.CompareTo(other.LockedTimeToNextFire);
      }

      #endregion
   }

   /// <summary>
   /// A timer class that should be highly accurate
   /// </summary>
   public class QuickTimer : IMediaTimer
   {
       internal QuickTimer(PeriodicTimerWatch objWatch, int nMsTimer, DelegateTimerFired del, string strGuid, ILogInterface logmgr)
      {
         Period = nMsTimer;
         watch = objWatch;
         CallBack = del;
         Id = Interlocked.Increment(ref BaseTimerId);
         Guid = strGuid;
         m_logmgr = logmgr;
         m_bCanceled = false;
      }

      internal QuickTimer(PeriodicTimerWatch objWatch, int nMsTimer, DelegateTimerFired del, string strGuid, ILogInterface logmgr, int nAvgDevms)
      {
         Period = nMsTimer;
         watch = objWatch;
         CallBack = del;
         Id = Interlocked.Increment(ref BaseTimerId);
         Guid = strGuid;
         m_logmgr = logmgr;
         m_bCanceled = false;
         AverageDeviationMs = nAvgDevms;
      }

      private int m_nAverageDeviationMs = 0;

      public int AverageDeviationMs
      {
         get { return m_nAverageDeviationMs; }
         set { m_nAverageDeviationMs = value; }
      }

     public double RemainingTimeSeconds
      {
         get
         {
            return 0;
         }
      }


     public void Cancel()
     {
        m_bCanceled = true;
        watch.RemoveTimer(this);
     }

     bool m_bCanceled = false;
     public bool Canceled
     {
        get
        {
           return m_bCanceled;
        }
     }
      public PeriodicTimerWatch watch = null;
      public readonly int Id;
      public readonly int Period = 0;
      public readonly string Guid;
      private DelegateTimerFired CallBack;


      public void Fire()
      {
         if (m_bCanceled == true)
            return;

         try
         {
            if (CallBack != null)
            {
               CallBack.DynamicInvoke(new object[] { this }); //invoke our self if we have no host
            }
         }
         catch (System.NullReferenceException e)
         {
            if (m_logmgr != null)
               m_logmgr.LogError(Guid, MessageImportance.Highest, "", string.Format("Exception in timer thread: {0}", e));

         }
      }


      protected ILogInterface m_logmgr = null;

      private int BaseTimerId = 1;
      private object LockInit = new object();

   }

   /// <summary>
   /// A timer class that should be highly accurate
   /// </summary>
   public class QuickTimerController
   {
      public QuickTimerController()
      {
      }

#if !MONO
      [DllImport("WinMM.DLL")]
      public static extern int timeBeginPeriod(int period);
#endif

      /// <summary>
      ///  Stores global watches so all timers with the same interval (for short duration timers) fire
      /// at the same time
      /// </summary>
      protected Dictionary<int, PeriodicTimerWatch> GlobalWatches = new Dictionary<int, PeriodicTimerWatch>();
      protected List<PeriodicTimerWatch> GlobalWatchesSorted = new List<PeriodicTimerWatch>();
      protected object GlobalWatchesLock = new object();

      private bool Initialized = false;
      private object LockInit = new object();

      /// <summary>
      /// The default timeout to check all timers even if none have fired
      /// </summary>
      public int TimerCheck = 10000;
      public int SystemTimerInaccuracyTimeMs = 2; /// the inaccuracy of system wait events, in ms
      /// The greater this is, the more CPU we'll spend waiting, so
      /// change accordingly

      public readonly int AvgJitterMs = 0;

      public void Cancel(QuickTimer timer)
      {
         lock (GlobalWatchesLock)
         {
            if (GlobalWatches.ContainsKey(timer.Period) == true)
            {
               timer.watch = GlobalWatches[timer.Period];
               timer.watch.RemoveTimer(timer);

               /// Remove this watch if we have no one else waiting;
               if (timer.watch.Count <= 0)
               {
                  GlobalWatches.Remove(timer.Period);
                  GlobalWatchesSorted.Remove(timer.watch);
               }
            }
         }
      }


      public IMediaTimer CreateTimer(int nMilliseconds, DelegateTimerFired del, string strGuid, ILogInterface logmgr)
      {
         lock (LockInit)
         {
            if (Initialized == false)
            {
               PrepareStuff();
               Initialized = true;
            }
         }

         PeriodicTimerWatch watch = null;
         lock (GlobalWatchesLock)
         {
            if (GlobalWatches.ContainsKey(nMilliseconds) == false)
            {
               watch = new PeriodicTimerWatch(nMilliseconds);
               GlobalWatches.Add(nMilliseconds, watch);
               GlobalWatchesSorted.Add(watch);

               foreach (PeriodicTimerWatch nextwatch in GlobalWatchesSorted)
                   nextwatch.LockTimeForSort();
               GlobalWatchesSorted.Sort();

               EventNewTimer.Set();
            }
            else
            {
               watch = GlobalWatches[nMilliseconds];
            }
         }

         QuickTimer objNewTimer = new QuickTimer(watch, nMilliseconds, del, strGuid, logmgr);
         watch.AddTimer(objNewTimer);

         return objNewTimer;
      }

      public IMediaTimer CreateTimer(int nMilliseconds, DelegateTimerFired del, string strGuid, ILogInterface logmgr, int nAvgDevMs)
      {
         lock (LockInit)
         {
            if (Initialized == false)
            {
               PrepareStuff();
               Initialized = true;
            }
         }

         PeriodicTimerWatch watch = null;
         lock (GlobalWatchesLock)
         {
            if (GlobalWatches.ContainsKey(nMilliseconds) == false)
            {
               watch = new PeriodicTimerWatch(nMilliseconds);
               GlobalWatches.Add(nMilliseconds, watch);
               GlobalWatchesSorted.Add(watch);

               foreach (PeriodicTimerWatch nextwatch in GlobalWatchesSorted)
                   nextwatch.LockTimeForSort();
               GlobalWatchesSorted.Sort();

               EventNewTimer.Set();
            }
            else
            {
               watch = GlobalWatches[nMilliseconds];
            }
         }

         QuickTimer objNewTimer = new QuickTimer(watch, nMilliseconds, del, strGuid, logmgr, nAvgDevMs);
         watch.AddTimer(objNewTimer);

         return objNewTimer;
      }


      private System.Threading.AutoResetEvent EventNewTimer = new AutoResetEvent(false);
      private Thread WorkerThread;
      private void PrepareStuff()
      {
         WorkerThread = new Thread(new ThreadStart(CheckTimerThread));
         WorkerThread.IsBackground = true;
#if !WINDOWS_PHONE
          WorkerThread.Priority = ThreadPriority.AboveNormal;
#endif
         WorkerThread.Start();
      }

      public System.Threading.ThreadState QueryThreadState
      {
         get
         {
            return WorkerThread.ThreadState;
         }
      }
      

      private void CheckTimerThread()
      {
#if !MONO
          timeBeginPeriod(1);
#endif

         Stopwatch watch = new Stopwatch();
         watch.Start();
         long nNextDueTimeInMs = 0;

#if !WINDOWS_PHONE
          System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
#endif

         while (true)
         {
            int nHandle = WaitHandle.WaitTimeout;

            /// See how long until the next timer is due

            if (nNextDueTimeInMs < 0)
               nNextDueTimeInMs = 0;
            else if (nNextDueTimeInMs > TimerCheck)
               nNextDueTimeInMs = TimerCheck;

            //Thread.SpinWait(
            if (nNextDueTimeInMs > (PeriodicTimerWatch.AccuracyAndLag + SystemTimerInaccuracyTimeMs))
            {
#if !WINDOWS_PHONE
                nHandle = WaitHandle.WaitAny(new WaitHandle[] { EventNewTimer }, (int)(nNextDueTimeInMs - SystemTimerInaccuracyTimeMs), true);
#else
                nHandle = WaitHandle.WaitAny(new WaitHandle[] { EventNewTimer }, (int)(nNextDueTimeInMs - SystemTimerInaccuracyTimeMs));
#endif
            }
            else if (nNextDueTimeInMs > 0)
            {
                /// Loop instead of waiting at this point until our next time out
                while (watch.ElapsedMilliseconds < nNextDueTimeInMs)
                {
                    /// Do nothing but take up CPU
                    /// 
                    /// Changed by B.B. 10-28-2009
                    /// We don't really need this to be super accurate, so we'll sleep a little here and see if it has
                    /// any negative affects
                    /// 
                    Thread.Sleep(1);

                }
            }

            try
            {

               if (nHandle == WaitHandle.WaitTimeout)  // Timer elapsed
               {
                  List<PeriodicTimerWatch> alTimersFire = new List<PeriodicTimerWatch>();
                  lock (GlobalWatchesLock)
                  {
                     if (GlobalWatchesSorted.Count == 0)  /// no timers, no need to check until we get signaled
                     {
                        nNextDueTimeInMs = TimerCheck;
                        watch.Reset();
                        watch.Start();
                        continue;
                     }

                     foreach (PeriodicTimerWatch nextTimer in GlobalWatchesSorted)
                     {
                        if (nextTimer.Expired == true)
                        {
                           alTimersFire.Add(nextTimer);
                        }
                        else
                        {
                           nNextDueTimeInMs = nextTimer.GetTimeToNextFireMs();
                           watch.Reset();
                           watch.Start();
                           break;
                        }
                     }

                     if (alTimersFire.Count > 0)
                     {
                        foreach (PeriodicTimerWatch nextTimer in alTimersFire)
                           nextTimer.Fire();

                        lock (GlobalWatchesLock)
                        {
                           foreach (PeriodicTimerWatch nextwatch in GlobalWatchesSorted)
                               nextwatch.LockTimeForSort();
                           GlobalWatchesSorted.Sort();  /// Resort
                                                         /// 
                           if (GlobalWatchesSorted.Count > 0)
                           {
                              nNextDueTimeInMs = GlobalWatchesSorted[0].GetTimeToNextFireMs();
                              watch.Reset();
                              watch.Start();
                           }
                           EventNewTimer.Reset(); // we are again sorted... ignore added event 
                        }
                     }
                  }
               }
               else if (nHandle == 0) /// new item added or removed
               {
                  lock (GlobalWatchesLock)
                  {
                     if (GlobalWatchesSorted.Count > 0)
                     {
                        nNextDueTimeInMs = GlobalWatchesSorted[0].GetTimeToNextFireMs();
                        watch.Reset();
                        watch.Start();

                     }
                     else
                     {
                        nNextDueTimeInMs = TimerCheck;
                        watch.Reset();
                        watch.Start();

                     }

                  }
               }

            }
            catch (System.Exception)
            {
            }
         }
      }
   }


   /// <summary>
   /// Creates a QuickTimer for each CPU on the system
   /// </summary>
   public class QuickTimerControllerCPU
   {
      static int m_MaxThreads = System.Environment.ProcessorCount;
      public static int MaxThreads
      {
         get
         {
            return m_MaxThreads;
         }
         set  
         {
            m_MaxThreads = value;
         }
      }



      static QuickTimerControllerCPU()
      {
         for (int i = 0; i < MaxThreads; i++)
         {
            QuickTimerController newcontroller = new QuickTimerController();
            Timers.Add(newcontroller);
         }

      }

      public static IMediaTimer CreateTimer(int nMilliseconds, DelegateTimerFired del, string strGuid, ILogInterface logmgr)
      {
         int nIndex = 0;
         lock (LockCurrentTimerThread)
         {
            CurrentTimerThread++;
            if (CurrentTimerThread > (Timers.Count - 1))
               CurrentTimerThread = 0;
            nIndex = CurrentTimerThread;
         }
         return Timers[nIndex].CreateTimer(nMilliseconds, del, strGuid, logmgr);
      }

      public static IMediaTimer CreateTimer(int nMilliseconds, DelegateTimerFired del, string strGuid, ILogInterface logmgr, int nAvgDevMs)
      {
         int nIndex = 0;
         lock (LockCurrentTimerThread)
         {
            CurrentTimerThread++;
            if (CurrentTimerThread > (Timers.Count - 1))
               CurrentTimerThread = 0;
            nIndex = CurrentTimerThread;
         }
         return Timers[nIndex].CreateTimer(nMilliseconds, del, strGuid, logmgr, nAvgDevMs);
      }

      static List<QuickTimerController> Timers = new List<QuickTimerController>();

      static int CurrentTimerThread = 0;
      static object LockCurrentTimerThread = new object();

   }

}
