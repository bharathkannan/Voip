using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.XMPP;

namespace JingleToneGeneratorService
{
    /// <summary>
    /// A simple jingle example to test our jingle libraries.  This object just looks for jingle sessions
    /// requests and responds by sending back RTP tone
    /// </summary>
    class Program
    {
        static XMPPClient XMPPClient = new XMPPClient();
        static Dictionary<string, ToneSession> ToneSessions = new System.Collections.Generic.Dictionary<string, ToneSession>();

        static void Main(string[] args)
        {

            /// TODO, user your own server and accounts, not mine :)
            XMPPClient.XMPPAccount.User = "test";
            XMPPClient.XMPPAccount.Password = "test";
            XMPPClient.XMPPAccount.Server = "ninethumbs.com";
            XMPPClient.XMPPAccount.Domain = "ninethumbs.com";
            XMPPClient.XMPPAccount.Resource = Guid.NewGuid().ToString();
            XMPPClient.XMPPAccount.Port = 5222;

            XMPPClient.AutoAcceptPresenceSubscribe = false;
            XMPPClient.AutomaticallyDownloadAvatars = false;
            XMPPClient.RetrieveRoster = false;

            XMPPClient.OnStateChanged += new EventHandler(XMPPClient_OnStateChanged);
            XMPPClient.JingleSessionManager.OnNewSession += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfoAndIQ(JingleSessionManager_OnNewSession);
            XMPPClient.JingleSessionManager.OnNewSessionAckReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnNewSessionAckReceived);
            XMPPClient.JingleSessionManager.OnSessionAcceptedAckReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnSessionAcceptedAckReceived);
            XMPPClient.JingleSessionManager.OnSessionAcceptedReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionAcceptedReceived);

            XMPPClient.JingleSessionManager.OnSessionTransportInfoReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionTransportInfoReceived);

            XMPPClient.JingleSessionManager.OnSessionTerminated += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEvent(JingleSessionManager_OnSessionTerminated);
            XMPPClient.Connect();

            
            
            Console.WriteLine("Jingle Session tone generator");
            Console.WriteLine("Type Exit to exit");

            while (true)
            {


                string strLine = Console.ReadLine();
                if (string.Compare(strLine, "exit", true) == 0)
                    break;
            }

            XMPPClient.Disconnect();
        }

        static void JingleSessionManager_OnNewSession(string strSession, System.Net.XMPP.Jingle.JingleIQ iq, System.Net.XMPP.Jingle.Jingle jingle, XMPPClient client)
        {
            Console.WriteLine("A new incoming session [{0}] has been found", strSession);

            /// Start a thread to send a session accept with our port information
            /// 

            ToneSession session = new ToneSession(strSession, jingle, client);
            ToneSessions.Add(strSession, session);
            
        }

        static void JingleSessionManager_OnNewSessionAckReceived(string strSession, System.Net.XMPP.Jingle.IQResponseAction response, XMPPClient client)
        {
            /// Should never happen since we don't send out session invitations, but only receive them
            if (response.AcceptIQ == true)
                Console.WriteLine("Session {0} has said OK to our Session invitation", strSession);
            else
                Console.WriteLine("Session {0} has rejected our Session invitation, Error is {1}-{2}", strSession, response.Error.Type, response.Error.ErrorDescription);
        }


        static void JingleSessionManager_OnSessionAcceptedReceived(string strSession, System.Net.XMPP.Jingle.Jingle jingle, XMPPClient client)
        {
            /// Should never happen since we don't send out session invitations, but only receive them
            Console.WriteLine("Session {0} has accepted our invitation", strSession);
        }

        static void JingleSessionManager_OnSessionAcceptedAckReceived(string strSession, System.Net.XMPP.Jingle.IQResponseAction response, XMPPClient client)
        {
            if (response.AcceptIQ == true)
            {
                Console.WriteLine("Session {0} has said OK to our Accept invitation", strSession);
                if (ToneSessions.ContainsKey(strSession) == true)
                {
                    ToneSession session = ToneSessions[strSession];
                    session.StartMedia();
                }
            }
            else
                Console.WriteLine("Session {0} has rejected our Accept invitation, Error is {1}-{2}", strSession, response.Error.Type, response.Error.ErrorDescription);

        }

        static void JingleSessionManager_OnSessionTransportInfoReceived(string strSession, System.Net.XMPP.Jingle.Jingle jingle, XMPPClient client)
        {
            if (ToneSessions.ContainsKey(strSession) == true)
            {
                ToneSession session = ToneSessions[strSession];
                session.GotTransportInfo(jingle);
            }

        }


        static void JingleSessionManager_OnSessionTerminated(string strSession, XMPPClient client)
        {
            Console.WriteLine("Session {0} has been terminated", strSession);
            if (ToneSessions.ContainsKey(strSession) == true)
            {
                if (ToneSessions.ContainsKey(strSession) == true)
                {
                    ToneSession session = ToneSessions[strSession];
                    session.Stop();
                }

                ToneSessions.Remove(strSession);
            }


        }


        static void XMPPClient_OnStateChanged(object sender, EventArgs e)
        {
            Console.WriteLine("XMPP Client state changed to: {0}", XMPPClient.XMPPState);

            if (XMPPClient.XMPPState == XMPPState.Ready)
            {
                XMPPClient.PresenceStatus.PresenceType = PresenceType.available;
                XMPPClient.PresenceStatus.Status = "online";
                XMPPClient.PresenceStatus.PresenceShow = PresenceShow.chat;
                XMPPClient.UpdatePresence();

            }
        }
    }
}
