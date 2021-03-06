﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.IO;



namespace MessageClientSide
{
    public partial class mainForm : Form
    {
        private UdpClient udpReceiver;
        private UdpClient udpSender;

        private int receivePort = 11001;
        private const int sendPort = 11002;
        private const int serverListenPort = 11000;
        private string localIPString ;
        private const string serverIPString = "10.108.96.44";  //192.168.1.212   10.108.96.44 
        private IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse(serverIPString), serverListenPort);
        private event Action<DataTable> bindDB;
        public mainForm()
        {
            localIPString = Array.FindAll(Dns.GetHostEntry(Dns.GetHostName()).AddressList,
                             a => a.AddressFamily == AddressFamily.InterNetwork)[0].ToString();
            udpReceiver = new UdpClient(new IPEndPoint(IPAddress.Parse(localIPString),receivePort));
            udpSender = new UdpClient(new IPEndPoint(IPAddress.Parse(localIPString),sendPort));
            bindDB += binding;
            InitializeComponent();
        }
        private void mainForm_Load(object sender, EventArgs e)
        {   
            byte[] bytes = Encoding.ASCII.GetBytes("init>");
            udpSender.Send(bytes, bytes.Length, serverAddress);
            udpReceiver.BeginReceive(new AsyncCallback(receiveCallBack), null);
        }
        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
 
            Application.Exit();
        }
        private void receiveCallBack(IAsyncResult obj)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpReceiver.EndReceive(obj, ref ep);
            string jsonstring = Encoding.ASCII.GetString(data, 0, data.Length);
            if (jsonstring == "HIMSending")
            {
                MessageBox.Show("The request has been processed by server....","Feedback");
            }
            else
            {
                try
                {
                    DataTable db = JsonConvert.DeserializeObject<DataTable>(jsonstring);
                    bindDB(db);
                }
                catch (Exception e)
                { MessageBox.Show(e.ToString(),"erro UDP package"); return; }
            }
            
            udpReceiver.BeginReceive(new AsyncCallback(receiveCallBack), null);          
        }

        private void sendBtt_Click(object sender, EventArgs e)
        {
            if (messageRichBox.Text == "")
            {
                MessageBox.Show("you cannot send empty message");
                return;
            }
            if (radioCheckBox.Checked && !mobileCheckBox.Checked)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    string udpPack = string.Format("radio>to:{0};msg:{1};", row.Cells[3].Value.ToString(), messageRichBox.Text);
                    byte[] bytes = Encoding.ASCII.GetBytes(udpPack);
                    udpSender.BeginSend(bytes, bytes.Length, serverAddress, new AsyncCallback(sendCallBack), null);
                }
            }
            else if (!radioCheckBox.Checked && mobileCheckBox.Checked)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    string udpPack = string.Format("mobile>to:{0};msg:{1};", row.Cells[2].Value.ToString(), messageRichBox.Text);
                    byte[] bytes = Encoding.ASCII.GetBytes(udpPack);
                    //udpSender.Send(bytes, bytes.Length, serverAddress);
                    udpSender.BeginSend(bytes, bytes.Length, serverAddress, new AsyncCallback(sendCallBack), null);
                    Thread.Sleep(5000);
                }
            }
            else
            {
                MessageBox.Show("you have to select one-only method of sending meassage");
                return;
            }

        }
        private void hibBtt_Click(object sender, EventArgs e)
        {
            if (messageRichBox.Text == "")
            {
                MessageBox.Show("you cannot send empty message");
                return;
            }
            if (MessageBox.Show("Are you sure you want to broadcast to all HIM group ?", "Checking", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string udpPack = string.Format("him>to:;msg:{0};", messageRichBox.Text);
                byte[] bytes = Encoding.ASCII.GetBytes(udpPack);
                udpSender.Send(bytes, bytes.Length, serverAddress);
            }
            else return;

        }
        private void sendCallBack(IAsyncResult iar)
        {
            if (udpSender.EndSend(iar) > 0)
                MessageBox.Show("Message(s) have been sent","Information");
        }

        private void binding(DataTable db)
        {
            if (this.dataGridView.InvokeRequired)
            {
                Action<DataTable> b = new Action<DataTable>(binding);
                this.Invoke(b, new object[] { db });
            }
            else
            { this.dataGridView.DataSource = db; }
        }

        private void refreshBtt_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("init>");
            udpSender.Send(bytes, bytes.Length, serverAddress);
        }
    }

}
