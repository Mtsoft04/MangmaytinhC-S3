using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GroupChatApp
{
    public partial class Client2Form : Form
    {
        private TcpClient client;
        private NetworkStream clientStream;
        private bool isConnected = false;
        private string clientName;

        public Client2Form()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() == "")
            {
                MessageBox.Show("Please enter your name to connect.");
                return;
            }

            clientName = textBox1.Text.Trim();
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                client.Connect("127.0.0.1", 8888); // Replace with your server IP and port

                clientStream = client.GetStream();
                isConnected = true;

                // Send client name to server
                byte[] nameBuffer = Encoding.Unicode.GetBytes(clientName);
                clientStream.Write(nameBuffer, 0, nameBuffer.Length);

                Log("Connected to server.");

                // Start listening for incoming messages in a separate thread
                ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveMessages));
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                btnSend.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server: " + ex.Message);
            }
        }

        private void ReceiveMessages(object obj)
        {
            byte[] message = new byte[4096];
            int bytesRead;

            while (isConnected)
            {
                bytesRead = 0;

                try
                {
                    // Read server message
                    bytesRead = clientStream.Read(message, 0, 4096);

                    if (bytesRead == 0)
                    {
                        break; // Server disconnected
                    }

                    // Process the message (e.g., display in chat box)
                    string incomingMessage = Encoding.Unicode.GetString(message, 0, bytesRead);
                    Log("Received: " + incomingMessage);
                }
                catch (Exception ex)
                {
                    Log("Error receiving server message: " + ex.Message);
                    break;
                }
            }

            // Clean up client connection
            DisconnectFromServer();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisconnectFromServer();
        }

        private void DisconnectFromServer()
        {
            isConnected = false;

            // Close client connection
            clientStream.Close();
            client.Close();

            Log("Disconnected from server.");
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnSend.Enabled = false;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            try
            {
                string message = $"{clientName}: {tbMessage.Text}";
                byte[] buffer = Encoding.Unicode.GetBytes(message);

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();

                Log("Sent: " + message);

                tbMessage.Clear();
            }
            catch (Exception ex)
            {
                Log("Error sending message: " + ex.Message);
            }
        }


        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }

            rtbChat.AppendText(message + Environment.NewLine);
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isConnected)
            {
                DisconnectFromServer();
            }
        }
    }
}
