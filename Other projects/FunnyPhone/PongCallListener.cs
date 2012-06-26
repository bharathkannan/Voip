using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using VoIP;
//using VoIP;

namespace FunnyPhone
{
    class PongCallListener : IPhoneCallListener
    {
        public void DtmfReceived(object sender, VoIPEventArgs<DTMF> e)
        {
            var dtmf = e.Item;
            var call = (PhoneCall)sender;
            Console.WriteLine("Dtmf received");
            call.SendDTMFSignal(VoIPMediaType.Audio, e.Item);
        }

        public void CallErrorOccured(object sender, VoIPEventArgs<CallError> e)
        {
            var call = (PhoneCall)sender;
            Console.WriteLine("Call error occured: " + e.Item);
        }

        public void MediaDataReceived(object sender, VoIPEventArgs<VoIPMediaData> e)
        {
            var call = (PhoneCall)sender;
            call.SendMediaData(e.Item.MediaType, e.Item.PCMData);
        }

        public void CallStateChanged(object sender, VoIPEventArgs<CallState> e)
        {
            var call = (PhoneCall)sender;
            Console.WriteLine("Call state changed: " + e.Item);

            if (e.Item > CallState.InCall)
                call.DetachListener(this);
        }

        public void PlainMediaDataReceived(object sender, VoIPEventArgs<EncodedMediaData> e)
        {
        }

    }
}
