using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient t = new TcpClient();
            t.Connect(IPAddress.Parse("172.16.41.181"),8237);
            NetworkStream ns = t.GetStream();
            StreamWriter s = new StreamWriter(ns);
            s.WriteLine("8001");
            Console.WriteLine("sent");
           // StreamReader r = new StreamReader(ns);
           // Console.WriteLine(r.ReadLine());
            r.Close();
            s.Close();
            t.Close();
            Console.Read();

        }
    }
}
