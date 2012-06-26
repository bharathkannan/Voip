using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Chat_server
{
    class Program
    {
        static void Main(string[] args)
        {
            //   IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);
            UdpClient newsock = new UdpClient(9050);
            IPEndPoint send = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] data1 = newsock.Receive(ref send);
                string data = Encoding.ASCII.GetString(data1);
                Console.WriteLine("Client {0}:{1}\n",send.Port.ToString(),data);
                Console.WriteLine(send.Address.ToString());
                UdpClient up = new UdpClient(send.Address.ToString(), send.Port);
                Console.WriteLine("Server:");
                string s = Console.ReadLine();
                byte[] s1 = Encoding.ASCII.GetBytes(s);
                int n = up.Send(s1, s1.Length);
           }
       }
    }
}
