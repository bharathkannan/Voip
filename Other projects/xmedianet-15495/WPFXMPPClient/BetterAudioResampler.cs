using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AudioClasses;
using ImageAquisition;

namespace WPFXMPPClient
{
    public class BetterAudioResampler : AudioResampler
    {
        public BetterAudioResampler() : base()
        {
        }

        ImageAquisition.SampleConvertor Converter = null;

        public override MediaSample Resample(MediaSample sample, AudioFormat outformat)
        {
            if (outformat == sample.AudioFormat)
                return sample;

            short [] sData = sample.GetShortData();
            if (Converter == null)
            {
                /// Assume we will always get the same size data to resample, and the same in/out formats
                /// 
                Converter = new SampleConvertor(((int)sample.AudioFormat.AudioSamplingRate) / 100, ((int)outformat.AudioSamplingRate) / 100, sData.Length);
            }

            short[] sConverted = Converter.Convert(sData);

            return new MediaSample(sConverted, outformat);
        }

    }
}
