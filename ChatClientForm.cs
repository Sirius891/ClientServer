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
        private bool isConnected = false;
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
            button5.Enabled = false;

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
                isConnected = true;  // Установка флага подключения

                // Отправка имени пользователя на сервер сразу после подключения
                string userName = txtUserName.Text;
                byte[] data = Encoding.UTF8.GetBytes(userName);
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server: " + ex.Message);
            }
        }

        private void SendMessage()
        {
            if (!client.Connected)
            {
                MessageBox.Show("Not connected to a server.");
                return;
            }
            try
            {
                // Получаем текущее время
                string timestamp = DateTime.Now.ToString("HH:mm");
                string message = $"{txtUserName.Text} (Время отправления:{timestamp}): {txtMessage.Text}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                stream.Flush();
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

            try
            {
                while (client.Connected)
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        throw new Exception("Disconnected from server"); // Инициируем исключение при потере соединения
                    }
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AddMessageToListBox(message);
                }
            }
            catch
            {
                if (isConnected) // Проверяем, не было ли уже обработано отключение
                {
                    isConnected = false; // Устанавливаем флаг в false
                }
            }
        }

        private void Disconnect()
        {
            if (client != null && client.Connected)
            {
                try
                {
                    // Посылаем серверу сообщение о выходе из чата
                    string message = $"{txtUserName.Text} покинул чат.";
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                    stream.Flush();

                    stream.Close(); // Закрываем поток
                    client.Close(); // Закрываем соединение
                    isConnected = false; // Устанавливаем флаг в false

                    AddMessageToListBox("Вы вышли из данного чата"); // Добавляем сообщение в лог на стороне клиента
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while disconnecting: " + ex.Message);
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
            txtMessage.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1 a = new Form1();
            a.Show();
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Disconnect();
            button5.Enabled = true;
        }
       


    }
}
