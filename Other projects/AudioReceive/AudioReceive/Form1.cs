using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Net;
using System.Net.Sockets;
namespace AudioReceive
{
    public partial class Form1 : Form
    {

        static int ct = 0;
        List<byte[]> audio = new List<byte[]>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            UdpClient newsock = new UdpClient(4001);
            IPEndPoint send = new IPEndPoint(IPAddress.Any, 0);
            int ct = 0;
            while (ct < 200000)
            {
                byte[] data1 = newsock.Receive(ref send);
                audio.Add(data1);
                ct += data1.Length;
            }
            textBox1.Text += "Received";
        }



        private void button2_Click(object sender, EventArgs e)
        {
            byte[] toplay = Combine(audio);
            SoundEffect s = new SoundEffect(toplay, Microphone.Default.SampleRate, AudioChannels.Mono);
            SoundEffectInstance sm = s.CreateInstance();
            sm.Play();
            
        }
        private byte[] Combine(List<  byte[] > arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            UdpClient x=new UdpClient("172.16.41.174",3001);
            foreach (byte[] temp in audio)
            {
                if (ct < 30)
                {
                    foreach (byte b in temp)
                    { textBox1.Text += b + ' '; ct++; }
                }
                x.Send(temp, temp.Length);
            }
            textBox1.Text += "sent";

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

    }
}
