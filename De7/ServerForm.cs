using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GroupChatApp
{
    public partial class ServerForm : Form
    {
        private TcpListener tcpListener;
        private List<ClientInfo> clients = new List<ClientInfo>();
        private bool isServerRunning = false;

        public ServerForm()
        {
            InitializeComponent();
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            // Initialize your server UI or any other setup here
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }

            rtbLog.AppendText(message + Environment.NewLine);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartServer();
        }

        private void StartServer()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 8888);
                tcpListener.Start();
                isServerRunning = true;
                Log("Server started.");

                // Start accepting client connections asynchronously
                ThreadPool.QueueUserWorkItem(new WaitCallback(ListenForClients));
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting server: " + ex.Message);
            }
        }

        private void ListenForClients(object obj)
        {
            while (isServerRunning)
            {
                try
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    ClientInfo clientInfo = new ClientInfo(tcpClient);
                    clients.Add(clientInfo);

                    // Start handling client communication
                    ThreadPool.QueueUserWorkItem(new WaitCallback(HandleClientComm), clientInfo);

                    // Log client connection
                    Log($"{clientInfo.Name} connected.");
                }
                catch (SocketException)
                {
                    break; // Socket is closed while waiting for a connection
                }
                catch (Exception ex)
                {
                    Log("Error accepting client: " + ex.Message);
                }
            }
        }

        private void HandleClientComm(object clientInfoObj)
        {
            ClientInfo clientInfo = (ClientInfo)clientInfoObj;
            TcpClient client = clientInfo.TcpClient;
            NetworkStream clientStream = client.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            // Read client name
            bytesRead = clientStream.Read(message, 0, 4096);
            clientInfo.SetName(Encoding.Unicode.GetString(message, 0, bytesRead));

            Log($"{clientInfo.Name} connected.");

            while (isServerRunning)
            {
                bytesRead = 0;

                try
                {
                    // Read client message
                    bytesRead = clientStream.Read(message, 0, 4096);

                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }

                    // Process the message (e.g., broadcast to other clients)
                    string incomingMessage = $"{Encoding.Unicode.GetString(message, 0, bytesRead)}";
                    Log(incomingMessage);

                    // Broadcast message to all clients
                    BroadcastMessage(incomingMessage, client);
                }
                catch (Exception ex)
                {
                    Log("Error handling client message: " + ex.Message);
                    break;
                }
            }

            // Clean up client connection
            clients.Remove(clientInfo);
            clientInfo.Dispose();
            client.Close();
        }


        public void BroadcastMessage(string message, TcpClient excludeClient = null)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(message);

            foreach (ClientInfo clientInfo in clients)
            {
                if (clientInfo.TcpClient != excludeClient)
                {
                    NetworkStream clientStream = clientInfo.TcpClient.GetStream();
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                }
            }
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void StopServer()
        {
            isServerRunning = false;

            // Stop accepting new clients
            tcpListener.Stop();

            // Close all client connections
            foreach (ClientInfo clientInfo in clients)
            {
                clientInfo.TcpClient.Close();
                clientInfo.Dispose();
            }
            clients.Clear();

            Log("Server stopped.");
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isServerRunning)
            {
                StopServer();
            }
        }

        private class ClientInfo : IDisposable
        {
            public TcpClient TcpClient { get; private set; }
            public string Name { get; private set; }

            public ClientInfo(TcpClient tcpClient)
            {
                TcpClient = tcpClient;
                Name = ""; // Default name
            }

            public void SetName(string name)
            {
                Name = name;
            }

            public void Dispose()
            {
                TcpClient = null;
            }
        }
    }
}
