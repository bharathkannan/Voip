/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;

namespace RTP
{
    public enum StunClass : byte
    {
        Request = 0,
        Inidication = 1,
        Success = 2,
        Error = 3,
    }

    public enum StunMethod : ushort
    {
        Binding = 1,
    }

    public enum StunAttributeType : ushort
    {
        Reserved = 0x0000,
        MappedAddress = 0x0001,
        LegacyResponseAddress = 0x0002,
        LegacyChangeAddress = 0x0003,
        LegacySourceAddress = 0x0004,
        LegacyChangedAddress = 0x0005,
        UserName = 0x0006,
        Password = 0x0007,
        MessageIntegrity = 0x0008,
        ErrorCode = 0x0009,
        UnknownAttributes = 0x000A,
        Realm = 0x0014,
        Nonce = 0x0015,
        XorMappedAddress = 0x0020,
        Software = 0x8022,
        AlternateServer = 0x8023,
        Fingerprint = 0x8028,
        Priority = 0x0024, /// ICE PRIORITY attribute
        UseCandidate = 0x0025, 
        IceControlled = 0x8029,
        IceControlling = 0x802A,
    }

    public class STUNAttribute
    {
        public STUNAttribute(StunAttributeType nType)
        {
            Type = nType;
        }
        public STUNAttribute()
        {
        }


        protected StunAttributeType m_nType = 0;
        public StunAttributeType Type
        {
            get { return m_nType; }
            protected set { m_nType = value; }
        }

