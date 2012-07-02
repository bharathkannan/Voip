/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Net;
using System.Net.Sockets;
using AudioClasses;
using SocketServer;

namespace RTP
{
    public delegate void DelegateSTUNMessage(STUNMessage smsg, IPEndPoint epfrom);


    public class STUNRequestResponse
    {
        public STUNRequestResponse(STUNMessage requestmessage)
        {
            RequestMessage = requestmessage;
        }

        private STUNMessage m_objRequestMessage = null;

        public STUNMessage RequestMessage
        {
            get { return m_objRequestMessage; }
            set { m_objRequestMessage = value; }
        }

        private STUNMessage m_objResponseMessage = null;
        public STUNMessage ResponseMessage
        {
          get { return m_objResponseMessage; }
          set { m_objResponseMessage = value; }
        }

        public bool IsThisYou(byte [] bTransactionId)
        {
            return SocketServer.TLS.ByteHelper.CompareArrays(RequestMessage.TransactionId, bTransactionId);
        }

        public bool IsThisYourResponseSetIfItIs(STUNMessage msg)
        {
            bool bRet = SocketServer.TLS.ByteHelper.CompareArrays(RequestMessage.TransactionId, msg.TransactionId);
            if (bRet == true)
            {
                ResponseMessage = msg;
                try
                {
                    if (WaitHandle != null)
                        WaitHandle.Set();
                }
                catch (Exception ex)
                {
                    WaitHandle = null;
                }
            }
            return bRet; 
        }

        public void Reset(STUNMessage requestmessage)
        {
            RequestMessage = requestmessage;
            try
            {
                if (WaitHandle != null)
                    WaitHandle.Close();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                WaitHandle = null;
            }
            WaitHandle = new System.Threading.ManualResetEvent(false);
            ResponseMessage = null;
        }

        public bool WaitForResponse(int nTimeOut)
        {
            bool bRet = false;
            if (WaitHandle != null)
            {
                bRet = WaitHandle.WaitOne(nTimeOut);
                try
                {
                    WaitHandle.Close();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    WaitHandle = null;
                }

            }

            return bRet;
        }

        protected System.Threading.ManualResetEvent WaitHandle = new System.Threading.ManualResetEvent(false);
    }

    public class RTPStream
    {
        public RTPStream(byte nPayload)
        {
            Payload = nPayload;
            SSRC = (uint) ran.Next();
            Sequence = (ushort) ran.Next(ushort.MaxValue);
        }
        static Random ran = new Random();

        private uint m_nReceiveSSRC = 0;
        public uint ReceiveSSRC
        {
            get { return m_nReceiveSSRC; }
            protected set { m_nReceiveSSRC = value; }
        }

        private uint m_nSSRC = 0;

        public uint SSRC
        {
            get { return m_nSSRC; }
            set { m_nSSRC = value; }
        }
        private IPEndPoint m_objDestinationEndpoint = new IPEndPoint(IPAddress.Any, 0);

        public IPEndPoint DestinationEndpoint
        {
            get { return m_objDestinationEndpoint; }
            set { m_objDestinationEndpoint = value; }
        }

       private IPEndPoint m_objLocalEndpoint = new IPEndPoint(IPAddress.Any, 0);

        public IPEndPoint LocalEndpoint
        {
            get { return m_objLocalEndpoint; }
            set { m_objLocalEndpoint = value; }
        }

        protected SocketServer.UDPSocketClient RTPUDPClient = null;

        protected object SocketLock = new object();

        private bool m_bIsActive =  false;

        protected bool IsActive
        {
          get { return m_bIsActive; }
          set { m_bIsActive = value; }
        }

        private bool m_bIsBound = false;
        public bool IsBound
        {
            get { return m_bIsBound; }
            set { m_bIsBound = value; }
        }

        protected int m_nPTimeReceive = 20;

        public virtual int PTimeReceive
        {
          get { return m_nPTimeReceive; }
          set 
          { 
              m_nPTimeReceive = value; 
          }
        }

        protected int m_nPTimeTransmit = 20;
        public virtual int PTimeTransmit
        {
            get { return m_nPTimeTransmit; }
            set { m_nPTimeTransmit = value; }
        }
        
        protected IMediaTimer SendTimer = null;
        protected IMediaTimer ExpectPacketTimer = null;
        protected object TimerLock = new object();

