using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;

namespace RTP
{
    public class RTCPSession
    {
        public RTCPSession()
        {
        }

        private IPEndPoint m_objLocalEndpoint = new IPEndPoint(IPAddress.Any, 0);

        public IPEndPoint LocalEndpoint
        {
            get { return m_objLocalEndpoint; }
            set { m_objLocalEndpoint = value; }
        }

        protected SocketServer.UDPSocketClient UDPClient = null;

        protected object SocketLock = new object();

        private bool m_bIsActive = false;

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

       
        public event DelegateSTUNMessage OnUnhandleSTUNMessage = null;

        protected List<STUNRequestResponse> StunRequestResponses = new List<STUNRequestResponse>();
        protected object StunLock = new object();

        public STUNMessage SendRecvSTUN(IPEndPoint epStun, STUNMessage msgRequest, int nTimeout)
        {
            STUNRequestResponse req = new STUNRequestResponse(msgRequest);
            lock (StunLock)
            {
                StunRequestResponses.Add(req);
            }

            SendSTUNMessage(msgRequest, epStun);

            req.WaitForResponse(nTimeout);
            return req.ResponseMessage;
        }

        public int SendSTUNMessage(STUNMessage msg, IPEndPoint epStun)
        {
            byte[] bMessage = msg.Bytes;
            return this.UDPClient.SendUDP(bMessage, bMessage.Length, epStun);
        }


        public void Bind(IPEndPoint localEp)
        {
            if (IsBound == false)
            {
                LocalEndpoint = localEp;
                UDPClient = new SocketServer.UDPSocketClient(LocalEndpoint);
                UDPClient.Bind();

#if !WINDOWS_PHONE
                LocalEndpoint = UDPClient.s.LocalEndPoint as IPEndPoint;
#else
#endif
                IsBound = true;
                UDPClient.OnReceiveMessage += new SocketServer.UDPSocketClient.DelegateReceivePacket(RTPUDPClient_OnReceiveMessage);
                UDPClient.StartReceiving();

            }
        }


        public virtual void Stop()
        {
            if (IsActive == false)
                return;

            IsActive = false;
            IsBound = false;
            UDPClient.StopReceiving();
            UDPClient.OnReceiveMessage -= new SocketServer.UDPSocketClient.DelegateReceivePacket(RTPUDPClient_OnReceiveMessage);
            UDPClient = null;
        }


        void RTPUDPClient_OnReceiveMessage(byte[] bData, int nLength, IPEndPoint epfrom, IPEndPoint epthis, DateTime dtReceived)
        {
            /// if we are an performing ICE, see if this is an ICE packet instead of an RTP one
            if (nLength >= 8)
            {
                //0x2112A442
                if ((bData[4] == 0x21) && (bData[5] == 0x12) && (bData[6] == 0xA4) && (bData[7] == 0x42))
                {
                    /// STUN message
                    STUN2Message smsg = new STUN2Message();
                    byte[] bStun = new byte[nLength];
                    Array.Copy(bData, 0, bStun, 0, nLength);
                    smsg.Bytes = bStun;

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

            /// TODO... handle RTCP packets if we ever care to
        }
    }


}
