using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.XMPP;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using AudioClasses;

namespace Tests
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //TestIQErrorParsing();
            //TestPubSubParsing();
            //TestCapture();
            
            //TestCorrelation();
            //TestCorrelationScaledMix();
            //TestHMAC();
            TestCRC32();
        }

        static void TestCapture()
        {
            WPFImageWindows.ScreenGrabUtility.GetScreenPNG();
        }

        static void TestIQErrorParsing()
        {
            IQ iqerror = new IQ();
            iqerror.From = "me@no.where";
            iqerror.To = "you@over.there";
            iqerror.Type = IQType.result.ToString();
            iqerror.Error = new Error(ErrorType.badformat);
            iqerror.Error.Code = "405";
            iqerror.Error.Type = "typeeror";

            string strXML = Utility.GetXMLStringFromObject(iqerror);
            Console.WriteLine(strXML);

            IQ iqnew = Utility.ParseObjectFromXMLString(strXML, typeof(IQ)) as IQ;
            ErrorType type = iqnew.Error.ErrorDescription.ErrorType;

            System.Diagnostics.Debug.Assert(iqnew.Error.Code == "405");
            System.Diagnostics.Debug.Assert(type == ErrorType.badformat);

        }

        static void TestPubSubParsing()
        {
            PubSubIQ iq = new PubSubIQ();
            iq.From = "me@no.where";
            iq.To = "you@over.there";
            iq.Type = IQType.set.ToString();

            GroceryItem item = new GroceryItem() { Name = "onions", Person = "Me", Price = "$10.02" };

            iq.PubSub.Publish = new Publish();
            iq.PubSub.Publish.Item = new PubSubItem();
            iq.PubSub.Publish.Item.SetNodeFromObject(item);
            iq.PubSub.Publish.Item.Id = "my_super_id";



            string strXML = Utility.GetXMLStringFromObject(iq);
            Console.WriteLine(strXML);

            PubSubIQ iqnew = Utility.ParseObjectFromXMLString(strXML, typeof(PubSubIQ)) as PubSubIQ;
            GroceryItem newitem = iqnew.PubSub.Publish.Item.GetObjectFromXML<GroceryItem>();

            System.Diagnostics.Debug.Assert(newitem.Name == item.Name);
        }


        /// <summary>
        /// Test that a source signal can be found in a mix where the source is mixed in at full amplitude
        /// </summary>
        static void TestCorrelation()
        {
            short[] sRTPIn = ReadPCMFile("../../Incoming16x16pcm.pcm");
            short[] sMicIn = ReadPCMFile("../../Mic_Mix16x16pcm.pcm");


            int nMaxAt = 0;
            float[] fData = SimpleEchoCanceller.CrossCorrelation(sMicIn, sMicIn.Length, sRTPIn, out nMaxAt);
            float fValue = fData[nMaxAt];
            /// subtract the array and see what we get
            /// 
            for (int i = nMaxAt; i < nMaxAt + sRTPIn.Length; i++)
            {
                sMicIn[i] -= sRTPIn[i - nMaxAt];
            }
            
            byte[] bData = Utils.ConvertShortArrayToByteArray(sMicIn);
            FileStream stream = new FileStream("../../output.pcm", FileMode.Create);
            stream.Write(bData, 0, bData.Length);
            stream.Close();
        }

        /// <summary>
        /// Mic_Mix_amp_16x16pcm.pcm has some normal audio and the Incoming audio mixed in at 60% amplitude.  This test
        /// finds the input in the mix, finds what it scaled to, removes it and saves the file.
        /// A manual comparison must be done to see that it is the same since I'm too lazy to code it
        /// </summary>
        static void TestCorrelationScaledMix()
        {
            short[] sRTPIn = ReadPCMFile("../../Incoming16x16pcm.pcm");
            short[] sMicIn = ReadPCMFile("../../Mic_Mix_amp_16x16pcm.pcm");

            float fAutoCorrelationValue = SimpleEchoCanceller.GetSelfCorrelationValue(sRTPIn);

            int nMaxAt = 0;
            float[] fData = SimpleEchoCanceller.CrossCorrelation(sMicIn, sMicIn.Length, sRTPIn, out nMaxAt);

            float fValue = fData[nMaxAt];

            float fScaleValue = fValue/fAutoCorrelationValue;

            SimpleEchoCanceller.ScaleArray(fScaleValue, sRTPIn);
            /// subtract the array and see what we get
            /// 
            for (int i = nMaxAt; i < nMaxAt + sRTPIn.Length; i++)
            {
                sMicIn[i] -= sRTPIn[i - nMaxAt];
            }

            byte[] bData = Utils.ConvertShortArrayToByteArray(sMicIn);
            FileStream stream = new FileStream("../../output.pcm", FileMode.Create);
            stream.Write(bData, 0, bData.Length);
            stream.Close();

            //SimpleEchoCanceller can = new SimpleEchoCanceller(AudioFormat.SixteenBySixteenThousandMono, TimeSpan.FromMilliseconds(100));
        }

     

        static short[] ReadPCMFile(string strFileName)
        {
            FileStream stream = new FileStream(strFileName, FileMode.Open);
            byte[] bData = new byte[stream.Length];
            stream.Read(bData, 0, bData.Length);
            stream.Close();

            return AudioClasses.Utils.ConvertByteArrayToShortArrayLittleEndian(bData);
        }

        /// <summary>
        /// Test function for STUN message integrity
        /// </summary>
        static void TestHMAC()
        {
            string RemotePassword = "6j4036s66j3jqfqn3n5bb457t2";
            string Password = "LIbovkUZpfMCL9py";


            string strAllBytes = "000100582112a442540ba8d83601ad2d9b63a731002400046e000aff802a00087c5b0d397c1780900006000e79505051513857623a3569396971000080220009696365346a2e6f7267000000000800140dc86c4e0e5f98f9476a746f389bbdd80e7213da802800047df4e33a";
            string strBytes = "000100502112a442540ba8d83601ad2d9b63a731002400046e000aff802a00087c5b0d397c1780900006000e79505051513857623a3569396971000080220009696365346a2e6f7267000000";

            byte [] bBytes = SocketServer.TLS.ByteHelper.ByteFromHexString(strBytes);

            RTP.MessageIntegrityAttribute macattr = new RTP.MessageIntegrityAttribute();
            macattr.ComputeHMACShortTermCredentials(bBytes, bBytes.Length, Password);

            string strHMACshouldbe = "0dc86c4e0e5f98f9476a746f389bbdd80e7213da";
            
        }

        /// <summary>
        /// Test function for STUN fingerprint
        /// </summary>
        static void TestCRC32()
        {
            string strAllBytes = "000100582112a442540ba8d83601ad2d9b63a731002400046e000aff802a00087c5b0d397c1780900006000e79505051513857623a3569396971000080220009696365346a2e6f7267000000000800140dc86c4e0e5f98f9476a746f389bbdd80e7213da802800047df4e33a";
            string strBytes = "000100582112a442540ba8d83601ad2d9b63a731002400046e000aff802a00087c5b0d397c1780900006000e79505051513857623a3569396971000080220009696365346a2e6f7267000000000800140dc86c4e0e5f98f9476a746f389bbdd80e7213da";

            byte[] bBytes = SocketServer.TLS.ByteHelper.ByteFromHexString(strBytes);

            RTP.FingerPrintAttribute fattr = new RTP.FingerPrintAttribute();
            fattr.ComputeCRC(bBytes, bBytes.Length);

            int nCRCSHouldBe = 0x7df4e33a;

        }

    }

    


    [DataContract]
    [XmlRoot(ElementName = "groceryitem", Namespace="http://testnamsapce.com")]
    public class GroceryItem
    {
        public GroceryItem()
        {
        }

        private string m_strName = null;
        [XmlElement(ElementName = "name")]
        [DataMember]
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }

        private bool m_bIsAccountedFor = false;
        [XmlElement(ElementName = "isaccountedfor")]
        [DataMember]
        public bool IsAccountedFor
        {
            get { return m_bIsAccountedFor; }
            set { m_bIsAccountedFor = value; }
        }

        private string m_strPerson = "";
        /// <summary>
        /// The person who has last modified this item
        /// </summary>
        [XmlElement(ElementName = "person")]
        [DataMember]
        public string Person
        {
            get { return m_strPerson; }
            set { m_strPerson = value; }
        }

        private string m_strPrice = "";
        /// <summary>
        /// The price that was paid or will be paid
        /// </summary>
        [XmlElement(ElementName = "price")]
        [DataMember]
        public string Price
        {
            get { return m_strPrice; }
            set { m_strPrice = value; }
        }

        private string m_strItemId = Guid.NewGuid().ToString();
        [XmlElement(ElementName = "itemid")]
        [DataMember]
        public string ItemId
        {
            get { return m_strItemId; }
            set { m_strItemId = value; }
        }
    }
}
