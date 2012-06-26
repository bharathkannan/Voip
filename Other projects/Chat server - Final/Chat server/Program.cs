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
            byte[] data1 = newsock.Receive(ref send);
            string data = Encoding.ASCII.GetString(data1);
            Console.WriteLine("Client {0}:{1}\n",send.Port.ToString(),data);
            receiver rs = new receiver(send,newsock);
            ThreadStart ts = new ThreadStart(rs.receive);
            Thread t = new Thread(ts);
            t.Start();
            while(true)
            {
                UdpClient up = new UdpClient(send.Address.ToString(), send.Port);
                Console.Write("Server:");
                string s = Console.ReadLine();
                byte[] s1 = Encoding.ASCII.GetBytes(s);
                int n = up.Send(s1, s1.Length);
            }
           
       }
    }
    public class receiver
    {
        IPEndPoint ip;
        UdpClient cli;
        public receiver(IPEndPoint ip, UdpClient cli)
        {
            this.ip = ip;
            this.cli = cli;
        }
        public void receive()
        {
            while(true)
            {
                byte[] b = cli.Receive(ref ip);
                string res = Encoding.ASCII.GetString(b);
                Console.WriteLine("Client{0}:{1}", ip.Port.ToString(), res);
            }
        }
    }
}
