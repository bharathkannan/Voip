using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Linq;
using System.Text;


///http://blogs.msdn.com/b/andypennell/archive/2011/08/11/finding-your-own-ip-address-on-windows-phone-mango.aspx
namespace FindMyIP
{
    public class MyIPAddress
    {
        IPAddress FoundIPAddress = null;
        UdpAnySourceMulticastClient MulticastSocket;
        const int PortNumber = 50000;       // pick a number, any number
        string MulticastMessage = "FIND-MY-IP-PLEASE" + new Random().Next().ToString();

        System.Threading.ManualResetEvent WaitEvent = new System.Threading.ManualResetEvent(false);
        public IPAddress Find()
        {
            WaitEvent.Reset();
            MulticastSocket = new UdpAnySourceMulticastClient(IPAddress.Parse("239.255.255.250"), PortNumber);
            MulticastSocket.BeginJoinGroup((result) =>
            {
                try
                {
                    MulticastSocket.EndJoinGroup(result);
                    GroupJoined(result);
                }
                catch (Exception ex)
                {
                    WaitEvent.Set();
                    Debug.WriteLine("EndjoinGroup exception {0}", ex.Message);
                }
            },
                null);

            WaitEvent.WaitOne();

            return FoundIPAddress;
        }

        void callback_send(IAsyncResult result)
        {
        }

        byte[] MulticastData;
        bool keepsearching;

        void GroupJoined(IAsyncResult result)
        {
            MulticastData = Encoding.UTF8.GetBytes(MulticastMessage);
            keepsearching = true;
            MulticastSocket.BeginSendToGroup(MulticastData, 0, MulticastData.Length, callback_send, null);

            while (keepsearching)
            {
                try
                {
                    byte[] buffer = new byte[MulticastData.Length];
                    MulticastSocket.BeginReceiveFromGroup(buffer, 0, buffer.Length, DoneReceiveFromGroup, buffer);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Stopped Group read due to " + ex.Message);
                    keepsearching = false;
                    WaitEvent.Set();
                }
            }

        }

        void DoneReceiveFromGroup(IAsyncResult result)
        {
            IPEndPoint where;
            int responselength = MulticastSocket.EndReceiveFromGroup(result, out where);
            byte[] buffer = result.AsyncState as byte[];
            if (responselength == MulticastData.Length && buffer.SequenceEqual(MulticastData))
            {
                Debug.WriteLine("FOUND myself at " + where.Address.ToString());
                keepsearching = false;
                FoundIPAddress = where.Address;
            }

            WaitEvent.Set();
        }
    }
}
