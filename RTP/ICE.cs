/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.XMPP.Jingle;
using System.Net;

namespace RTP
{
    /// <summary>
    ///  Interactive Connectivity Establishment (RFC5245) stuff
    /// </summary>
    class ICE
    {
    }

    public enum CandidatePairState
    {
        Waiting,
        InProgress,
        Succeeded,
        Failed,
        Frozen
    }

    public class CandidatePair
    {
        public CandidatePair(Candidate local, Candidate remote, bool bControlling)
        {
            IsControlling = bControlling;
            LocalCandidate = local;
            RemoteCandidate = remote;

            if (IsControlling == true)
                this.Priority = (int) ComputerPairPriority(local.priority, remote.priority);
            else
                this.Priority = (int) ComputerPairPriority(remote.priority, local.priority);

        }

        private bool m_bIsControlling = false;
        public bool IsControlling
        {
            get { return m_bIsControlling; }
            set { m_bIsControlling = value; }
        }

        private Candidate m_objLocalCandidate = null;
        public Candidate LocalCandidate
        {
            get { return m_objLocalCandidate; }
            set { m_objLocalCandidate = value; }
        }

        private Candidate m_objRemoteCandidate = null;
        public Candidate RemoteCandidate
        {
            get { return m_objRemoteCandidate; }
            set { m_objRemoteCandidate = value; }
        }

        private CandidatePairState m_eCandidatePairState = CandidatePairState.Frozen;

        public CandidatePairState CandidatePairState
        {
            get { return m_eCandidatePairState; }
            set { m_eCandidatePairState = value; }
        }

        int m_nPriority = 0;
        public int Priority
        {
            get
            {
                return m_nPriority;
            }
            protected set
            {
                m_nPriority = value;
            }

        }

        public int Foundation
        {
            get
            {
                return Convert.ToInt32(LocalCandidate.foundation) + Convert.ToInt32(RemoteCandidate.foundation);
            }
        }

        public static long ComputerPairPriority(int G_PriorityControlling, int D_PriorityControlled)
        {
            long nPriority = (((long)Math.Min(G_PriorityControlling, D_PriorityControlled)) << 32) +
                            Math.Max(G_PriorityControlling, D_PriorityControlled) * 2 +
                            ((G_PriorityControlling > D_PriorityControlled) ? 1 : 0);

            return nPriority;
        }

        public static uint CalculatePriority(int typepref, int localpref, int nComponentid)
        {
            // priority = (2^24)*(type preference) +
            //(2^8)*(local preference) +
            //(2^0)*(256 - component ID)

            uint nPriority = (uint)(((typepref & 0x7E) << 24) | ((localpref & 0xFFFF) << 8) | (256 - nComponentid & 0xFF));
            return nPriority;
        }

        private bool m_bHasReceivedSuccessfulIncomingSTUNCheck = false;
        public bool HasReceivedSuccessfulIncomingSTUNCheck
        {
            get { return m_bHasReceivedSuccessfulIncomingSTUNCheck; }
            set { m_bHasReceivedSuccessfulIncomingSTUNCheck = value; }
        }

        public IPEndPoint ResponseEndpoint = null;