        public void Bind(IPEndPoint localEp)
        {
            if (IsBound == false)
            {
                LocalEndpoint = localEp;
                RTPUDPClient = new SocketServer.UDPSocketClient(LocalEndpoint);
                RTPUDPClient.Bind();

#if !WINDOWS_PHONE
                LocalEndpoint = RTPUDPClient.s.LocalEndPoint as IPEndPoint;
#endif
                IsBound = true;
                RTPUDPClient.OnReceiveMessage += new SocketServer.UDPSocketClient.DelegateReceivePacket(RTPUDPClient_OnReceiveMessage);

                //I changed Bind this line should be uncommented
            //    RTPUDPClient.StartReceiving();
               // 

            }
        }

        public event DelegateSTUNMessage OnUnhandleSTUNMessage = null;

        protected List<STUNRequestResponse> StunRequestResponses = new List<STUNRequestResponse>();
        protected object StunLock = new object();

        public STUNMessage SendRecvSTUN(EndPoint epStun, STUN2Message msgRequest, int nTimeout)
        {
            STUNRequestResponse req = new STUNRequestResponse(msgRequest);
            lock (StunLock)
            {
                StunRequestResponses.Add(req);
            }

            SendSTUNMessage(msgRequest, epStun);

            bool bResponse = req.WaitForResponse(nTimeout);
            lock (StunLock)
            {
                if (StunRequestResponses.Contains(req) == true)
                    StunRequestResponses.Remove(req);
            }
            //RTPUDPClient.StopReceiving();
            return req.ResponseMessage;
        }

        public STUNMessage SendRecvSTUN1(EndPoint epStun, STUN2Message msgRequest, int nTimeout)
        {
            STUNRequestResponse req = new STUNRequestResponse(msgRequest);
            lock (StunLock)
            {
                StunRequestResponses.Add(req);
            }

            SendSTUNMessage(msgRequest, epStun);

            bool bResponse = req.WaitForResponse(nTimeout);
            lock (StunLock)
            {
                if (StunRequestResponses.Contains(req) == true)
                    StunRequestResponses.Remove(req);
            }
            
            return req.ResponseMessage;
        }

        public int SendSTUNMessage(STUNMessage msg, EndPoint epStun)
        {
            byte[] bMessage = msg.Bytes;
            return this.RTPUDPClient.SendUDP(bMessage, bMessage.Length, epStun);
        }

    

        public virtual bool Start(IPEndPoint destinationEp, int nPacketTimeTx, int nPacketTimeRx)
        {
            if (IsActive == true)
                return false;
            if (IsBound == false)
                throw new Exception("You first must bind the local socket by calling Bind()");

            Reset();
            DestinationEndpoint = destinationEp;
            PTimeReceive = nPacketTimeRx;
            PTimeTransmit = nPacketTimeTx;

            if (UseInternalTimersForPacketPushPull == true)
            {
                SendTimer = SocketServer.QuickTimerControllerCPU.CreateTimer(PTimeTransmit, new SocketServer.DelegateTimerFired(OnTimeToPushPacket), "", null);
                ExpectPacketTimer = SocketServer.QuickTimerControllerCPU.CreateTimer(PTimeReceive, new SocketServer.DelegateTimerFired(OnTimeToForwardPacket), "", null);
            }

                   // byte[] test = new byte[100];
           // RTPUDPClient.SendUDP(test, test.Length, new IPEndPoint(IPAddress.Parse("172.16.41.174"), 4507));
            IsActive = true;
            RTPUDPClient.StartReceiving();
            return true;
        }

        private bool m_bUseInternalTimersForPacketPushPull = true;

        public bool UseInternalTimersForPacketPushPull
        {
            get { return m_bUseInternalTimersForPacketPushPull; }
            set { m_bUseInternalTimersForPacketPushPull = value; }
        }

        public virtual void StopSending()
        {
            if (IsActive == false)
                return;

            IsActive = false;
            IsBound = false;
            if (SendTimer != null)
            {
                SendTimer.Cancel();
                SendTimer = null;
            }
            if (ExpectPacketTimer != null)
            {
                ExpectPacketTimer.Cancel();
                ExpectPacketTimer = null;
            }
            RTPUDPClient.StopReceiving();
            RTPUDPClient.OnReceiveMessage -= new UDPSocketClient.DelegateReceivePacket(RTPUDPClient_OnReceiveMessage);
            RTPUDPClient = null;
        }

