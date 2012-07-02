using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel;

namespace RTP
{
   public class RTPPacketBuffer : IComparer<RTPPacket>, INotifyPropertyChanged
   {
      public RTPPacketBuffer()
      {
      }
      public RTPPacketBuffer(int nPacketQueueMinimumSize)
      {
         InitialPacketQueueMinimumSize = nPacketQueueMinimumSize;
         if (InitialPacketQueueMinimumSize < 0)
            throw new Exception("Packet queue size must be greater than zero");
      }

      public override string ToString()
      {
         return string.Format("RTPPacketBuffer, Current Size {0}, Last Received Sequence: {1}", m_nCurrentQueueSize, m_nLastReceivedSequence);
      }

      private int m_nInitialPacketQueueMinimumSize = 2;
      public int InitialPacketQueueMinimumSize
      {
          get { return m_nInitialPacketQueueMinimumSize; }
          set { m_nInitialPacketQueueMinimumSize = value; CurrentPacketQueueMinimumSize = value; }
      }

      private int m_nPacketQueueMinimumSize = 2;
      public int CurrentPacketQueueMinimumSize
      {
        get { return m_nPacketQueueMinimumSize; }
        set { m_nPacketQueueMinimumSize = value; CurrentMaxQueueSize = (value * 3);  }
      }

      public int PacketSizeShiftMax = 4; // allow the PacketQueueMinimumSize and MaxQueueSize to grow by 4 if needed

    
      private int m_nMaxQueueSize = 4;
      protected int CurrentMaxQueueSize
      {
        get { return m_nMaxQueueSize; }
        set { m_nMaxQueueSize = value; }
      }

      List<RTPPacket> Packets = new List<RTPPacket>();
      object PacketLock = new object();

      public void Clear()
      {
         lock (PacketLock)
         {
            Packets.Clear();
         }
      }

      private int m_nCurrentQueueSize = 0;

      public int CurrentQueueSize
      {
         get { return m_nCurrentQueueSize; }
         protected set 
         { 
            if (m_nCurrentQueueSize != value)
            {
              m_nCurrentQueueSize = value; 
               FirePropertyChanged("CurrentQueueSize");
            }
         }
      }

      private int m_nUnavailablePackets = 0;

      /// <summary>
      /// This doesn't really work because we don't push packets on a timer in our audio graph.  If a packet is unavailble, a filter down the line just generates silence
      /// </summary>
      public int UnavailablePackets
      {
         get { return m_nUnavailablePackets; }
         protected set { m_nUnavailablePackets = value; }
      }


      private int m_nDiscardedPackets = 0;

      public int DiscardedPackets
      {
         get { return m_nDiscardedPackets; }
         protected set { m_nDiscardedPackets = value; }
      }


      public void ResetStats()
      {
         UnavailablePackets = 0;
         DiscardedPackets = 0;
      }

      public void Reset()
      {
          ResetStats();
          m_bHaveSetInitialSequence = false;
          m_nNextExpectedSequence = 0xFFFF;
          m_nLastReceivedSequence = 0xFFFF;
          CurrentPacketQueueMinimumSize = InitialPacketQueueMinimumSize;
          m_nTotalPackets = 0;
          FirstPacketTime = DateTime.Now;
          lock (PacketLock)
          {
              Packets.Clear();
          }
      }

      bool m_bHaveSetInitialSequence = false;
      ushort m_nNextExpectedSequence = 0xFFFF;
      ushort m_nLastReceivedSequence = 0xFFFF;
      private uint m_nTotalPackets = 0;
      public uint TotalPackets
      {
          get { return m_nTotalPackets; }
          set { m_nTotalPackets = value; }
      }

      private DateTime m_dtFirstPacket = DateTime.MinValue;

      public DateTime FirstPacketTime
      {
          get { return m_dtFirstPacket; }
          set { m_dtFirstPacket = value; }
      }

      public TimeSpan Duration
      {
          get
          {
              if (m_dtFirstPacket == DateTime.MinValue)
                  return TimeSpan.Zero;

              return DateTime.Now - m_dtFirstPacket;
          }
      }

      public double AverageInterPacketTimeMs
      {
          get
          {
              if (m_nTotalPackets <= 1)
                  return 0.0f;
              TimeSpan duration = Duration;
              return duration.TotalMilliseconds / m_nTotalPackets;
          }
      }

      public string Statistics
      {
          get
          {
              if (m_nTotalPackets == 0)
                  return "none";

              double fPercent = ((double)(DiscardedPackets*100.0f)) / (double)m_nTotalPackets;
              return string.Format("Loss: {0}, Total: {1}, Discarded: {2}, NA: {3}, Size: {4} in {5}-{6}", fPercent.ToString("N2"), m_nTotalPackets, DiscardedPackets, UnavailablePackets, Packets.Count, this.CurrentPacketQueueMinimumSize, this.CurrentMaxQueueSize);

          }
      }

