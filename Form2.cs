using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        private TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        private Thread listenThread;
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            StartServer();
        }
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }
        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8888);
            listenThread = new Thread(new ThreadStart(ListenForClients));
            listenThread.Start();
            AddMessageToListBox("Server started...");
        }
        private void StopServer()
        {
            if (listener != null)
            {
                listener.Stop();
                AddMessageToListBox("Server stopped.");
            }
        }
        private void ListenForClients()
        {
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
            }
        }
        private void HandleClient(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                string data = Encoding.UTF8.GetString(message, 0, bytesRead);
                AddMessageToListBox(data);
            }

            clients.Remove(tcpClient);
            tcpClient.Close();
        }
        private void AddMessageToListBox(string message)
        {
            if (listBoxMessages.InvokeRequired)
            {
                listBoxMessages.Invoke(new MethodInvoker(() => AddMessageToListBox(message)));
            }
            else
            {
                listBoxMessages.Items.Add(message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
          StopServer();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ChatClientForm form = new ChatClientForm();
            form.Show();
        }
    }
}
