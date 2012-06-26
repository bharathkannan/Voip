/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace SocketServer
{
	/// <summary>
	/// One instance of this class is made for each connection manager and is used to
	/// listen to multiple TCP and UDP ports for incoming connections.  Once a connection is
	/// made, the appropriate SocketCreator.CreateSocket function is called
	/// </summary>
	/// 
	internal class AcceptorManager
	{
		internal AcceptorManager(ConnectMgr conparent)
		{
			m_ConMgrParent = conparent;
			AcceptCallback = new AsyncCallback(OnAcceptReceived);
		}

      /// <summary>
      /// set for logging
      /// </summary>
      protected ILogInterface m_Logger = null;
      public string OurGuid = "UDPClient";

      public ILogInterface Logger
      {
         set
         {
            m_Logger = value;
         }
      }

      void LogMessage(MessageImportance importance, string strEventName, string strMessage)
      {
         if (m_Logger != null)
         {
             m_Logger.LogMessage(OurGuid, importance, strMessage);
         }
         else
         {
#if !MONO
            System.Diagnostics.Trace.WriteLine(strMessage, strEventName);
#endif
         }
      }

      void LogWarning(MessageImportance importance, string strEventName, string strMessage)
      {
         if (m_Logger != null)
         {
             m_Logger.LogWarning(OurGuid, importance, strMessage);
         }
         else
         {
#if !MONO
             System.Diagnostics.Trace.WriteLine(strMessage, strEventName);
#endif
         }
      }

      void LogError(MessageImportance importance, string strEventName, string strMessage)
      {
         if (m_Logger != null)
         {
             m_Logger.LogError(OurGuid, importance, strMessage);
         }
         else
         {
#if !MONO    
             System.Diagnostics.Trace.WriteLine(strMessage, strEventName);
#endif
         }
      }

		internal bool StopListen(int nPort)
		{
			// find out who has this port, then stop them from listening
			System.Net.Sockets.Socket objSocket = GetSocketForThisPort(nPort);
			if (objSocket != null)
			{
				objSocket.Close();
				m_SocketSet.Remove(objSocket);
				return true;
			}

			return false;
		}

		internal bool EnableAccept( int nPort, SocketCreator creator)
		{
			IPEndPoint ep = new IPEndPoint(System.Net.IPAddress.Any, nPort);
			return EnableAccept(ep, creator);
		}

      internal bool EnableAcceptIPV6(int nPort, SocketCreator creator)
      {
         IPEndPoint ep = new IPEndPoint(System.Net.IPAddress.IPv6Any, nPort);
         return EnableAccept(ep, creator);
      }

		internal System.Net.Sockets.Socket GetSocketForThisPort(int nPort)
		{
			foreach (Socket nextsocket in m_SocketSet.Keys)
			{
				if (nextsocket != null)
				{
					IPEndPoint ep = (IPEndPoint) nextsocket.LocalEndPoint;
					if (ep != null)
					{
						if (ep.Port == nPort)
							return nextsocket;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Starts listening for new connection son this socket
		/// </summary>
		/// <param name="ep"></param>
		/// <param name="creator"></param>
		internal bool EnableAccept( IPEndPoint ep, SocketCreator creator)
		{
			/// start listening on thisport
			/// Start the accept process
			/// 

			/// First make sure no one we have is already listening on this port
			/// 

			System.Net.EndPoint epBind  = (EndPoint)ep;

			System.Net.Sockets.Socket sman = new System.Net.Sockets.Socket(ep.AddressFamily/*AddressFamily.InterNetwork*/, SocketType.Stream, ProtocolType.IP);
			try
			{
				sman.Bind(epBind);
			}
         catch(SocketException e) /// winso
         {
            string strError = string.Format("Exception calling Bind {0}", e);
            LogError(MessageImportance.Highest, "EXCEPTION", strError );
            return false;
         }
         catch(ObjectDisposedException e2) // socket was closed
         {
            string strError = string.Format("Exception calling Bind {0}", e2);
            LogError(MessageImportance.Highest, "EXCEPTION", strError);
            return false;
         }
         catch(System.Exception ebind) // socket was closed
         {
            string strError = string.Format("Exception calling Bind {0}", ebind);
            LogError(MessageImportance.Highest, "EXCEPTION", strError);
            return false;
         }

			m_SocketSet.Add(sman, creator);

			try
			{
            //int nCon = Convert.ToInt32(sman.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.MaxConnections));
            //this.LogMessage(MessageImportance.High, "socket", string.Format("Max listen queue is {0}", nCon));
            sman.Listen((int)SocketOptionName.MaxConnections);
			}
         catch(SocketException e3) /// winso
         {
            string strError = string.Format("Exception calling Listen {0}", e3);
            LogError(MessageImportance.Highest, "EXCEPTION", strError );
            return false;
         }
         catch(ObjectDisposedException e4) // socket was closed
         {
            string strError = string.Format("Exception calling Listen {0}", e4);
            LogError(MessageImportance.Highest, "EXCEPTION", strError);
            return false;
         }
         catch(System.Exception eit) // socket was closed
         {
            string strError = string.Format("Exception calling Listen {0}", eit);
            LogError(MessageImportance.Highest, "EXCEPTION", strError);
            return false;
         }


         try
         {
            /// Multiple Accepts commented out by B.B. 3-23-2007.
            /// These cause 100% CPU on Windows 2003 Server whenever the first incoming 
            /// connection is received - but works fine on Windows XP
            /// 
            sman.BeginAccept(AcceptCallback, sman);

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
            string strError = string.Format("Exception calling BeginAccept {0}", e5);
            LogError(MessageImportance.Highest, "EXCEPTION", strError );
            return false;
         }
         catch(ObjectDisposedException e6) // socket was closed
         {
            string strError = string.Format("Exception calling BeginAccept {0}", e6);
            LogError(MessageImportance.Highest, "EXCEPTION", strError);
            return false;
         }
         catch(System.Exception eall)
         {
            string strError = string.Format("Exception calling BeginAccept {0}", eall);
            LogError(MessageImportance.Highest, "EXCEPTION", strError);
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
            catch(System.ObjectDisposedException e)
            {
               string strError = string.Format("Exception calling EndAccept {0}", e);
               LogError(MessageImportance.Highest, "EXCEPTION", strError);
               return;
            }
            catch(System.Exception eall)
            {
               string strError = string.Format("Exception calling EndAccept - continueing {0}", eall);
               LogError(MessageImportance.Highest, "EXCEPTION", strError);
            }

            /// Start a new accept
            try
            {
               sman.BeginAccept(AcceptCallback, sman);
            }
            catch (SocketException e3) /// winso
            {
               string strError = string.Format("Exception calling BeginAccept2 {0}", e3);
               LogError(MessageImportance.Highest, "EXCEPTION", strError);
            }
            catch (ObjectDisposedException e4) // socket was closed
            {
               string strError = string.Format("Exception calling BeginAccept2 {0}", e4);
               LogError(MessageImportance.Highest, "EXCEPTION", strError);
            }
            catch (System.Exception eall)
            {
               string strError = string.Format("Exception calling BeginAccept2 {0}", eall);
               LogError(MessageImportance.Highest, "EXCEPTION", strError);
            }


            if (newsocket == null)
               return;

            LogMessage(MessageImportance.Medium, "NEWCON", string.Format("Accepted new socket {0}", newsocket.Handle));
				 /// look up the creator for this socket
				 /// 
				SocketCreator creator = (SocketCreator) this.m_SocketSet[sman];
				if (creator != null)
				{
					SocketClient newclient = creator.AcceptSocket(newsocket, this.m_ConMgrParent);

               try
               {
                  m_ConMgrParent.FireAcceptHandler(newclient);
               }
               catch(System.Exception efire)
               {
                  string strError = string.Format("Exception Fireing Acception Handler -{0}", efire);
                  LogError(MessageImportance.Highest, "EXCEPTION", strError);
               }

					newclient.DoAsyncRead();
				}

			}
		}


		/// <summary>
		/// a hash of our sockets versus socket creators
		/// </summary>
		protected System.Collections.Hashtable m_SocketSet =  new System.Collections.Hashtable();

		/// <summary>
		/// our connection manager
		/// </summary>
		private ConnectMgr m_ConMgrParent = null;

	}
}