      protected System.Threading.ManualResetEvent NewPacketEvent = new System.Threading.ManualResetEvent(false);

      DateTime dtLastAdded = DateTime.Now;
      /// <summary>
      /// Adds a packet to our buffer
      /// </summary>
      /// <param name="packet"></param>
      public void AddPacket(RTPPacket packet)
      {
         m_nTotalPackets++;
         m_nLastReceivedSequence = packet.SequenceNumber;
         if (m_bHaveSetInitialSequence == false)
         {
             m_nNextExpectedSequence = packet.SequenceNumber;
             m_bHaveSetInitialSequence = true;
         }

         int nNewSize = 0;
         lock (PacketLock)
         {
            /// If packet is before the last one we've given out, discard it, we already assumed it was lost
            /// 
            int nSequenceCompare = CompareSequence(packet.SequenceNumber, m_nNextExpectedSequence);
            if (nSequenceCompare < 0)
            {
               System.Diagnostics.Debug.WriteLine("Discarding packet with seq {0}, because it's less than {1}", packet.SequenceNumber, m_nNextExpectedSequence);
               DiscardedPackets++;
               return;
            }

            Packets.Add(packet);

            //DateTime dtNow = DateTime.Now;
            //TimeSpan tsDif = dtNow - dtLastAdded;
            //dtLastAdded = dtNow;
            //System.Diagnostics.Debug.WriteLine("Adding packet {0}, dif is {1} ms", packet, tsDif.TotalMilliseconds);
            Packets.Sort(this);

            if (Packets.Count > CurrentMaxQueueSize)
            {
                while (Packets.Count > CurrentPacketQueueMinimumSize)
                {
                    RTPPacket firstpacket = Packets[0];
                    Packets.RemoveAt(0);
                    System.Diagnostics.Debug.WriteLine("Discarding overflow packet {0}, QueueCount is {1} (over {2})", firstpacket, Packets.Count+1, CurrentMaxQueueSize);
                    DiscardedPackets++;
                }
            }

            nNewSize = Packets.Count;
            NewPacketEvent.Set();
         }
         CurrentQueueSize = nNewSize;
         
      }

      System.Diagnostics.Stopwatch WaitPacketWatch = new System.Diagnostics.Stopwatch();
      /// <summary>
      ///  Wait for the next packet in our sequence for the specified number of ms;
      /// </summary>
      /// <param name="nMsWait"></param>
      /// <returns></returns>
      public RTPPacket WaitPacket(int nMsWait, out int nMsTook)
      {
#if WINDOWS_PHONE
          WaitPacketWatch.Reset();
          WaitPacketWatch.Start();
#else
          WaitPacketWatch.Restart();
#endif
          while (true)
          {
              RTPPacket packet = GetPacketInternal();
              if (packet != null)
              {
                  nMsTook = (int) WaitPacketWatch.ElapsedMilliseconds;
                  return packet;
              }

              int nWait = nMsWait - (int)WaitPacketWatch.ElapsedMilliseconds;
              if (nWait <= 0)
              {
                  SetPacketUnavailable();
                  nMsTook = (int)WaitPacketWatch.ElapsedMilliseconds;
                  return null;
              }

              if (NewPacketEvent.WaitOne(nWait) == false)
              {
                  SetPacketUnavailable();
                  nMsTook = (int)WaitPacketWatch.ElapsedMilliseconds;
                  return null;
              }
          }
          
      }

       /// <summary>
       /// A packet was unavaible when it should have been.  If we are below our minimum queue size, we won't return any packets
       /// until we get to it - also, increase the queue size
       /// </summary>
      void SetPacketUnavailable()
      {
          if (this.m_nTotalPackets <= 0)
              return;
          if (m_bHaveSetInitialSequence == false)
              return;

          m_nCorrectOrientedPackets = 0;
          UnavailablePackets++;

          if (m_bWaitingForQueueToGrow == true) // we are still waiting for our queue to reach the initial size, so don't resize yet
              return;


          m_bWaitingForQueueToGrow = true;

          if (Packets.Count < CurrentPacketQueueMinimumSize) // don't resize the min packet queue unless we were at that size and couldn't find the right packet
          {
              System.Diagnostics.Debug.WriteLine("***RTPPacket not available (Not increasing size) Size is {0}, sequence expected is {1}, Queue size is {2}", CurrentQueueSize, m_nNextExpectedSequence, Packets.Count);
              return;
          }

          if (CurrentPacketQueueMinimumSize < (this.InitialPacketQueueMinimumSize + this.PacketSizeShiftMax))
          {
              CurrentPacketQueueMinimumSize++;
              System.Diagnostics.Debug.WriteLine("**********Increasing jitter buffer size to {0}=>{1}", CurrentPacketQueueMinimumSize, CurrentMaxQueueSize);
          }

          
      }

