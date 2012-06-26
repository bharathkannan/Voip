/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AudioClasses;
using SocketServer;

namespace RTP
{
   
    public class PushPullObject
    {
        public PushPullObject(IAudioSource source, IAudioSink sink)
        {
            AudioSource = source;
            AudioSink = sink;

            SourceExcludeList.Add(AudioSource);
        }

        // A list of audio sources that should be subtracted from being played to this sinc
        public List<IAudioSource> SourceExcludeList = new List<IAudioSource>();

        private IAudioSource m_objAudioSource = null;
        public IAudioSource AudioSource
        {
          get { return m_objAudioSource; }
          set { m_objAudioSource = value; }
        }

        private IAudioSink m_objAudioSink = null;
        public IAudioSink AudioSink
        {
          get { return m_objAudioSink; }
          set { m_objAudioSink = value; }
        }
    }

    /// <summary>
    ///  Combines audio from several sources into one stream, and ouputs that to all the listeners
    ///  Right now, supports 16x16 only
    /// </summary>
    public class AudioConferenceMixer
    {
        public AudioConferenceMixer(AudioFormat format)
        {
            AudioFormat = format;
        }

        protected AudioFormat AudioFormat = AudioFormat.SixteenBySixteenThousandMono;

        /// <summary>
        /// Adds a source/sink combination to this muxer
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sink"></param>
        public PushPullObject AddInputOutputSource(IAudioSource source, IAudioSink sink)
        {
            PushPullObject member = new PushPullObject(source, sink);
            lock (MemberLock)
            {
                Members.Add(member);
            }
            return member;
        }

        public void RemoveInputOutputSource(IAudioSource source, IAudioSink sink)
        {
            lock (MemberLock)
            {
                PushPullObject removeobject = null;
                foreach (PushPullObject ppo in Members)
                {
                    if ((ppo.AudioSource == source) && (ppo.AudioSink == sink))
                    {
                        removeobject = ppo;
                        break;
                    }
                }

                if (removeobject != null)
                {
                    Members.Remove(removeobject);
                }
            }
        }

        object MemberLock = new object();
        List<PushPullObject> Members = new List<PushPullObject>();

        private object m_objCustom = null;

        public object CustomObject
        {
            get { return m_objCustom; }
            set { m_objCustom = value; }
        }


        IMediaTimer SendTimer = null;
        object TimerLock = new object();

        
        /// <summary>
        ///  Start our timer to push/pull traffic
        /// </summary>
        public void Start()
        {
            lock (TimerLock)
            {
                if (SendTimer != null)
                {
                    SendTimer.Cancel();
                    SendTimer = null;
                }
            }


            SendTimer = SocketServer.QuickTimerControllerCPU.CreateTimer(20, new SocketServer.DelegateTimerFired(OnTimeToPushPacket), "", null);
        }

        public void Stop()
        {
            lock (TimerLock)
            {
                if (SendTimer != null)
                {
                    SendTimer.Cancel();
                    SendTimer = null;
                }
            }
        }


        public void OnTimeToPushPacket(IMediaTimer timer)
        {
            DoPushPull(new TimeSpan(0, 0, 0, 0, 20));
        }

        ///  Push and Pull Sample aren't used.  We pull from the ConferenceMemberInputFilters on a timer and
        ///  push to the ConferencememberOutputFilters at the same time
        ///  

        object PushPullLock = new object();
        /// <summary>
        /// Pull from all our input pins, then combine, then subtract
        /// </summary>
        /// <param name="tsElapsed"></param>
        void DoPushPull(TimeSpan tsElapsed)
        {
            lock (PushPullLock)
            {
                PushPullObject[] members = null;
                lock (MemberLock)
                {
                    members = Members.ToArray();
                }

                if (members.Length <= 0)
                    return;

                Dictionary<IAudioSource, short[]> InputSamples = new Dictionary<IAudioSource, short[]>();

                /// Convert our short data to int so we don't during addition
                // int[] combinedint = Utils.MakeIntArrayFromShortArray(sInitialData);
                int[] combinedint = new int[AudioFormat.CalculateNumberOfSamplesForDuration(tsElapsed)];


                ///Sum the input data from all our input sources, storing the data for each source so we can subtract it when sending
                foreach (PushPullObject nextobj in members)
                {
                    if (nextobj.AudioSource == null)
                        continue;

                    // Always pull data from a source even if it's not active, because some just queue their buffers
                    MediaSample sample = nextobj.AudioSource.PullSample(AudioFormat, tsElapsed);
                    if (sample == null)
                        continue;

                    if (nextobj.AudioSource.IsSourceActive == false)
                        continue;

                    short[] sData = sample.GetShortData();

                    /// Amplify our data if told to
                    if (nextobj.AudioSource.SourceAmplitudeMultiplier != 1.0f)
                    {
                        for (int i = 0; i < sData.Length; i++)
                        {
                            sData[i] = (short)(nextobj.AudioSource.SourceAmplitudeMultiplier * sData[i]);
                        }
                    }

                    InputSamples.Add(nextobj.AudioSource, sData);

                    Utils.SumArrays(combinedint, sData);
                }

                /// Push data to all our output filters, subtracting the data this member supplied
                foreach (PushPullObject nextobj in members)
                {
                    if (nextobj.AudioSink == null)
                        continue;

                    if (nextobj.AudioSink.IsSinkActive == false)
                        continue;

                    /// copy the summed data so we don't mangle it for the next client
                    int[] nCopy = new int[combinedint.Length];
                    Array.Copy(combinedint, nCopy, nCopy.Length);

                    foreach (IAudioSource excludesource in nextobj.SourceExcludeList)
                    {
                        if (InputSamples.ContainsKey(excludesource) == true)  // If we are in the dictionary, we are not muted, so no need to subtract
                        {
                            short[] sData = InputSamples[excludesource];
                            Utils.SubtractArray(nCopy, sData);
                        }
                    }


                    /// Amplify our data if told to
                    if (nextobj.AudioSink.SinkAmplitudeMultiplier != 1.0f)
                    {
                        for (int i = 0; i < nCopy.Length; i++)
                        {
                            nCopy[i] = (int)(nextobj.AudioSink.SinkAmplitudeMultiplier * nCopy[i]);
                        }
                    }


                    //short[] sOutput = Utils.MakeShortArrayFromIntArray(nCopy);
                    short[] sOutput = Utils.AGCAndShortArray(nCopy, short.MaxValue);
                    

                    MediaSample outputsample = new MediaSample(sOutput, AudioFormat);
                    nextobj.AudioSink.PushSample(outputsample, this);
                }
            }

        }



    }

}
