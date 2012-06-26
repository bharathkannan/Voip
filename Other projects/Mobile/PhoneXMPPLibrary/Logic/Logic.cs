/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

namespace System.Net.XMPP
{
    /// <summary>
    /// Handles different types of xml fragments on our streams, such as negotiation, IQ's, etc
    /// </summary>
    public class Logic
    {
        public Logic(XMPPClient client)
        {
            XMPPClient = client;
        }

        protected XMPPClient XMPPClient = null;

        public virtual void Start()
        {
        }


        /// <summary>
        /// A new XML fragment has been received
        /// </summary>
        /// <param name="node"></param>
        /// <returns>returns true if we handled this fragment, false if other wise</returns>
        public virtual bool NewXMLFragment(XMPPStanza stanza)
        {
            return false;
        }

        public virtual bool NewIQ(IQ iq)
        {
            return false;
        }

        public virtual bool NewMessage(Message iq)
        {
            return false;
        }

        public virtual bool NewPresence(PresenceMessage iq)
        {
            return false;
        }

        protected bool m_bCompleted = false;

        /// <summary>
        /// Set to true if we have completed our logic and should be removed from the logic list
        /// </summary>
        public bool IsCompleted
        {
            get { return m_bCompleted; }
            set { m_bCompleted = value; }
        }

        private bool m_bSuccess = false;

        public bool Success
        {
            get { return m_bSuccess; }
            set { m_bSuccess = value; }
        }

    }
}
