using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.XMPP;
using System.Net.XMPP.Jingle;

using SocketServer;
using RTP;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace JingleToneGeneratorService
{

    /// <summary>
    ///  A simple tone session, we ignore incoming RTP and send out RTP packets with tone
    /// </summary>
    public class ToneSession
    {
        public ToneSession(string strSession, Jingle intialJingle, XMPPClient client)
        {
            Session = strSession;
            InitialJingle = intialJingle;
            XMPPClient = client;

            /// If we don't have any candidates in our sesion-initate message, we must wait for a transport info to get the candidates
            ParseCandidates();
            if (Candidates.Count > 0)
                SendOurTransportInfo(intialJingle); // not here, 
        }

        List<Candidate> Candidates = new List<Candidate>();

        public Jingle InitialJingle = null;
        public string Session = null;
        public XMPPClient XMPPClient = null;

        IPEndPoint SendToEndpoint = null;
        void ParseCandidates()
        {
            if (InitialJingle != null)
            {
                if ((InitialJingle.Content != null) && (InitialJingle.Content.ICETransport != null))
                {
                    if (InitialJingle.Content.ICETransport.Candidates.Count > 0)
                    {
                        Candidates = InitialJingle.Content.ICETransport.Candidates;
                        SendToEndpoint = new IPEndPoint(IPAddress.Parse(InitialJingle.Content.ICETransport.Candidates[0].ipaddress),
                            InitialJingle.Content.ICETransport.Candidates[0].port);
                    }

                }
            }
        }

        public void GotTransportInfo(Jingle initialjingle)
        {
            InitialJingle = initialjingle;
            ParseCandidates();
            if (Candidates.Count > 0)
                SendOurTransportInfo(initialjingle); // not here, 
        }


        UDPSocketClient UdpClient = null;
        string strPassword = null;
        string strUser = null;

        void SendOurTransportInfo(Jingle jingle)
        {
            /// First find our local ip address
            IPAddress [] addresses = FindAddresses();

            if (addresses.Length <= 0)
                return;

            /// Generate a random user/password
            strPassword = GenerateRandomString(22);
            strUser = GenerateRandomString(4);

            /// Send out accept session with speex only
            /// 
            IPEndPoint ep = new IPEndPoint(addresses[0], 8010);
            UdpClient = new UDPSocketClient(ep);
            UdpClient.OnReceivePacket += new UDPSocketClient.DelegateReceivePacket(UdpClient_OnReceivePacket);
            UdpClient.Bind();
            UdpClient.StartReceiving();

            int nPort = ((IPEndPoint)UdpClient.s.LocalEndPoint).Port;

            /// Build a jingle response
            /// 
            Jingle jinglecontent = new Jingle();
            jinglecontent.Content = new Content();
            jinglecontent.Action = Jingle.TransportInfo;
            jinglecontent.Initiator = null;
            jinglecontent.Content.Description = null;  ;
            jinglecontent.Content.Creator = "initiator";
            jinglecontent.Content.Name = "A";


            /// If you don't want to use ICE UDP, new a different transport object
            jinglecontent.Content.ICETransport = new Transport();
            jinglecontent.Content.ICETransport.pwd = strPassword;
            jinglecontent.Content.ICETransport.ufrag = strUser;

            jinglecontent.Content.ICETransport.Candidates.Add(new Candidate() { id = GenerateRandomString(10), component=1, priority=2130706435, ipaddress = ep.Address.ToString(), port = nPort, network = 0});
            jinglecontent.Content.ICETransport.Candidates.Add(new Candidate() { id = GenerateRandomString(10), component=2, priority=2130706434, ipaddress = addresses[0].ToString(), port = nPort + 1, network = 0 });

            XMPPClient.JingleSessionManager.SendJingle(this.Session, jinglecontent);
        }

        void SendAcceptSession(Jingle jingle)
        {
            /// Build a jingle response
            Jingle jinglecontent = new Jingle();
            jinglecontent.Content = new Content();
            jinglecontent.Action = Jingle.SessionAccept;
            jinglecontent.Content.Description = new Description();
            jinglecontent.Content.Description.media = "audio";
            jinglecontent.Content.Creator = "responder";
            jinglecontent.Content.Name = "B";

            jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 96, Channels = "1", ClockRate = "16000", Name = "speex" });
            //jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 0, Channels = "1", ClockRate = "8000", Name = "PCMU" });

            XMPPClient.JingleSessionManager.SendAcceptSession(this.Session, jinglecontent);
        }
        Random rand = new Random();
        public string GenerateRandomString(int nLength)
        {
            /// 48-57, 65-90, 97-122
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < nLength; i++)
            {
                int c = rand.Next(62);
                if (c < 10)
                    c += 48;
                else if ((c >= 10) && (c < 36))
                    c += (65-10);
                else if (c >= 36)
                    c += (97-36);

                sb.Append((char)c);
            }

            return sb.ToString();
        }

        void UdpClient_OnReceivePacket(byte[] bData, int nLength, IPEndPoint epfrom, IPEndPoint epthis, DateTime dtReceived)
        {
            /// ignore incoming audio
        }

        IMediaTimer packettimer = null;
        ushort nSequence = 0;
        uint nSSRC = 0;
        public void StartMedia()
        {
            if (packettimer != null)
                return;

            nSSRC = (uint) new Random().Next();

            packettimer = SocketServer.QuickTimerControllerCPU.CreateTimer(20, new DelegateTimerFired(OnTimeSendNextPacket), Session, null);
        }

        void OnTimeSendNextPacket(IMediaTimer timer)
        {
            if (SendToEndpoint == null)
                return;


            RTPPacket []packets = codec.Encode(BuildTonePayload());
            RTPPacket nextpacket = packets[0]; // Should only return 1 packet

            nextpacket.PayloadType = 0;
            nextpacket.SSRC = nSSRC;
            nextpacket.TimeStamp = (uint) (160 * nSequence);
            nextpacket.SequenceNumber = nSequence++;

            byte[] bSend = nextpacket.GetBytes();

            UdpClient.SendUDP(bSend, bSend.Length, SendToEndpoint);
        }

        const double w1 = 350 * 2.0 *Math.PI;
        const double w2= 440 * 2.0 * Math.PI;
        //RTP.G711Codec codec = new G711Codec();
        RTP.SpeexCodec codec = new SpeexCodec(NSpeex.BandMode.Wide);

        short[] BuildTonePayload()
        {
            short[] sPayload = new short[160]; /// 160 samples for 20 ms packets
            double fAmplitude = 11000; /// 11000/16000 amplitude
             
            for (int i = 0; i < 160; i++)
            {
                double t = (i + (160 * nSequence)) / 8000.0f;

                sPayload[i] = (short) (fAmplitude*(Math.Sin(w1 * t) + Math.Sin(w2 * t)));
            }

            return sPayload;
        }

        public void Stop()
        {
            if (packettimer != null)
            {
                packettimer.Cancel();
                packettimer = null;
            }
        }

      public IPAddress [] FindAddresses()
      {
          List<IPAddress> IPs = new List<IPAddress>();
         /// See what interfaces can connect to our itpcluster
         /// 

         IPAddress BindAddress = IPAddress.Any;
         NetworkInterface[] infs = NetworkInterface.GetAllNetworkInterfaces();


         foreach (NetworkInterface inf in infs)
         {
             try
             {
                 IPInterfaceProperties props = inf.GetIPProperties();
                 if (props == null)
                    continue;

                 IPv4InterfaceProperties ip4 = props.GetIPv4Properties();
                 if (ip4 == null)  /// TODO.. allow for IPV6 interfaces
                     continue;
                 if (ip4.IsAutomaticPrivateAddressingActive == true)
                     continue;
                 foreach (UnicastIPAddressInformation addrinfo in props.UnicastAddresses)
                 {

                     if (addrinfo.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                         continue;

                     //addrinfo.SuffixOrigin == SuffixOrigin.OriginDhcp
                     //addrinfo.PrefixOrigin == PrefixOrigin.Dhcp

                     if (addrinfo.PrefixOrigin == PrefixOrigin.WellKnown)
                        continue; /// ignore well known IP addresses


                     if (addrinfo.Address.Equals(IPAddress.Any) == false)
                     {

                        if (addrinfo.Address.Equals(IPAddress.Parse("127.0.0.1") ) == false)
                           IPs.Add(new IPAddress(addrinfo.Address.GetAddressBytes()));
                     }

                 }
             }
             catch (Exception)
             {
             }
         }

          return IPs.ToArray();
      }


    }
}