        protected byte[] m_bBytes = new byte[] { };
        public virtual byte[] GetBytes(STUNMessage parentmessage)
        {
            return m_bBytes;
        }
        public virtual void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
        }
    }

    public enum StunAddressFamily : byte
    {
        IPv4 = 1,
        IPv6 = 2,
    }

    public class MappedAddressAttribute : STUNAttribute
    {
        public MappedAddressAttribute()
            : base(StunAttributeType.MappedAddress)
        {
        }

        private StunAddressFamily m_eAddressFamily = StunAddressFamily.IPv4;
        public StunAddressFamily AddressFamily
        {
            get { return m_eAddressFamily; }
            set { m_eAddressFamily = value; }
        }

        protected ushort m_nPort = 0x00;

        public virtual ushort Port
        {
            get { return m_nPort; }
            set { m_nPort = value; }
        }

        protected IPAddress m_objIPAddress = new IPAddress(0);

        public virtual IPAddress IPAddress
        {
            get { return m_objIPAddress; }
            set { m_objIPAddress = value; }
        }



        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            byte[] bIPaddress = IPAddress.GetAddressBytes();
            m_bBytes = new byte[4 + bIPaddress.Length];
            m_bBytes[0] = 0;
            m_bBytes[1] = (byte)AddressFamily;
            m_bBytes[2] = (byte)((Port & 0xFF00) >> 8);
            m_bBytes[3] = (byte)(Port & 0xFF);
            Array.Copy(bIPaddress, 0, m_bBytes, 4, 4);

            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            if (m_bBytes.Length < 8) // somethin rong
                return;

            AddressFamily = (StunAddressFamily)m_bBytes[1];
            Port = (ushort)((m_bBytes[2] << 8) | (m_bBytes[3]));

            int nIPLength = 4;
            if (AddressFamily == RTP.StunAddressFamily.IPv6)
                nIPLength = 16;
            byte[] bIPAddress = new byte[nIPLength];

            Array.Copy(m_bBytes, 4, bIPAddress, 0, nIPLength);
            IPAddress = new IPAddress(bIPAddress);
        }
    }

    public class AlternateServerAttribute : MappedAddressAttribute
    {
        public AlternateServerAttribute()
            : base()
        {
            Type = StunAttributeType.AlternateServer;
        }
    }

    public class LegacySourceAddressAttribute : MappedAddressAttribute
    {
        public LegacySourceAddressAttribute()
            : base()
        {
            Type = StunAttributeType.LegacySourceAddress;
        }
    }

    public class LegacyResponseAddressAttribute : MappedAddressAttribute
    {
        public LegacyResponseAddressAttribute()
            : base()
        {
            Type = StunAttributeType.LegacyResponseAddress;
        }
    }

    public class LegacyChangeAddressAttribute : STUNAttribute
    {
        public LegacyChangeAddressAttribute()
            : base()
        {
            Type = StunAttributeType.LegacyChangeAddress;
        }

        private bool m_bChangeIP = false;

        public bool ChangeIP
        {
            get { return m_bChangeIP; }
            set { m_bChangeIP = value; }
        }

        private bool m_bChangePort = false;

        public bool ChangePort
        {
            get { return m_bChangePort; }
            set { m_bChangePort = value; }
        }

        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = new byte[4];
            if (ChangeIP == true)
                m_bBytes[3] |= 0x04;
            if (ChangePort == true)
                m_bBytes[3] |= 0x02;

            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            ChangeIP = ((m_bBytes[3] & 0x04) > 0) ? true : false;
            ChangePort = ((m_bBytes[3] & 0x02) > 0) ? true : false;
        }

    }

      public class LegacyChangedAddressAttribute : MappedAddressAttribute
    {
        public LegacyChangedAddressAttribute()
            : base()
        {
            Type = StunAttributeType.LegacyChangedAddress;
        }
    }

    public class XORMappedAddressAttribute : MappedAddressAttribute
    {
        public XORMappedAddressAttribute()
            : base()
        {
            Type = StunAttributeType.XorMappedAddress;
        }

        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            byte[] bMagicCookie = new byte[4];
            bMagicCookie[0] = (byte)((parentmessage.MagicCookie & 0xFF000000) >> 24);
            bMagicCookie[1] = (byte)((parentmessage.MagicCookie & 0x00FF0000) >> 16);
            bMagicCookie[2] = (byte)((parentmessage.MagicCookie & 0x0000FF00) >> 08);
            bMagicCookie[3] = (byte)((parentmessage.MagicCookie & 0x000000FF) >> 00);

            byte[] bIPAddress = IPAddress.GetAddressBytes();
            m_bBytes = new byte[4 + bIPAddress.Length];
            m_bBytes[0] = 0;
            m_bBytes[1] = (byte)AddressFamily;
            m_bBytes[2] = (byte)(((Port & 0xFF00) >> 8) ^ bMagicCookie[0]);
            m_bBytes[3] = (byte)((Port & 0xFF) ^ bMagicCookie[1]);

            byte[] bXOR = new byte[16];
            Array.Copy(bMagicCookie, 0, bXOR, 0, 4);
            Array.Copy(parentmessage.TransactionId, 0, bXOR, 4, parentmessage.TransactionId.Length);
            for (int i = 0; i < bIPAddress.Length; i++)
            {
                bIPAddress[i] ^= bXOR[i];
            }


            Array.Copy(bIPAddress, 0, m_bBytes, 4, bIPAddress.Length);

            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            if (m_bBytes.Length < 8) // somethin rong
                return;

            byte[] bMagicCookie = new byte[4];
            bMagicCookie[0] = (byte)((parentmessage.MagicCookie & 0xFF000000) >> 24);
            bMagicCookie[1] = (byte)((parentmessage.MagicCookie & 0x00FF0000) >> 16);
            bMagicCookie[2] = (byte)((parentmessage.MagicCookie & 0x0000FF00) >> 08);
            bMagicCookie[3] = (byte)((parentmessage.MagicCookie & 0x000000FF) >> 00);

            AddressFamily = (StunAddressFamily)m_bBytes[1];
            Port = (ushort)(((m_bBytes[2] << 8) ^ bMagicCookie[0]) | (m_bBytes[3] ^ bMagicCookie[1]));

            int nIPLength = 4;
            if (AddressFamily == RTP.StunAddressFamily.IPv6)
                nIPLength = 16;
            byte[] bIPAddress = new byte[nIPLength];

            Array.Copy(m_bBytes, 4, bIPAddress, 0, nIPLength);

            byte[] bXOR = new byte[16];
            Array.Copy(bMagicCookie, 0, bXOR, 0, 4);
            Array.Copy(parentmessage.TransactionId, 0, bXOR, 4, parentmessage.TransactionId.Length);
            for (int i = 0; i < bIPAddress.Length; i++)
            {
                bIPAddress[i] ^= bXOR[i];
            }


            IPAddress = new IPAddress(bIPAddress);
        }

    }


    public class UserNameAttribute : STUNAttribute
    {
        public UserNameAttribute()
            : base(StunAttributeType.UserName)
        {
        }

        private string m_strUserName = "";

        public string UserName
        {
            get { return m_strUserName; }
            set { m_strUserName = value; }
        }


        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = System.Text.UTF8Encoding.UTF8.GetBytes(UserName);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            UserName = System.Text.UTF8Encoding.UTF8.GetString(m_bBytes, 0, m_bBytes.Length);
        }
    }

    public class PasswordAttribute : STUNAttribute
    {
        public PasswordAttribute()
            : base(StunAttributeType.Password)
        {
        }

        private string m_strPassword = "";

        public string Password
        {
            get { return m_strPassword; }
            set { m_strPassword = value; }
        }


        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = System.Text.UTF8Encoding.UTF8.GetBytes(Password);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            Password = System.Text.UTF8Encoding.UTF8.GetString(m_bBytes, 0, m_bBytes.Length);
        }
    }

    public class MessageIntegrityAttribute : STUNAttribute
    {
        public MessageIntegrityAttribute()
            : base(StunAttributeType.MessageIntegrity)
        {
            m_bBytes = new byte[20];
        }

        public byte[] HMAC
        {
            get { return m_bBytes; }
            set { m_bBytes = value; }
        }

#if !WINDOWS_PHONE
        public void ComputeHMACLongTermCredentials(STUNMessage msgWithoutHMAC, int nLengthWithoutMessageIntegrity, string strUserName, string strRealm, string strPassword)
        {
            string strKey = string.Format("{0}:{1}:{2}", strUserName, strRealm, strPassword);
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

            byte[] bKey = md5.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(strKey));

            byte[] bBytes = msgWithoutHMAC.Bytes;

            System.Security.Cryptography.HMACSHA1 sha1 = new System.Security.Cryptography.HMACSHA1(bKey);
            HMAC = sha1.ComputeHash(bBytes, 0, nLengthWithoutMessageIntegrity);
        }
