using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress myip = IPAddress.Parse("172.16.41.174");
            TcpListener mylistener = new TcpListener(myip,8237);
            mylistener.Start();
            Console.WriteLine("started");
            Socket s = mylistener.AcceptSocket();
            Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);
            Console.WriteLine("Connection to" + s.LocalEndPoint);
            byte[] recd = new byte[100];
            
            int n=s.Receive(recd);
            for (int i = 0; i < n; i++)
                Console.Write(Convert.ToChar(recd[i]));
            s.Close();
            Console.Read();



        }
    }
}