        public void PerformOutgoingSTUNCheck(RTPStream stream, string strUsername, string strPassword)
        {
            STUN2Message msgRequest = new STUN2Message();
            msgRequest.Method = StunMethod.Binding;
            msgRequest.Class = StunClass.Request;


            //MappedAddressAttribute mattr = new MappedAddressAttribute();
            //mattr.IPAddress = LocalCandidate.IPEndPoint.Address;
            //mattr.Port = (ushort)LocalCandidate.IPEndPoint.Port;

            //msgRequest.AddAttribute(mattr);

            PriorityAttribute pattr = new PriorityAttribute();
            pattr.Priority = (int) CalculatePriority(110, 10, this.LocalCandidate.component); ///Peer reflexive, not sure of the purpose of this yet  //this.Priority;
            msgRequest.AddAttribute(pattr);

            if (IsControlling == true)
            {
                IceControllingAttribute cattr = new IceControllingAttribute();
                msgRequest.AddAttribute(cattr);
            }
            else
            {
                IceControlledAttribute cattr = new IceControlledAttribute();
                msgRequest.AddAttribute(cattr);
            }

            if (strUsername != null)
            {
                UserNameAttribute unameattr = new UserNameAttribute();
                unameattr.UserName = strUsername;
                msgRequest.AddAttribute(unameattr);
            }

          

            /// Add message integrity, computes over all the items currently added
            /// 
            int nLengthWithoutMessageIntegrity = msgRequest.Bytes.Length;
            MessageIntegrityAttribute mac = new MessageIntegrityAttribute();
            msgRequest.AddAttribute(mac);
            mac.ComputeHMACShortTermCredentials(msgRequest, nLengthWithoutMessageIntegrity, strPassword);

            /// Add fingerprint
            /// 
            int nLengthWithoutFingerPrint = msgRequest.Bytes.Length;
            FingerPrintAttribute fattr = new FingerPrintAttribute();
            msgRequest.AddAttribute(fattr);
            fattr.ComputeCRC(msgRequest, nLengthWithoutFingerPrint);


            foreach (int nNextTimeout in Timeouts)
            {
                STUNMessage ResponseMessage = stream.SendRecvSTUN(this.RemoteCandidate.IPEndPoint, msgRequest, nNextTimeout);

                ResponseEndpoint = null;
                if (ResponseMessage != null)
                {
                    foreach (STUNAttributeContainer cont in ResponseMessage.Attributes)
                    {
                        if (cont.ParsedAttribute.Type == StunAttributeType.MappedAddress)
                        {

                            MappedAddressAttribute attrib = cont.ParsedAttribute as MappedAddressAttribute;
                            ResponseEndpoint = new IPEndPoint(attrib.IPAddress, attrib.Port);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("STUN check for remote candidate {0} succeeded", this.RemoteCandidate.IPEndPoint);
                    this.CandidatePairState = RTP.CandidatePairState.Succeeded;
                    break;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("STUN check for remote candidate {0} failed", this.RemoteCandidate.IPEndPoint);
                    this.CandidatePairState = RTP.CandidatePairState.Failed;
                }
            }
        }

        public void PerformOutgoingSTUNCheckGoogle(RTPStream stream, string strUsername, string strPassword)
        {
            STUN2Message msgRequest = new STUN2Message();
            msgRequest.Method = StunMethod.Binding;
            msgRequest.Class = StunClass.Request;


            if (strUsername != null)
            {
                UserNameAttribute unameattr = new UserNameAttribute();
                unameattr.UserName = strUsername;
                msgRequest.AddAttribute(unameattr);
            }

            foreach (int nNextTimeout in Timeouts)
            {
                STUNMessage ResponseMessage = stream.SendRecvSTUN(this.RemoteCandidate.IPEndPoint, msgRequest, nNextTimeout);

                ResponseEndpoint = null;
                if (ResponseMessage != null)
                {
                    foreach (STUNAttributeContainer cont in ResponseMessage.Attributes)
                    {
                        if (cont.ParsedAttribute.Type == StunAttributeType.MappedAddress)
                        {

                            MappedAddressAttribute attrib = cont.ParsedAttribute as MappedAddressAttribute;
                            ResponseEndpoint = new IPEndPoint(attrib.IPAddress, attrib.Port);
                        }
                    }
                    this.CandidatePairState = RTP.CandidatePairState.Succeeded;
                    break;
                }
                else
                {
                    this.CandidatePairState = RTP.CandidatePairState.Failed;
                }
            }
        }

        public void TellRemoteEndToUseThisPair(RTPStream stream, string strUsername, string strPassword)
        {
            if (IsControlling == false)
                throw new Exception("Only controlling endpoint can send a usecandidate attribute");

            STUN2Message msgRequest = new STUN2Message();
            msgRequest.Method = StunMethod.Binding;
            msgRequest.Class = StunClass.Request;

            PriorityAttribute pattr = new PriorityAttribute();
            pattr.Priority = (int)CalculatePriority(110, 30, this.LocalCandidate.component); ///Peer reflexive, not sure of the purpose of this yet  //this.Priority;
            msgRequest.AddAttribute(pattr);

            IceControllingAttribute cattr = new IceControllingAttribute();
            msgRequest.AddAttribute(cattr);

            UseCandidateAttribute uattr = new UseCandidateAttribute();
            msgRequest.AddAttribute(uattr);

            if (strUsername != null)
            {
                UserNameAttribute unameattr = new UserNameAttribute();
                unameattr.UserName = strUsername;
                msgRequest.AddAttribute(unameattr);
            }
           

            /// Add message integrity, computes over all the items currently added
            /// 
            int nLengthWithoutMessageIntegrity = msgRequest.Bytes.Length;
            MessageIntegrityAttribute mac = new MessageIntegrityAttribute();
            msgRequest.AddAttribute(mac);
            mac.ComputeHMACShortTermCredentials(msgRequest, nLengthWithoutMessageIntegrity, strPassword);

            /// Add fingerprint
            /// 
            int nLengthWithoutFingerPrint = msgRequest.Bytes.Length;
            FingerPrintAttribute fattr = new FingerPrintAttribute();
            msgRequest.AddAttribute(fattr);
            fattr.ComputeCRC(msgRequest, nLengthWithoutFingerPrint);

            foreach (int nNextTimeout in Timeouts)
            {
                STUNMessage ResponseMessage = stream.SendRecvSTUN(this.RemoteCandidate.IPEndPoint, msgRequest, nNextTimeout);
                if (ResponseMessage != null)
                    break;
            }

        }

        int m_nSendTimeout = 3000;
        bool m_bThreadRunning = false;
        bool m_bGoogle = false;
        RTPStream RTPStream = null;
        System.Threading.Thread ThreadIndication = null;

        /// <summary>
        /// Start the thread that sends STUN requests periodically.  Must clients will still work without this, but google talk will kill incoming audio if
        /// stun binding requests aren't sent periodically.  
        /// (Google clients appear to send these every 500 ms, but we'll do every 3 s)
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bGoogle"></param>
        public void StartIndicationThread(RTPStream stream, bool bGoogle)
        {
            if (m_bThreadRunning == true)
                return;
            m_bThreadRunning = true;
            m_bGoogle = bGoogle;
            RTPStream = stream;

            ThreadIndication = new System.Threading.Thread(new System.Threading.ThreadStart(IndicationThread));
            ThreadIndication.IsBackground = true;
#if !WINDOWS_PHONE
            ThreadIndication.Priority = System.Threading.ThreadPriority.BelowNormal;
#endif
            ThreadIndication.Start();
        }

        public void StopIndicationThread()
        {
            m_bThreadRunning = false;
        }

        void IndicationThread()
        {
            while (m_bThreadRunning == true)
            {
                try
                {
                    if (m_bGoogle)
                        SendIndicationGoogle(RTPStream, string.Format("{0}{1}", this.RemoteCandidate.username, this.LocalCandidate.username));
                    else
                        SendIndication(RTPStream);
                }
                catch (Exception ex)
                {
                    return;
                }

                System.Threading.Thread.Sleep(m_nSendTimeout);
            }
        }

        public void SendIndication(RTPStream stream)
        {
            /// Need to send a binding indication every 3 s from now on?
            STUN2Message msgBindingIndication = new STUN2Message();
            msgBindingIndication.Method = StunMethod.Binding;
            msgBindingIndication.Class = StunClass.Inidication;


            /// Add fingerprint
            /// 
            int nLengthWithoutFingerPrint = msgBindingIndication.Bytes.Length;
            FingerPrintAttribute fattr = new FingerPrintAttribute();
            msgBindingIndication.AddAttribute(fattr);
            fattr.ComputeCRC(msgBindingIndication, nLengthWithoutFingerPrint);
            stream.SendRecvSTUN(this.RemoteCandidate.IPEndPoint, msgBindingIndication, 0);
        }

        public void SendIndicationGoogle(RTPStream stream, string strUsername)
        {
            /// Google talk appears to send a full stun binding request every 500 ms instead of a binding indication
            STUN2Message msgRequest = new STUN2Message();
            msgRequest.Method = StunMethod.Binding;
            msgRequest.Class = StunClass.Request;


            if (strUsername != null)
            {
                UserNameAttribute unameattr = new UserNameAttribute();
                unameattr.UserName = strUsername;
                msgRequest.AddAttribute(unameattr);
            }

            STUNMessage ResponseMessage = stream.SendRecvSTUN(this.RemoteCandidate.IPEndPoint, msgRequest, 0);
        }
        public int[] Timeouts = new int[] { 200, 500, 800, 1200 };

    }
}