#endif

        public void ComputeHMACShortTermCredentials(STUNMessage msgWithoutHMAC, int nLengthWithoutMessageIntegrity, string strPassword)
        {
            if (strPassword == null)
                strPassword = "";

            /// No MD5 on short term credentials
            byte[] bKey = System.Text.UTF8Encoding.UTF8.GetBytes(strPassword);

            byte[] bBytes = msgWithoutHMAC.Bytes;

            System.Security.Cryptography.HMACSHA1 sha1 = new System.Security.Cryptography.HMACSHA1(bKey);
            HMAC = sha1.ComputeHash(bBytes, 0, nLengthWithoutMessageIntegrity);
        }
        public void ComputeHMACShortTermCredentials(byte [] bMsgBytes, int nLengthWithoutMessageIntegrity, string strPassword)
        {
            byte[] bKey = System.Text.UTF8Encoding.UTF8.GetBytes(strPassword);

            System.Security.Cryptography.HMACSHA1 sha1 = new System.Security.Cryptography.HMACSHA1(bKey);
            HMAC = sha1.ComputeHash(bMsgBytes, 0, nLengthWithoutMessageIntegrity);
        }

        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
        }
    }

    public class FingerPrintAttribute : STUNAttribute
    {
        public FingerPrintAttribute()
            : base(StunAttributeType.Fingerprint)
        {
            
        }

       
        // adapted from public domain source by Ross Williams and Eric Durbin
        static uint[] crctable = new uint[] 
        {
            0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA,
            0x076DC419, 0x706AF48F, 0xE963A535, 0x9E6495A3,
            0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988,
            0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91,
            0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE,
            0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7,
            0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC,
            0x14015C4F, 0x63066CD9, 0xFA0F3D63, 0x8D080DF5,
            0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172,
            0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B,
            0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940,
            0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59,
            0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116,
            0x21B4F4B5, 0x56B3C423, 0xCFBA9599, 0xB8BDA50F,
            0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
            0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D,
            0x76DC4190, 0x01DB7106, 0x98D220BC, 0xEFD5102A,
            0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433,
            0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818,
            0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
            0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E,
            0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457,
            0x65B0D9C6, 0x12B7E950, 0x8BBEB8EA, 0xFCB9887C,
            0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65,
            0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2,
            0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB,
            0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0,
            0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9,
            0x5005713C, 0x270241AA, 0xBE0B1010, 0xC90C2086,
            0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
            0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4,
            0x59B33D17, 0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD,
            0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A,
            0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683,
            0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8,
            0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1,
            0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE,
            0xF762575D, 0x806567CB, 0x196C3671, 0x6E6B06E7,
            0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC,
            0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
            0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252,
            0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B,
            0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60,
            0xDF60EFC3, 0xA867DF55, 0x316E8EEF, 0x4669BE79,
            0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
            0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F,
            0xC5BA3BBE, 0xB2BD0B28, 0x2BB45A92, 0x5CB36A04,
            0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D,
            0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A,
            0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
            0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38,
            0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21,
            0x86D3D2D4, 0xF1D4E242, 0x68DDB3F8, 0x1FDA836E,
            0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777,
            0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C,
            0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45,
            0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2,
            0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB,
            0xAED16A4A, 0xD9D65ADC, 0x40DF0B66, 0x37D83BF0,
            0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
            0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6,
            0xBAD03605, 0xCDD70693, 0x54DE5729, 0x23D967BF,
            0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94,
            0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D };

       
        private uint m_nCRC = 0;

        public uint CRC
        {
            get { return m_nCRC; }
            set { m_nCRC = value; }
        }

        public void ComputeCRC(STUNMessage msg, int nLengthWithoutFingerprint)
        {
            byte [] bMessage = msg.Bytes;
            CRC = 0xffffffff;

            for(int n = 0; n < nLengthWithoutFingerprint; ++n)
                CRC = (CRC >> 8) ^ (crctable[(CRC & 0xff) ^ bMessage[n]]);

            CRC ^= 0xffffffff;

            // XOR the result with 0x5354554e
            CRC ^= 0x5354554e;
        }

        public void ComputeCRC(byte [] bMessage, int nLengthWithoutFingerprint)
        {
            CRC = 0xffffffff;

            for (int n = 0; n < nLengthWithoutFingerprint; ++n)
                CRC = (CRC >> 8) ^ (crctable[(CRC & 0xff) ^ bMessage[n]]);

            CRC ^= 0xffffffff;

            // XOR the result with 0x5354554e
            CRC ^= 0x5354554e;
        }

        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = SocketServer.TLS.ByteHelper.GetBytesForInt32((int)CRC, SocketServer.TLS.Endianess.Big);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            CRC = (uint)SocketServer.TLS.ByteHelper.ReadUintBigEndian(bBytes, 0);
        }
       
    }

    public class ErrorCodeAttribute : STUNAttribute
    {
        public ErrorCodeAttribute()
            : base(StunAttributeType.ErrorCode)
        {
        }

        private byte m_bClass = 0;

        public byte Class
        {
            get { return m_bClass; }
            set { m_bClass = value; }
        }
        private byte m_bNumber = 0;

        public byte Number
        {
            get { return m_bNumber; }
            set { m_bNumber = value; }
        }
        private string m_strReasonPhrase = "";

        public string ReasonPhrase
        {
            get { return m_strReasonPhrase; }
            set { m_strReasonPhrase = value; }
        }

        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            byte[] bPhrase = System.Text.UTF8Encoding.UTF8.GetBytes(ReasonPhrase);
            m_bBytes = new byte[bPhrase.Length + 4];
            m_bBytes[2] = (byte)(Class & 0x07);
            m_bBytes[2] = (byte)Number;
            Array.Copy(bPhrase, 0, m_bBytes, 1, bPhrase.Length);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            Class = (byte)(bBytes[2] & 0x07);
            Number = bBytes[3];
            ReasonPhrase = System.Text.UTF8Encoding.UTF8.GetString(m_bBytes, 1, m_bBytes.Length - 1);
        }
    }


    public class RealmAttribute : STUNAttribute
    {
        public RealmAttribute()
            : base(StunAttributeType.Realm)
        {
        }

        private string m_strRealm = "";

        public string Realm
        {
            get { return m_strRealm; }
            set { m_strRealm = value; }
        }


        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = System.Text.UTF8Encoding.UTF8.GetBytes(Realm);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            Realm = System.Text.UTF8Encoding.UTF8.GetString(m_bBytes, 0, m_bBytes.Length);
        }
    }

    public class NonceAttribute : STUNAttribute
    {
        public NonceAttribute()
            : base(StunAttributeType.Nonce)
        {
        }

        private string m_strNonce = "";

        public string Nonce
        {
            get { return m_strNonce; }
            set { m_strNonce = value; }
        }


        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = System.Text.UTF8Encoding.UTF8.GetBytes(Nonce);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            Nonce = System.Text.UTF8Encoding.UTF8.GetString(m_bBytes, 0, m_bBytes.Length);
        }
    }


    public class SoftwareAttribute : STUNAttribute
    {
        public SoftwareAttribute()
            : base(StunAttributeType.Software)
        {
        }

        private string m_strSoftware = "";

        public string Software
        {
            get { return m_strSoftware; }
            set { m_strSoftware = value; }
        }


        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = System.Text.UTF8Encoding.UTF8.GetBytes(Software);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            Software = System.Text.UTF8Encoding.UTF8.GetString(m_bBytes, 0, m_bBytes.Length);
        }
    }

    public class PriorityAttribute : STUNAttribute
    {
        public PriorityAttribute()
            : base(StunAttributeType.Priority)
        {
        }

        private int m_nPriority = 0;

        public int Priority
        {
            get { return m_nPriority; }
            set { m_nPriority = value; }
        }


        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            m_bBytes = SocketServer.TLS.ByteHelper.GetBytesForInt32(Priority, SocketServer.TLS.Endianess.Big);
            return m_bBytes;
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            Priority = (int) SocketServer.TLS.ByteHelper.ReadUintBigEndian(bBytes, 0);
        }
    }

    public class UseCandidateAttribute : STUNAttribute
    {
        public UseCandidateAttribute()
            : base(StunAttributeType.UseCandidate)
        {
        }

        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            return new byte[] {};
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
        }
    }

    public class IceControlledAttribute : STUNAttribute
    {
        public IceControlledAttribute()
            : base(StunAttributeType.IceControlled)
        {
            Random rand = new Random();
            byte [] Random = new byte[8];
            rand.NextBytes(Random);
            RandomNumber = SocketServer.TLS.ByteHelper.ReadULongBigEndian(Random, 0);
        }

        public ulong RandomNumber = 0;
        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            return SocketServer.TLS.ByteHelper.GetBytesForUint64(RandomNumber, SocketServer.TLS.Endianess.Big);
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            RandomNumber = SocketServer.TLS.ByteHelper.ReadULongBigEndian(m_bBytes, 0);
        }
    }

    public class IceControllingAttribute : STUNAttribute
    {
        public IceControllingAttribute()
            : base(StunAttributeType.IceControlling)
        {
            Random rand = new Random();
            byte[] Random = new byte[8];
            rand.NextBytes(Random);
            RandomNumber = SocketServer.TLS.ByteHelper.ReadULongBigEndian(Random, 0);
        }

        public ulong RandomNumber = 0;
        public override byte[] GetBytes(STUNMessage parentmessage)
        {
            return SocketServer.TLS.ByteHelper.GetBytesForUint64(RandomNumber, SocketServer.TLS.Endianess.Big);
        }

        public override void SetBytes(byte[] bBytes, STUNMessage parentmessage)
        {
            m_bBytes = bBytes;
            RandomNumber = SocketServer.TLS.ByteHelper.ReadULongBigEndian(m_bBytes, 0);
        }
    }


    public class STUNAttributeContainer
    {
        public STUNAttributeContainer()
        {
        }

        public STUNAttributeContainer(STUNAttribute attr)
        {
            SetAttribute(attr);
        }

        void SetAttribute(STUNAttribute attr)
        {
            StunAttributeType = attr.Type;
            m_objParsedAttribute = attr;
        }

        private StunAttributeType m_eStunAttributeType = StunAttributeType.UnknownAttributes;

        public StunAttributeType StunAttributeType
        {
            get { return m_eStunAttributeType; }
            set { m_eStunAttributeType = value; }
        }
        private ushort m_nLength;

        public ushort Length
        {
            get { return m_nLength; }
            set { m_nLength = value; }
        }
        public byte[] m_bValue = new byte[] { };
        private STUNAttribute m_objParsedAttribute = new STUNAttribute();

        public STUNAttribute ParsedAttribute
        {
            get { return m_objParsedAttribute; }
            set { m_objParsedAttribute = value; }
        }


        public byte[] GetBytes(STUNMessage parentmessage, bool bDwordAlign)
        {
            byte[] bAttribute = m_objParsedAttribute.GetBytes(parentmessage);
            int nLength = bAttribute.Length + 4;
            if (bDwordAlign == true)
            {
                while ((nLength % 4) != 0)
                    nLength++;
            }

            m_bValue = new byte[nLength];
            Length = (ushort) bAttribute.Length;
            m_bValue[0] = (byte)(((int)m_objParsedAttribute.Type & 0xFF00) >> 8);
            m_bValue[1] = (byte)(((int)m_objParsedAttribute.Type & 0x00FF) >> 0);

            m_bValue[2] = (byte)(((int)Length & 0xFF00) >> 8);
            m_bValue[3] = (byte)(((int)Length & 0x00FF) >> 0);

            Array.Copy(bAttribute, 0, m_bValue, 4, bAttribute.Length);

            return m_bValue;
        }

        /// <summary>
        /// Read the next attribute from the stream
        /// </summary>
        /// <param name="bBytes"></param>
        /// <param name="parentmessage"></param>
        /// <returns>The number of bytes read, or 0 if no more can be read</returns>
        public int ReadFromBytes(byte[] bBytes, int nAt, bool bDwordAlign, STUNMessage parentmessage)
        {
            if ((bBytes.Length - nAt) < 4)
                return 0;

            ushort nType = (ushort)((bBytes[0 + nAt] << 8) | bBytes[1 + nAt]);
            ushort nLength = (ushort)((bBytes[2 + nAt] << 8) | bBytes[3 + nAt]);

            if (bDwordAlign == true)
            {
                while ((nLength % 4) != 0)
                    nLength++;
            }

            if ((bBytes.Length-nAt) < (nLength - 4))
                return 0;

            byte[] bAttribute = new byte[nLength];
            Array.Copy(bBytes, 4+nAt, bAttribute, 0, bAttribute.Length);

            BuildAttribute(nType, bAttribute, bDwordAlign, parentmessage);

            return 4 + nLength;
        }

        void BuildAttribute(ushort nType, byte[] bAttribute, bool bDwordAlign, STUNMessage parentmessage)
        {
            if (nType == (ushort)StunAttributeType.MappedAddress)
            {
                m_objParsedAttribute = new MappedAddressAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.AlternateServer)
            {
                m_objParsedAttribute = new AlternateServerAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.ErrorCode)
            {
                m_objParsedAttribute = new ErrorCodeAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.MessageIntegrity)
            {
                m_objParsedAttribute = new MessageIntegrityAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.Fingerprint)
            {
                m_objParsedAttribute = new FingerPrintAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.Nonce)
            {
                m_objParsedAttribute = new NonceAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.Realm)
            {
                m_objParsedAttribute = new RealmAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.Software)
            {
                m_objParsedAttribute = new SoftwareAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.UserName)
            {
                m_objParsedAttribute = new UserNameAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.Password)
            {
                m_objParsedAttribute = new PasswordAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.XorMappedAddress)
            {
                m_objParsedAttribute = new XORMappedAddressAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.LegacySourceAddress)
            {
                m_objParsedAttribute = new LegacySourceAddressAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }            
            else if (nType == (ushort)StunAttributeType.LegacyResponseAddress)
            {
                m_objParsedAttribute = new LegacyResponseAddressAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }            
            else if (nType == (ushort)StunAttributeType.LegacyChangeAddress)
            {
                m_objParsedAttribute = new LegacyChangeAddressAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.LegacyChangedAddress)
            {
                m_objParsedAttribute = new LegacyChangedAddressAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.Priority)
            {
                m_objParsedAttribute = new PriorityAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.UseCandidate)
            {
                m_objParsedAttribute = new UseCandidateAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.IceControlled)
            {
                m_objParsedAttribute = new IceControlledAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
            else if (nType == (ushort)StunAttributeType.IceControlling)
            {
                m_objParsedAttribute = new IceControllingAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }            
               
            else 
            {
                m_objParsedAttribute = new STUNAttribute();
                m_objParsedAttribute.SetBytes(bAttribute, parentmessage);
            }
        }
    }


    public class STUNMessage
    {
        public STUNMessage()
        {
            new Random().NextBytes(TransactionId);
            MagicCookie = ConstMagicCookie;
        }

        public STUNMessage(byte[] bTransactionId)
        {
            if ((bTransactionId == null) || (bTransactionId.Length <= 0))
                new Random().NextBytes(TransactionId);
            else
            {
                int nCopyLen = (bTransactionId.Length < 8) ? bTransactionId.Length : 8;
                Array.Copy(bTransactionId, 0, TransactionId, 0, nCopyLen);
            }
            MagicCookie = ConstMagicCookie;
        }

        public STUNAttribute FindAttribute(StunAttributeType type)
        {
            foreach (STUNAttributeContainer cont in Attributes)
            {
                if (cont.ParsedAttribute.Type == type)
                {
                    return cont.ParsedAttribute;
                }
            }
            return null;
        }

        public const uint ConstMagicCookie = 0x2112A442;

        private ushort m_nMessageType; // most significant 2 bits must be 0  (Network Byte order - big endian)

        protected bool DWORDAligned = false;
        /// <summary>
        ///  Used for setting the raw message type, Use Method and class to parse out these bits
        /// </summary>
        public ushort MessageType
        {
            get { return m_nMessageType; }
            set { m_nMessageType = value; }
        }

        // bits 0,1,2,3,5,6,8,9,10,11,12,13 of MessageType
        // pos  0,1,2,3,4,5,6,7,8, 9, 10,11
        public StunMethod Method
        {
            get
            {
                ushort nValue = (ushort)(GetBitValueOffsetToNewValue(MessageType, 0, 0) |
                                    GetBitValueOffsetToNewValue(MessageType, 1, 1) |
                                    GetBitValueOffsetToNewValue(MessageType, 2, 2) |
                                    GetBitValueOffsetToNewValue(MessageType, 3, 3) |
                                    GetBitValueOffsetToNewValue(MessageType, 4, 5) |
                                    GetBitValueOffsetToNewValue(MessageType, 5, 6) |
                                    GetBitValueOffsetToNewValue(MessageType, 6, 8) |
                                    GetBitValueOffsetToNewValue(MessageType, 7, 9) |
                                    GetBitValueOffsetToNewValue(MessageType, 8, 10) |
                                    GetBitValueOffsetToNewValue(MessageType, 9, 11) |
                                    GetBitValueOffsetToNewValue(MessageType, 10, 12) |
                                    GetBitValueOffsetToNewValue(MessageType, 11, 13));
                return (StunMethod)nValue;
            }
            set
            {
                ushort nValue = (ushort)value;
                // 1111 1110 1110 1111b = 0xFEEF
                // 0000 0001 0001 0000b = 0x110
                MessageType &= 0x110; /// Clear the bits, everything but the class

                if ((nValue & 0x01) > 0) // get bit 0, set at 0
                    MessageType |= (1 << 0);
                else if ((nValue & 0x02) > 0) // get bit 1, set at 1
                    MessageType |= (1 << 1);
                else if ((nValue & 0x04) > 0) // get bit 2, set at 2
                    MessageType |= (1 << 2);
                else if ((nValue & 0x08) > 0) // get bit 3, set at 3
                    MessageType |= (1 << 3);
                else if ((nValue & 0x10) > 0) // get bit 4, set at 5
                    MessageType |= (1 << 5);
                else if ((nValue & 0x20) > 0) // get bit 5, set at 6
                    MessageType |= (1 << 6);
                else if ((nValue & 0x40) > 0) // get bit 6, set at 8
                    MessageType |= (1 << 8);
                else if ((nValue & 0x80) > 0) // get bit 7, set at 9
                    MessageType |= (1 << 9);
                else if ((nValue & 0x100) > 0) // get bit 8, set at 10
                    MessageType |= (1 << 10);
                else if ((nValue & 0x200) > 0) // get bit 9, set at 11
                    MessageType |= (1 << 11);
                else if ((nValue & 0x400) > 0) // get bit 10, set at 12
                    MessageType |= (1 << 12);
                else if ((nValue & 0x800) > 0) // get bit 11, set at 13
                    MessageType |= (1 << 13);

            }
        }

        // bits 4,8 of MessageType
        public StunClass Class
        {
            get
            {
                byte bValue = (byte)(GetBitValueOffsetToNewValue(MessageType, 4, 0) |
                                    GetBitValueOffsetToNewValue(MessageType, 8, 1));
                bValue &= 0x3; // only bottom 2 bits mean anything

                return (StunClass)bValue;
            }
            set
            {
                byte bValue = (byte)value;
                // 0000 0001 0001 0000b = 0x110
                // 1111 1110 1110 1111b = 0xFEEF
                MessageType &= 0xFEEF; /// Clear the bits
                if ((bValue & 2) > 0)
                    MessageType |= 0x100;    // 1 0000 0000b
                if ((bValue & 1) > 0)
                    MessageType |= 0x010;    // 0 0001 0000b

            }
        }

        public static int GetBitValueOffsetToNewValue(int nValue, int nBitToGet, int nBitToPlace)
        {
            int nOr = (1 << nBitToGet);
            if ((nValue & nOr) > 0) /// bit is set
            {
                return (1 << nBitToPlace);
            }
            else
            {
                return 0;
            }
        }

        private ushort m_nMessageLength;
        /// <summary>
        ///  Size in bytes of the message not including the 20 byte header... We'll set this at build time and parse byte time
        /// </summary>
        public ushort MessageLength
        {
            get { return m_nMessageLength; }
            set { m_nMessageLength = value; }
        }

        private uint m_nMagicCookie;

        public uint MagicCookie
        {
            get { return m_nMagicCookie; }
            set { m_nMagicCookie = value; }
        }
        public byte[] TransactionId = new byte[12];

        public void AddAttribute(STUNAttribute attr)
        {
            STUNAttributeContainer cont = new STUNAttributeContainer(attr);
            Attributes.Add(cont);
        }


        /// 0 or more attributes (Type-Length-Value)
        /// 
        private List<STUNAttributeContainer> m_ListAttributes = new List<STUNAttributeContainer>();

        public List<STUNAttributeContainer> Attributes
        {
            get { return m_ListAttributes; }
            set { m_ListAttributes = value; }
        }



        public virtual byte[] Bytes
        {
            get
            {
                int nTotalAttributeLength = 0;
                List<byte[]> AttributeBytes = new List<byte[]>();
                foreach (STUNAttributeContainer attr in Attributes)
                {
                    byte [] bAttrBytes = attr.GetBytes(this, this.DWORDAligned);
                    nTotalAttributeLength += bAttrBytes.Length;
                    AttributeBytes.Add(bAttrBytes);
                }

                MessageLength = (ushort) nTotalAttributeLength;

                byte[] bRet = new byte[20 + nTotalAttributeLength];
                bRet[0] = (byte)((MessageType & 0xFF00) >> 8);
                bRet[1] = (byte)((MessageType & 0x00FF) >> 0);

                bRet[2] = (byte)((MessageLength & 0xFF00) >> 8);
                bRet[3] = (byte)((MessageLength & 0x00FF) >> 0);
              
                bRet[4] = (byte)((MagicCookie & 0xFF000000) >> 24);
                bRet[5] = (byte)((MagicCookie & 0x00FF0000) >> 16);
                bRet[6] = (byte)((MagicCookie & 0x0000FF00) >> 08);
                bRet[7] = (byte)((MagicCookie & 0x000000FF) >> 00);

                Array.Copy(TransactionId, 0, bRet, 8, 12);

                int nAt = 20;
                foreach (byte[] bNextAttrBytes in AttributeBytes)
                {
                    Array.Copy(bNextAttrBytes, 0, bRet, nAt, bNextAttrBytes.Length);
                    nAt += bNextAttrBytes.Length;
                }
                return bRet;
            }
            set
            {
                if (value.Length < 20)
                    throw new Exception("Stun message must be at least 20 bytes");

                Attributes.Clear();

                /// Parse this out from the provide array
                /// 
                MessageType = (ushort)((value[0] << 8) | (value[1]));
                MessageLength = (ushort)((value[2] << 8) | (value[3]));
                MagicCookie = (uint)((value[4] << 24) | (value[5] << 16) | (value[6] << 8) | (value[7]));

                Array.Copy(value, 8, TransactionId, 0, 12);

                if (MessageLength <= 0)
                    return;

                byte[] bAttributes = new byte[MessageLength];
                Array.Copy(value, 20, bAttributes, 0, MessageLength);
                int nAt = 0;
                while (nAt < MessageLength)
                {
                    STUNAttributeContainer cont = new STUNAttributeContainer();
                    int nRead = cont.ReadFromBytes(bAttributes, nAt, this.DWORDAligned, this);

                    if (nRead <= 0)
                        break;

                    nAt += nRead;
                    Attributes.Add(cont);
                }

            }
        }

    }


    // Similar to a stun message, but all attributes are dword aligned - not sure why people think this is a good idea today
    public class STUN2Message : STUNMessage
    {
        public STUN2Message() : base()
        {
            DWORDAligned = true;
        }

        public STUN2Message(byte[] bTransactionId) : base(bTransactionId)
        {
            DWORDAligned = true;
        }
    }

}
