using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SOCKS5Test
{
    class Program
    {
        /// <summary>
        ///  Simple SOCKS4/5 service.  Point your browser to 127.0.0.1:8080
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            SOCKS5ServiceLibrary.SOCKServer server = new SOCKS5ServiceLibrary.SOCKServer();
            server.Port = 8080;
            server.Start();

            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
