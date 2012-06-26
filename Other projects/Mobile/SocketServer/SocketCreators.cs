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
    /// This class is used to create new socketclients of the correct typewithin ConnectMgr
    /// Derive from this class 
    /// </summary>
	public class SocketCreator
	{
		public SocketCreator()
		{
		}

		public virtual SocketClient AcceptSocket( Socket s, ConnectMgr cmgr )
		{
			s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 128000);
			return new SocketClient( s, cmgr );
		}

		public virtual SocketClient CreateSocket( Socket s, ConnectMgr cmgr )
		{
			s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 128000);
			return new SocketClient( s, cmgr );
		}
	}

	

}
