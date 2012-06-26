using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace SocketCoder_VoiceChat
{
    public partial class VoiceRoom : Form
    {

        private Socket ClientSocket;
        private DirectSoundHelper sound;
        private byte[] buffer = new byte[2205];
        private Thread th;

        public VoiceRoom()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();           
        }

        void Connect(string ServerIP, int Port)
        {
            try
            {
                if (ClientSocket != null && ClientSocket.Connected)
                {
                    ClientSocket.Shutdown(SocketShutdown.Both);
                    System.Threading.Thread.Sleep(10);
                    ClientSocket.Close();
                }

                ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint Server_EndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), Port);
                ClientSocket.Blocking = false;

                ClientSocket.BeginConnect(Server_EndPoint, new AsyncCallback(OnConnect), ClientSocket);

                th = new Thread(new ThreadStart(sound.StartCapturing));
                th.IsBackground = true;
                th.Start();               
            }
            catch (Exception) { }
        }

        public void OnConnect(IAsyncResult ar)
        {
            Socket sock = (Socket)ar.AsyncState;

            try
            {
                if (sock.Connected)
                {
                    SetupRecieveCallback(sock);
                    JoinBTN.Enabled = false;
                    StartTalkingBTN.Enabled = true;
                }
                else
                {
                    Disconncet();
                    JoinBTN.Enabled = true;
                    StartTalkingBTN.Enabled = false;
                    MessageBox.Show("Connection Failed");
                }
            }
            catch (Exception) { }
        }

        void SendBuffer(byte[] buffer)
        {
            ClientSocket.Send(buffer, SocketFlags.None);
        }

        public void OnRecievedData(IAsyncResult ar)
        {
            Socket sock = (Socket)ar.AsyncState;

            try
            {
                int nBytesRec = sock.EndReceive(ar);
                if (nBytesRec > 0)
                {
                    sound.PlayReceivedVoice(buffer);

                    SetupRecieveCallback(sock);
                }
                else
                {
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                }
            }
            catch (Exception) { }
        }

        public void SetupRecieveCallback(Socket sock)
        {
            try
            {
                AsyncCallback recieveData = new AsyncCallback(OnRecievedData);
                sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, recieveData, sock);
            }
            catch (Exception) { }
        }

        void Disconncet()
        {
            try
            {
                if (ClientSocket != null & ClientSocket.Connected)
                {
                    ClientSocket.Close();
                }
            }
            catch (Exception) { }
        }

        private void JoinBTN_Click(object sender, EventArgs e)
        {
            Connect(ServerIPTXT.Text, 5000);
        }


        private void VoiceRoom_Load(object sender, EventArgs e)
        {
            sound = new DirectSoundHelper();
            sound.OnBufferFulfill += new EventHandler(SendVoiceBuffer);
        }

        void SendVoiceBuffer(object VoiceBuffer, EventArgs e)
        {
            byte[] Buffer = (byte[]) VoiceBuffer;

            SendBuffer(Buffer);
            
        }

        private void StartTalkingBTN_Click(object sender, EventArgs e)
        {
            sound.StopLoop = false;

            StartTalkingBTN.Enabled = false;
            StopTalkingBTN.Enabled = true;
        }

        private void StopTalkingBTN_Click(object sender, EventArgs e)
        {
            sound.StopLoop = true;

            StartTalkingBTN.Enabled = true;
            StopTalkingBTN.Enabled = false;
        }

        private void VoiceRoom_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconncet();
        }
    }
}
