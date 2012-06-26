using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioClasses
{
    /// <summary>
    ///  A really bad resampler for 8000 to 16000.  Our C++ classes have one using IPP for windows programs to use
    ///  May add other rates later as well
    /// </summary>
    public class AudioResampler
    {
        public AudioResampler()
        {
        }

        public virtual MediaSample Resample(MediaSample sample, AudioFormat outformat)
        {
            if ((sample.AudioFormat.AudioSamplingRate == AudioSamplingRate.sr16000) && (outformat.AudioSamplingRate == AudioSamplingRate.sr8000))
            {
                /// Downsample the data
                /// 
                short[] sData = Utils.Resample16000To8000(sample.GetShortData());

                return new MediaSample(sData, outformat);
            }
            else if ((sample.AudioFormat.AudioSamplingRate == AudioSamplingRate.sr16000) && (outformat.AudioSamplingRate == AudioSamplingRate.sr8000))
            {
                /// Upsample the data.  This shouldn't happen because our incoming data should always be higher or equal quality
                /// 
                short[] sData = Utils.Resample8000To16000(sample.GetShortData());
                return new MediaSample(sData, outformat);
            }
            return sample;
        }
    }
}