      protected int m_nCorrectOrientedPackets = 0;
      protected bool m_bWaitingForQueueToGrow = false;
      protected bool IsQueueLargeEnough()
      {
          if (m_bWaitingForQueueToGrow == false)
              return true;
          else if (Packets.Count >= CurrentPacketQueueMinimumSize)
          {
              m_bWaitingForQueueToGrow = false;
              return true;
          }
          return false;
      }

      public RTPPacket GetPacket()
      {
          RTPPacket packet = GetPacketInternal();
          if (packet == null)
              SetPacketUnavailable();
          return packet;
      }

      /// <summary>
      /// Gets the next ordered packet from our buffer, or null if none are available
      /// </summary>
      /// <returns></returns>
      protected RTPPacket GetPacketInternal()
      {
          RTPPacket packetret = null;
          int nNewSize = 0;

          lock (PacketLock)
          {
              NewPacketEvent.Reset();

              if (Packets.Count <= 0)
                  return null;

              if (IsQueueLargeEnough() == false) ///we may be growing our queue because of unavailable packets, take the audio hit all at once
                  return null;

              if ((m_nCorrectOrientedPackets > 500) && (this.CurrentPacketQueueMinimumSize > InitialPacketQueueMinimumSize) && (Packets.Count > 1))
              {
                  // we can move our packet queue buffer back down because we've received enough packets in the correct order to have faith in the network connnection
                  // do this to decrease audio latency
                  m_nCorrectOrientedPackets = 0;
                  CurrentPacketQueueMinimumSize--;
                  System.Diagnostics.Debug.WriteLine("**********Decreasing jitter buffer size to {0}=>{1}", CurrentPacketQueueMinimumSize, CurrentMaxQueueSize);

                  Packets.RemoveAt(0);
                  m_nNextExpectedSequence = Packets[0].SequenceNumber;
              }


              packetret = Packets[0];
              int nSequenceCompare = CompareSequence(packetret.SequenceNumber, m_nNextExpectedSequence);


              if (nSequenceCompare > 0) //(packetret.SequenceNumber > m_nNextExpectedSequence)
              {
                  m_nCorrectOrientedPackets = 0;
                  Packets.RemoveAt(0);
                  m_nNextExpectedSequence = RTPPacket.GetNextSequence(packetret.SequenceNumber);

                  /// Increase our minimum size because our buffer is not big enough to handle the jitter if we can't find the right packet within it
                  /// 
                  if (CurrentPacketQueueMinimumSize < (this.InitialPacketQueueMinimumSize + this.PacketSizeShiftMax))
                  {
                      CurrentPacketQueueMinimumSize++;
                      System.Diagnostics.Debug.WriteLine("**********Increasing jitter buffer size to {0}=>{1}", CurrentPacketQueueMinimumSize, CurrentMaxQueueSize);
                      m_bWaitingForQueueToGrow = true;
                  }

              }
              else if (nSequenceCompare == 0) //(packetret.SequenceNumber == m_nNextExpectedSequence)
              {
                  m_nCorrectOrientedPackets++;
                  Packets.RemoveAt(0);
                  m_nNextExpectedSequence = RTPPacket.GetNextSequence(packetret.SequenceNumber);
                  //System.Diagnostics.Debug.WriteLine("MATCH - retrieving packet {0}", packetret);
              }
              else
              {
                  /// packet sequence is before the expected value... should never happen
                  /// 
                  Packets.RemoveAt(0);
                  System.Diagnostics.Debug.Assert(true);
                  packetret = null;
              }

              //if (packetret.SequenceNumber <= m_nNextExpectedSequence) /// Packet is the expected one, or before the expected one... should never happen since we don't add packets before the next expected one
              //{
              //    Packets.RemoveAt(0);
              //    m_nNextExpectedSequence = (ushort)(packetret.SequenceNumber + 1);
              //}
              //else if (Packets.Count == MaxQueueSize) // May have lost the desired packet.  We've waited all we can, get the lowest packet number
              //{
              //    Packets.RemoveAt(0);
              //    m_nNextExpectedSequence = (ushort)(packetret.SequenceNumber + 1);
              //}

              nNewSize = Packets.Count;
          }
          CurrentQueueSize = nNewSize;
          return packetret;
      }


      

      #region IComparer<RTPPacket> Members

      public int Compare(RTPPacket x, RTPPacket y)
      {
         int nXSeq = x.SequenceNumber;
         int nYSeq = y.SequenceNumber;
         return CompareSequence(nXSeq, nYSeq);
      }

      public int CompareSequence(int nXSeq, int nYSeq)
      {
         if (Math.Abs(nXSeq - nYSeq) > 60000) // we rolled around, add to our zero index so it appears in order
         {
            if (nXSeq < 5000)
               nXSeq += ushort.MaxValue;
            if (nYSeq < 5000)
               nYSeq += ushort.MaxValue;
         }
         return nXSeq.CompareTo(nYSeq);
      }

      #endregion

       #region INotifyPropertyChanged Members

      public event PropertyChangedEventHandler PropertyChanged;
      void FirePropertyChanged(string strName)
      {
         if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(strName));
      }

      #endregion
   }
}
