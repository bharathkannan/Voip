/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;


namespace SocketServer
{
	public class SocketListener
	{
		public SocketListener()
		{
			AcceptCallback = new AsyncCallback(OnAcceptReceived);
		}

        public void Close()
        {
            if (ListeningSocket != null)
            {
                try
                {
                    ListeningSocket.Close();
                }
                catch (Exception)
                {
                }
                finally
                {
                    ListeningSocket = null;
                }
            }
        }


        public bool EnableAccept(int nPort)
		{
			IPEndPoint ep = new IPEndPoint(System.Net.IPAddress.Any, nPort);
			return EnableAccept(ep);
		}

        public bool EnableAcceptIPV6(int nPort)
        {
            IPEndPoint ep = new IPEndPoint(System.Net.IPAddress.IPv6Any, nPort);
            return EnableAccept(ep);
        }


        public System.Net.Sockets.Socket ListeningSocket = null;
		/// <summary>
		/// Starts listening for new connections on this socket
		/// </summary>
		/// <param name="ep"></param>
		/// <param name="creator"></param>
		public bool EnableAccept( IPEndPoint ep)
		{
            if (ListeningSocket != null)
                throw new Exception("Already Listening");

			System.Net.EndPoint epBind  = (EndPoint)ep;
                
		    ListeningSocket = new System.Net.Sockets.Socket(ep.AddressFamily/*AddressFamily.InterNetwork*/, SocketType.Stream, ProtocolType.IP);
		    try
		    {
			    ListeningSocket.Bind(epBind);
		    }
         catch(SocketException e) /// winso
         {
            System.Diagnostics.Debug.WriteLine("Exception calling Bind {0}", e);
            return false;
         }
         catch(ObjectDisposedException e2) // socket was closed
         {
            System.Diagnostics.Debug.WriteLine("Exception calling Bind {0}", e2);
            return false;
         }
         catch(System.Exception ebind) // socket was closed
         {
            System.Diagnostics.Debug.WriteLine("Exception calling Bind {0}", ebind);
            return false;
         }

			try
			{
            //int nCon = Convert.ToInt32(sman.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.MaxConnections));
            //this.LogMessage(MessageImportance.High, "socket", string.Format("Max listen queue is {0}", nCon));
            ListeningSocket.Listen((int)SocketOptionName.MaxConnections);
			}
         catch(SocketException e3) /// winso
         {
             System.Diagnostics.Debug.WriteLine("Exception calling Listen {0}", e3);
            return false;
         }
         catch(ObjectDisposedException e4) // socket was closed
         {
             System.Diagnostics.Debug.WriteLine("Exception calling Listen {0}", e4);
            return false;
         }
         catch(System.Exception eit) // socket was closed
         {
             System.Diagnostics.Debug.WriteLine("Exception calling Listen {0}", eit);
            return false;
         }


         try
         {
            /// Multiple Accepts commented out by B.B. 3-23-2007.
            /// These cause 100% CPU on Windows 2003 Server whenever the first incoming 
            /// connection is received - but works fine on Windows XP
            /// 
            ListeningSocket.BeginAccept(AcceptCallback, ListeningSocket);

            /// 5.2 = windows 2003
            if (System.Environment.OSVersion.Version.Major >= 6) 
            {
               //sman.BeginAccept(AcceptCallback, sman);
               //sman.BeginAccept(AcceptCallback, sman);
            }
            //sman.BeginAccept(AcceptCallback, sman);
            //sman.BeginAccept(AcceptCallback, sman);
            //sman.BeginAccept(AcceptCallback, sman);
         }
         catch(SocketException e5) /// winso
         {
             System.Diagnostics.Debug.WriteLine("Exception calling BeginAccept {0}", e5);
            return false;
         }
         catch(ObjectDisposedException e6) // socket was closed
         {
             System.Diagnostics.Debug.WriteLine("Exception calling BeginAccept {0}", e6);
            return false;
         }
         catch(System.Exception eall)
         {
             System.Diagnostics.Debug.WriteLine("Exception calling BeginAccept {0}", eall);
            return false;
         }

			return true;
		}

		protected System.AsyncCallback AcceptCallback = null;
		

		internal void OnAcceptReceived(IAsyncResult ar)
		{
			System.Net.Sockets.Socket sman = (System.Net.Sockets.Socket) ar.AsyncState;

            if (sman != null)
            {


                System.Net.Sockets.Socket newsocket = null;
                try
                {
                    newsocket = sman.EndAccept(ar);
                }
                catch (System.ObjectDisposedException e)
                {
                    System.Diagnostics.Debug.WriteLine("Exception calling EndAccept {0}", e);
                    return;
                }
                catch (System.Exception eall)
                {
                    System.Diagnostics.Debug.WriteLine("Exception calling EndAccept - continueing {0}", eall);
                }

                /// Start a new accept
                try
                {
                    sman.BeginAccept(AcceptCallback, sman);
                }
                catch (SocketException e3) /// winso
                {
                    System.Diagnostics.Debug.WriteLine("Exception calling BeginAccept2 {0}", e3);
                }
                catch (ObjectDisposedException e4) // socket was closed
                {
                    System.Diagnostics.Debug.WriteLine("Exception calling BeginAccept2 {0}", e4);
                }
                catch (System.Exception eall)
                {
                    System.Diagnostics.Debug.WriteLine("Exception calling BeginAccept2 {0}", eall);
                }

                if (newsocket == null)
                    return;

                System.Diagnostics.Debug.WriteLine("Accepted new socket {0}", newsocket.Handle);
                if (OnNewConnection != null)
                    OnNewConnection(newsocket);
            }
        }


        public delegate void DelegateNewConnectedSocket(Socket s);
        public event DelegateNewConnectedSocket OnNewConnection = null;


	}
}

