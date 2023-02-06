using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Status;

namespace client
{
    public partial class Form1 : Form
    {

        bool terminating = false;
        bool connected = false;
        Socket clientSocket;
        private IFormatProvider port;
        private IFormatProvider number;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

            InitializeComponent();

            button_send.Enabled = false;

            textBox_ip.Text = "";
            textBox_port.Text = "";
            textBox_name.Text = "";
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_ip.Text;

            int portNum;
            if (Int32.TryParse(textBox_port.Text, out portNum))
            {
                try
                {
                    logs.Text = "";
                    clientSocket.Connect(IP, portNum);
                    button_connect.Enabled = false;
                    textBox_message.Enabled = true;
                    connected = true;
                    logs.AppendText("Connected to the server!\n");
                    string name = textBox_name.Text;
                    if (name != "" && name.Length <= 64)
                    {
                        Thread.Sleep(100);
                        var helloMessage = "Name," + name;
                        Byte[] buffer = Encoding.Default.GetBytes(helloMessage);
                        clientSocket.Send(buffer);
                    }

                    Thread receiveThread = new Thread(Receive);
                    receiveThread.Start();

                }
                catch
                {
                    logs.AppendText("Could not connect to the server!\n");
                }
            }
            else
            {
                logs.AppendText("Check the port\n");
            }

        }

        private void Receive()
        {
            while (IsSocketConnected(clientSocket) && connected)
            {
                try
                {
                    if (clientSocket.Available > 0)
                    {
                        Byte[] buffer = new Byte[1000]; // serverdan alma fonksiyonu bu 64 bitle kısıtlı
                        clientSocket.Receive(buffer);

                        string incomingMessage = Encoding.Default.GetString(buffer);
                        incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                        logs.AppendText("Server: " + incomingMessage + "\n");

                    }
                }
                catch
                {
                    if (!terminating)
                    {
                        ResetClient();
                        logs.AppendText("The server has disconnected\n");
                    }
                }

            }
            logs.AppendText("Server disconnected");
            ResetClient();


        }


        private bool IsSocketConnected(Socket socket)
        {
            try
            {
                if (!socket.Connected)
                    return false;

                bool part1 = socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (socket.Available == 0);
                return !(part1 && part2);
            }
            catch
            {
                return false;
            }
        }


        private void ResetClient()
        {
            textBox_ip.Text = "";
            textBox_port.Text = "";
            textBox_name.Text = "";
            button_send.Enabled = false;
            button_connect.Enabled = true;
            textBox_message.Enabled = false;
            textBox_message.Text = "";
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            if (clientSocket != null)
                clientSocket.Close();
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            string answer = textBox_message.Text;

            if (answer != "" && answer.Length <= 64)
            {
                var message = "Answer," + answer;
                Byte[] buffer = Encoding.UTF8.GetBytes(message);
                clientSocket.Send(buffer);
            }

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void textBox_name_TextChanged(object sender, EventArgs e)
        {
            HandleConnectButtonOnInputChanged();
        }

        private void textBox_message_TextChanged(object sender, EventArgs e)
        {
            int number;
            var isInputNumber = int.TryParse(textBox_message.Text, out number);
            if (textBox_message.Text == String.Empty || !isInputNumber)
            {
                button_send.Enabled = false;
            }
            else
            {
                button_send.Enabled = true;
            }
        }

        private void textBox_ip_TextChanged(object sender, EventArgs e)
        {

            HandleConnectButtonOnInputChanged();
        }

        private void textBox_port_TextChanged(object sender, EventArgs e)
        {
            HandleConnectButtonOnInputChanged();
        }

        private void HandleConnectButtonOnInputChanged()
        {
            int port;
            var isIpOk = textBox_ip.Text != String.Empty;
            var isPortOk = textBox_port.Text != String.Empty && int.TryParse(textBox_port.Text, out port);
            var isNameOk = textBox_name.Text != String.Empty;
            if (isIpOk && isPortOk && isNameOk)
            {
                button_connect.Enabled = true;
            }
            else
            {
                button_connect.Enabled = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void logs_TextChanged(object sender, EventArgs e)
        {
            logs.SelectionStart = logs.Text.Length;
            logs.ScrollToCaret();
        }
    }
}
