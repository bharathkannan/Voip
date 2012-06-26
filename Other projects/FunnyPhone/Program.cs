using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VoIP;
using VoIP.SDK;

namespace FunnyPhone
{
    class Program
    {
        static Dictionary<string,IPhoneCall> Calls;
        static PongCallListener FunnyCallListener;

        static void Main(string[] args)
        {
            ISoftPhone SoftPhone = new SoftPhone("", 5000, 8000, 5060);
            //ISoftPhone SoftPhone = new VoIP.SDK.Mock.ArbSoftPhone();
            SoftPhone.IncommingCall += (SoftPhone_IncommingCall);
            IPhoneLine PhoneLine = null;
            FunnyCallListener = new PongCallListener();

            Calls = new Dictionary<string, IPhoneCall>();

            Console.WriteLine("Be funny!");

            Console.Write("Display name: "); string displayName = Console.ReadLine();
            Console.Write("Username: "); string username = Console.ReadLine();
            Console.Write("Register name: "); string registerName = Console.ReadLine();
            Console.Write("Register password: "); string registerPassword = Console.ReadLine();
            Console.Write("Domain server: "); string domainServer = Console.ReadLine();

            string[] domains = domainServer.Split(':');
            int port = 5060;
            if (domains.Length == 2)
                port = Int32.Parse(domains[1]);
            SIPAccount account = new SIPAccount(true, displayName, username, registerName, registerPassword, domains[0], port);
            PhoneLine = SoftPhone.CreateAndRegisterPhoneLine(account);

            while (true)
            {
                string statement = Console.ReadLine().Trim();
                if (statement.StartsWith("exit"))
                    break;

                if (!Calls.ContainsKey(statement))
                {
                    if (PhoneLine.RegisteredInfo == PhoneLineInformation.RegistrationSucceded)
                    {
                        IPhoneCall Call = SoftPhone.CreateCallObject(PhoneLine, statement, FunnyCallListener);
                        Calls.Add(statement, Call);
                        Call.CallStateChanged += (Call_CallStateChanged);
                        Call.Start();
                    }
                }
            }
            foreach (IPhoneCall call in Calls.Values)
                call.HangUp();
            SoftPhone.Close();
        }

        static void SoftPhone_IncommingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            e.Item.AttachListener(FunnyCallListener);
            e.Item.CallStateChanged += (Call_CallStateChanged);
            Calls.Add(e.Item.DialInfo, e.Item);
            e.Item.Accept();
        }

        static void Call_CallStateChanged(object sender, VoIPEventArgs<CallState> e)
        {
            if (e.Item > CallState.InCall)
            {
                IPhoneCall call = sender as IPhoneCall;
                if (call == null)
                    return;

                Calls.Remove(call.DialInfo);

                call.DetachListener(FunnyCallListener);
                call.CallStateChanged -= (Call_CallStateChanged);
            }
        }

    }
}