        void OnTimeToPushPacket(IMediaTimer timer)
        {
            SendNextPacket();
        }

        /// Time to decode the next packet and add the audio to our outgoing audio buffer
        /// 
        void OnTimeToForwardPacket(IMediaTimer timer)
        {
            PushNextPacket();
        }

        void  RTPUDPClient_OnReceiveMessage(byte[] bData, int nLength, IPEndPoint epfrom, IPEndPoint epthis, DateTime dtReceived)
        {
            /// if we are an performing ICE, see if this is an ICE packet instead of an RTP one
            if (nLength >= 8)
            {
                //0x2112A442
                if ((bData[4] == 0x21) && (bData[5] == 0x12) && (bData[6] == 0xA4) && (bData[7] == 0x42))
                {
                    /// STUN message
                    STUNMessage smsg = new STUN2Message();
                    byte[] bStun = new byte[nLength];
                    Array.Copy(bData, 0, bStun, 0, nLength);
                    try
                    {
                        smsg.Bytes = bStun;
                    }
                    catch (Exception ex)
                    {
                        smsg = new STUNMessage();
                        smsg.Bytes = bStun;
                    }

                    STUNRequestResponse foundreq = null;
                    lock (StunLock)
                    {
                        foreach (STUNRequestResponse queuedreq in StunRequestResponses)
                        {
                            if (queuedreq.IsThisYourResponseSetIfItIs(smsg) == true)
                            {
                                foundreq = queuedreq;
                                break;
                            }
                        }

                        if (foundreq != null)
                        {
                            StunRequestResponses.Remove(foundreq);
                            return;
                        }
                    }

                    if (OnUnhandleSTUNMessage != null)
                        OnUnhandleSTUNMessage(smsg, epfrom);
                    return;
                }
            }

 	        RTPPacket packet = RTPPacket.BuildPacket(bData, 0, nLength);
            if (packet != null) /// Seems we get some TURN channel data messages from google talk
            {
                if (ReceiveSSRC == 0)
                    ReceiveSSRC = packet.SSRC;
                if ((packet.PayloadType == this.Payload) && (packet.SSRC == this.ReceiveSSRC))
                {
                    IncomingRTPPacketBuffer.AddPacket(packet);
                }
            }
        }

        public RTPPacketBuffer IncomingRTPPacketBuffer = new RTPPacketBuffer(2);

        protected virtual void SendNextPacket()
        {
        }

        public virtual void SendNextSample(byte[] bData)
        {
        }



        /// <summary>
        /// Pushes the next available packet to the audio/video receive buffer
        /// </summary>
        protected virtual void PushNextPacket()
        {
        }

        public virtual byte[] GetNextPacketSample(bool bReturnArrayOnNA)
        {
            return null;
        }

      
     
        private byte m_nPayload = 9;

        public byte Payload
        {
            get { return m_nPayload; }
            set { m_nPayload = value; }
        }

        private ushort m_nSequence = 0;

        protected ushort Sequence
        {
            get { return m_nSequence; }
            set { m_nSequence = value; }
        }

        private uint m_nTimeStamp = 0;

        protected uint TimeStamp
        {
            get { return m_nTimeStamp; }
            set { m_nTimeStamp = value; }
        }

        public void Reset()
        {
            Random ran = new Random();
            this.IncomingRTPPacketBuffer.Reset();
            ReceiveSSRC = 0;
            SSRC = (uint)ran.Next();
            m_nSequence = (ushort) ran.Next(ushort.MaxValue);
            m_nTimeStamp = 0;
        }

        /// <summary>
        /// Get the next timestamp.  Audio is usually in ptime*8 increments, video is in offsetfromstart in seconds*90000
        /// </summary>
        /// <returns></returns>
        public virtual uint GetNextTimeStamp()
        {
            uint nRet = m_nTimeStamp;
            m_nTimeStamp += (uint)(this.PTimeTransmit* 8);
            return nRet;
        }

        protected virtual void FormatNextPacket( RTPPacket packet)
        {
            packet.SSRC = m_nSSRC;
            packet.TimeStamp = GetNextTimeStamp();
            packet.PayloadType = this.Payload;
            packet.Marker = (m_nSequence == 0) ? true : false;

            m_nSequence = RTPPacket.GetNextSequence(m_nSequence);
            packet.SequenceNumber = m_nSequence;
        }
    }


}
