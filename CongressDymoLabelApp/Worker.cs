using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace CongressDymoLabelApp
{
    public class Worker
    {
        private volatile bool _shouldStop;
        volatile static string output = "";
        private volatile MainForm mainForm;

        public Worker(MainForm mainForm)
        {
            this.mainForm = mainForm;
        }
        
        public void doWork()
        {
            TcpListener tcpListener = null;
            try
            {
                // Set the listener on the local IP address 
                // and specify the port.
                tcpListener = new TcpListener(MainForm.selectedAddress, 9500);
                tcpListener.Start();
                output = "Waiting for a connection...";
            }
            catch (Exception e)
            {
                output = "Error: " + e.ToString();
                MessageBox.Show(output);
            }
            while (!this._shouldStop)
            {
                // Always use a Sleep call in a while(true) loop 
                // to avoid locking up your CPU.
                Thread.Sleep(10);
                // Create a TCP socket. 
                // If you ran this server on the desktop, you could use 
                // Socket socket = tcpListener.AcceptSocket() 
                // for greater flexibility.
                Debug.WriteLine("Waiting for connection...");
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                Debug.WriteLine("Connected");
                // Read the data stream from the client. 
                byte[] bytes = new byte[256];
                NetworkStream stream = tcpClient.GetStream();
                stream.Read(bytes, 0, bytes.Length);
                SocketHelper helper = new SocketHelper();
                this._shouldStop = helper.processMsg(tcpClient, stream, bytes, this.mainForm);
                tcpClient.Close();
            }
        }
    }
}
