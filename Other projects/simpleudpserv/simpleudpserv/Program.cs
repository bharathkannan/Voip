using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace simpleudpserv
{
    class Program
    {
        static void Main(string[] args)
        {
            
            /*
            UdpClient serv = new UdpClient(8002);
            serv.Connect(new IPEndPoint(IPAddress.Parse("127.100.100.100"), 8002));
            byte[] data = new byte[100];
            IPEndPoint ip = new IPEndPoint(IPAddress.Any,0);
            Console.WriteLine("waiting");
            
              
            data = serv.Receive(ref ip);
            string recd = Encoding.ASCII.GetString(data);
            Console.WriteLine("hi");
            Console.WriteLine(recd);
            Console.Read();
            

            string inp= Console.ReadLine();
            byte[] tos = Encoding.ASCII.GetBytes(inp);
            serv.Send(tos, tos.Length);
            Console.WriteLine("Sent");
            Console.ReadLine();
          


            */


            byte[] data = new byte[1024];
         //   IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);
            UdpClient newsock = new UdpClient(3001);
            Console.WriteLine("Waiting for a client...");
            IPEndPoint send = new IPEndPoint(IPAddress.Any, 0);




            List< byte[] > audio = new List<byte[]>();
            int ct = 0;
            while (true)
            {
                byte[] data1 = newsock.Receive(ref send);
              //  Console.WriteLine("test1 = {0}", test1);
                Console.WriteLine(send.Address.ToString());
                Console.WriteLine(data1.Length);
                audio.Add(data1);
                ct += data1.Length;
                Console.WriteLine(ct);
            }

            
                
            
            /*UdpClient response = new UdpClient(send.Address.ToString(),3001);
                Console.Write(send.Address.ToString());
                response.Send(data1, data1.Length);
            */

/*
             UdpClient up = new UdpClient(send.Address.ToString(), send.Port);
             String s="bharath";
             byte[] s1=Encoding.ASCII.GetBytes(s);
             int n=  up.Send(s1,s1.Length);
             Console.Write(n);
             Console.Read();
 */
        }
    }
}
