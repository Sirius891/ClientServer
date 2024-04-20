using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class ChatClientForm : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;

        public ChatClientForm()
        {
            InitializeComponent();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ConnectToServer();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient(txtServerIP.Text, int.Parse(txtServerPort.Text));
                stream = client.GetStream();
                receiveThread = new Thread(new ThreadStart(ReceiveMessages));
                receiveThread.Start();
                AddMessageToListBox("Connected to server...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server: " + ex.Message);
            }
        }
        private void SendMessage()
        {
            try
            {
                string message = txtUserName.Text + ": " + txtMessage.Text;
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                stream.Flush();
                AddMessageToListBox(message);
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending message: " + ex.Message);
            }
        }
        private void ReceiveMessages()
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AddMessageToListBox(message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error receiving message: " + ex.Message);
                    break;
                }
            }
        }
        private void AddMessageToListBox(string message)
        {
            if (lstChatMessages.InvokeRequired)
            {
                lstChatMessages.Invoke(new MethodInvoker(() => AddMessageToListBox(message)));
            }
            else
            {
                lstChatMessages.Items.Add(message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SendMessage();
        }
    }
}
