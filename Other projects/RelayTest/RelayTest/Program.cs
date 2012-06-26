using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RelayTest
{
    class Program
    {
        public class Pair<T, U>
        {
            public Pair()
            {
            }

            public Pair(T first, U second)
            {
                this.First = first;
                this.Second = second;
            }

            public T First { get; set; }
            public U Second { get; set; }
            public byte[] GetBytes()
            {
                String a = First.ToString();
                String b = Second.ToString();
                return Encoding.ASCII.GetBytes(a + b);
            }
        };

        static Pair<IPAddress, int> Parse(string s, ref string uname)
        {
            int ct = 0;
            string a, b, c;
            a = b = c = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ') { ct++; continue; }
                if (ct == 0)
                    a += s[i];
                else if (ct == 1)
                    b += s[i];
                else
                    c += s[i];
            }
            IPAddress ip = IPAddress.Parse(b);
            int port = Convert.ToInt32(c);
            uname = a;
            Pair<IPAddress, int> ret = new Pair<IPAddress, int>();
            ret.First = ip;
            ret.Second = port;
            return ret;
        }
        static void Main(string[] args)
        {
            while (true)
            {
                UdpClient uc = new UdpClient(4507);
                IPEndPoint x = new IPEndPoint(IPAddress.Any, 0);
                uc.Receive(ref x);
                Console.WriteLine(x.ToString());
                Console.Read();
            }
            
 
        }
    }
}
