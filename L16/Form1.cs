using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Drawing;

namespace L16
{
    public partial class Form1 : Form
    {
        bool alive = false;
        UdpClient client;
        const int LOCALPORT = 8001;
        const int REMOTEPORT = 8001;
        const int TTL = 20;
        const string HOST = "239.0.0.1";
        IPAddress groupAddress;
        Font chatFont;
        string userName;
        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
            chatTextBox.ReadOnly = true;

            chatFont = chatTextBox.Font;


            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(HOST);
                groupAddress = addresses.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка отримання IP-адреси: " + ex.Message);
                return;
            }

            if (groupAddress == null)
            {
                MessageBox.Show("Не вдалося отримати IP-адресу для доменного імені: " + HOST);
                return;
            }
        }

        private void SetupChatSettings()
        {
            FontDialog fontDialog = new FontDialog();
            fontDialog.Font = chatFont;

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                chatFont = fontDialog.Font;
                chatTextBox.Font = chatFont;
            }
        }
        private void settingsButton_Click(object sender, EventArgs e)
        {
            SetupChatSettings();
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            userName = userNameTextBox.Text;
            userNameTextBox.ReadOnly = true;
            try
            {
                client = new UdpClient(LOCALPORT);
                client.JoinMulticastGroup(groupAddress, TTL);
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();
                string message = userName + " вошел в чат";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                loginButton.Enabled = false;
                logoutButton.Enabled = true;
                sendButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при вході в чат: " + ex.Message);
            }
        }
        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        chatTextBox.Text = time + " " + message + "\r\n"
                        + chatTextBox.Text;
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void sendButton_Click(object sender, EventArgs e)
        {
            try
            {
                string message = String.Format("{0}: {1}", userName,
               messageTextBox.Text);
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }
        private void ExitChat()
        {
            string message = userName + " покидает чат";
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            client.DropMulticastGroup(groupAddress);
            alive = false;
            client.Close();
            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
        }
        private StringBuilder chatLog = new StringBuilder();
        private void SaveChatLogToFile()
        {
            try
            {
                using (StreamWriter sw = File.AppendText("chat_log.txt"))
                {
                    sw.Write(chatLog.ToString());
                }

                MessageBox.Show("Лог чату було успішно збережено у файлі chat_log.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при збереженні логу чату: " + ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (alive)
                ExitChat();

            SaveChatLogToFile();
        }
    }
}