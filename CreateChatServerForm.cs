using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Linq;



namespace WindowsFormsApp1
{
    public partial class CreateChatServerForm : Form
    {
        private int connectedClientsCount = 0;
        private TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>();
        private Thread listenThread;
        private string localport;
        private bool isListening = true;
        private Dictionary<string, TcpClient> clientMap = new Dictionary<string, TcpClient>();
        private List<string> messageHistory = new List<string>();
        public CreateChatServerForm()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            // Получаем все доступные IP-адреса и добавляем их в ComboBox
            List<string> availableIPs = GetLocalIPAddresses();
            comboBox1.Items.AddRange(availableIPs.ToArray());
            comboBox1.SelectedIndex = 2;
        }

        private List<string> GetLocalIPAddresses()
        {
            List<string> ipAddresses = new List<string>();

            // Получаем информацию обо всех сетевых интерфейсах
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in networkInterfaces)
            {
                // Получаем IPv4-адреса для выбранного адаптера
                UnicastIPAddressInformationCollection ipProps = adapter.GetIPProperties().UnicastAddresses;

                foreach (UnicastIPAddressInformation ipAddress in ipProps)
                {
                    // Проверяем, что адрес IPv4 и не является петлевым адресом
                    if (ipAddress.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ipAddress.Address))
                    {
                        ipAddresses.Add(ipAddress.Address.ToString());
                    }
                }
            }

            return ipAddresses;
        }
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }
        private void StartServer()
        {

            string selectedIP = comboBox1.SelectedItem.ToString();
            listener = new TcpListener(IPAddress.Parse(selectedIP), int.Parse(localport));
            listenThread = new Thread(new ThreadStart(ListenForClients));
            listenThread.Start();
            AddMessageToListBox("Server started...");
            

        }
        private void StopServer()
        {
            if (listener != null)
            {
                isListening = false;

                // Остановка всех клиентских соединений
                foreach (var client in clients)
                {
                    if (client.Connected)
                    {
                        client.GetStream().Close();
                        client.Close();
                    }
                }
                clients.Clear();  // Очистка списка клиентов

                // Остановка сервера
                listener.Stop();
                AddMessageToListBox("Server stopped.");
            }
        }
        private void ListenForClients()
        {
            listener.Start();

            while (isListening)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    clients.Add(client);

                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start(client);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    // Обработка прерывания ожидания подключений
                    break;
                }
            }
        }
        private void AddUserConnectedMessage(string username)
        {
            string message = $"{username} подключился к чату.";
            AddMessageToListBox(message);
        }
        private void HandleClient(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            if (checkBox1.Checked)
            {
                foreach (string message in messageHistory)
                {
                    string formattedMessage = message + "\r\n"; // Добавляем перенос строки для клирности, необязательно
                    byte[] messageBuffer = Encoding.UTF8.GetBytes(formattedMessage);
                    clientStream.Write(messageBuffer, 0, messageBuffer.Length);
                    clientStream.Flush();
                }
            }

            byte[] usernameBuffer = new byte[4096];
            int bytesRead = clientStream.Read(usernameBuffer, 0, 4096);
            string username = Encoding.UTF8.GetString(usernameBuffer, 0, bytesRead).Trim();

            // Проверка на уникальность имени пользователя
            if (clientMap.ContainsKey(username))
            {
                // Отправка сообщения пользователю о том, что имя уже занято
                string message = "Данное имя занято, попробуйте другое имя и подключитесь снова.";
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();

                // Закрытие соединения, так как имя пользователя уже занято
                tcpClient.Close();
            }
            else
            {
                // Добавление пользователя в словарь
                clientMap.Add(username, tcpClient);
                AddUserConnectedMessage(username);

                // Увеличение счетчика подключенных клиентов
                Interlocked.Increment(ref connectedClientsCount);

                byte[] messageBuffer = new byte[4096];
                bytesRead = 0;

                try
                {
                    while (true)
                    {
                        bytesRead = clientStream.Read(messageBuffer, 0, 4096);
                        if (bytesRead == 0)
                        {
                            // Клиент отключился
                            //BroadcastMessage($"{username} has left the chat.", null);  // Отправка сообщения всем клиентам
                            break;
                        }
                        string receivedMessage = Encoding.UTF8.GetString(messageBuffer, 0, bytesRead);
                        BroadcastMessage(receivedMessage, tcpClient);  // Отправка сообщения всем клиентам
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                // Уменьшение счетчика подключенных клиентов
                Interlocked.Decrement(ref connectedClientsCount);

                // Удаление клиента из списка активных клиентов и закрытие соединения
                clients.Remove(tcpClient);
                clientMap.Remove(username);
                tcpClient.Close();
            }
        }


        private void BroadcastMessage(string message, TcpClient originClient)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message +"\r\n");

            foreach (TcpClient client in clients)
            {
                if (client.Connected)  // Проверяем, подключен ли клиент
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(buffer, 0, buffer.Length);
                        stream.Flush();
                    }
                    catch (Exception ex)
                    {
                        // Обработка исключений, например, если клиент отключился
                        Console.WriteLine("Ошибка при отправке сообщения: " + ex.Message);
                    }
                }
            }

            // Также добавляем сообщение в ListBox сервера для отображения
            messageHistory.Add(message);
            AddMessageToListBox(message);
        }



        // Метод для обновления количества подключенных пользователей
        private void UpdateConnectedClientsCount()
        {
            groupBox1.Text = $"Подключено клиентов: {connectedClientsCount}";
        }
        // Метод для начала прослушивания клиентов
        private void StartListeningForClients()
        {
            listener.Start();
            AddMessageToListBox("Сервер запущен, ожидание подключений...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);

                // Создание нового потока для обработки клиента
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);

                // Обновление количества подключенных клиентов
                UpdateConnectedClientsCount();
            }
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
            try
            {
                StopServer();
                button1.Enabled = false;
                button2.Enabled = true;
                textBox2.Enabled = true;
            }
            catch
            {
                MessageBox.Show("Что то пошло не так");
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(comboBox1.Text))
            {
                MessageBox.Show("Пожалуйста выберите ip из доступных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Пожалуйста введите порт", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!IsValidPort(textBox2.Text))
            {
                MessageBox.Show("Неверный формат порта", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Прерываем выполнение метода, если формат некорректен
            }
            
            localport = textBox2.Text;
            try
            {
                StartServer();
                button2.Enabled = false;
                button1.Enabled = true;
                textBox2.Enabled = false;
            }
            catch
            {
                MessageBox.Show("Что то пошло не так");
            }
        }


        private bool IsValidPort(string port)
        {
            // Проверяем корректность формата порта с использованием регулярного выражения
            string pattern = @"^([1-9]\d{3,4}|[1-5]\d{4}|6[0-4]\d{3}|65[0-4]\d{2}|655[0-2]\d|6553[0-5])$";
            return Regex.IsMatch(port, pattern);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listener != null && listener.Server.IsBound)
            {
                // Показываем MessageBox с вопросом и кнопками "Да" и "Нет"
                DialogResult result = MessageBox.Show("Вы уверены, что хотите вернуться назад, стоит предупредить что это приведет к закрытию сервера", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                // Проверяем, какую кнопку нажал пользователь
                if (result == DialogResult.Yes)
                {
                    StopServer();
                    Form1 a = new Form1();
                    a.Show();
                    this.Close();
                }
                else
                {

                }
            }
            else
            {
                Form1 a = new Form1();
                a.Show();
                this.Close();
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            ChatClientForm a = new ChatClientForm();
            a.Show();
           
        }
    }
}
