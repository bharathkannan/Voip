using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
namespace Relayserver
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
            return Encoding.ASCII.GetBytes(a+b);
        }
    };
    
    class Program
    {
        
        static void Main(string[] args)
        {
            UdpClient uc = new UdpClient(4505);
            Dictionary<String, IPEndPoint> LookUp =  new Dictionary<String, IPEndPoint>();
            while (true)
            {

                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                String message = Encoding.ASCII.GetString(uc.Receive(ref remote));
                Console.WriteLine(message);
                if (message.Substring(0, 3).Equals("get"))
                {
                    string uname = message.Substring(4);
                    if (LookUp.ContainsKey(uname))
                    {
                        IPEndPoint ret = LookUp[uname];
                        byte[] reply = Encoding.ASCII.GetBytes(ret.ToString());
                        uc.Send(reply, reply.Length, remote);
                    }
                    else
                    {
                        byte[] reply = Encoding.ASCII.GetBytes("Failed");
                        uc.Send(reply, reply.Length,remote);
                    }
                }
                else if (message.Substring(0, 3).Equals("set"))
                {
                    string uname = message.Substring(4);
                    if(LookUp.ContainsKey(uname))
                        LookUp[uname]=remote;
                    else
                    {
                        LookUp.Add(uname, remote);
                    }
                    byte[] send = Encoding.ASCII.GetBytes("Changed");
                    uc.Send(send,send.Length,remote);
                }
            }
        }
  

        static Pair<IPAddress,int> Parse(string s,ref string uname)
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
            Pair<IPAddress,int> ret = new Pair<IPAddress,int>();
            ret.First = ip;
            ret.Second = port;
            return ret;
        }
    }
 }

