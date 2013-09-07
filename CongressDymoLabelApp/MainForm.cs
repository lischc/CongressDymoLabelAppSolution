using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Interop.Dymo;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace CongressDymoLabelApp
{
    public partial class MainForm : Form
    {
        private IDymoAddIn4 DymoAddIn;
        private IDymoLabels DymoLabels;
        public static IPAddress selectedAddress = null;
        private Worker worker;
        private Thread workerThread;

        public MainForm()
        {
            InitializeComponent();
            this.dataGridNames.CellClick += new DataGridViewCellEventHandler(dataGridNames_CellClick);
            this.dataGridNames.CellEndEdit += new DataGridViewCellEventHandler(dataGridNames_CellEndEdit);
            setupPrinter();
            selectNetworkInterface();
        }

        /// <summary>
        /// Registers the server on the congress webapp. Sends the local private ip address to the server.
        /// </summary>
        private void registerServer(bool isStartServer)
        {
            Debug.WriteLine("Selected address: " + MainForm.selectedAddress);
            // send information to congress webapp (Servlet must be in place)
            WebRequest request = WebRequest.Create("http://stafam-online.at/seminar-webapp/config?labelPrinterAddress=" + MainForm.selectedAddress);
            request.Method = "GET";
            request.Timeout = 1000; // 1 second timeout
            // Get the response.
            try
            {
                WebResponse response = request.GetResponse();
                // Display the status.
                Debug.WriteLine("StatusDescription: " + ((HttpWebResponse)response).StatusDescription);
                response.Close();
            }
            catch (WebException e)
            {
                Debug.WriteLine("Timeout. Could not send config parameter! " + e.Message);
            }

            if (isStartServer)
            {
                // finally start local server
                startServer();
            }
        }

        private void startServer()
        {
            // start in new thread
            this.worker = new Worker(this);
            this.workerThread = new Thread(worker.doWork);
            this.workerThread.Start();
            Debug.WriteLine("Started workerThread");
        }

        private void selectNetworkInterface()
        {
            this.Enabled = false;
            this.Hide();
            IPSelection ipSelection = new IPSelection();
            ipSelection.FormClosed += new FormClosedEventHandler(ipSelection_FormClosed);
            ipSelection.ShowDialog();
        }

        void ipSelection_FormClosed(object sender, FormClosedEventArgs e)
        {
            registerServer(true);
            this.Enabled = true;
            this.Show();
        }

        void dataGridNames_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                DataGridViewRow row = ((DataGridView)sender).Rows[e.RowIndex];
                row.Cells["colLastname"].Value = ((String)row.Cells["colLastname"].Value).ToUpper();
            }
        }

        private void setupPrinter()
        {
            // create DYMO COM objects
            DymoAddIn = new DymoAddIn();
            DymoLabels = new DymoLabels();

            bool isOpened = DymoAddIn.Open2(@"Kongress.LWL");
            System.Diagnostics.Debug.WriteLine("isOpened=" + isOpened);

            string PrtNames = DymoAddIn.GetDymoPrinters();
            System.Diagnostics.Debug.WriteLine(PrtNames);

            if (PrtNames != null)
            {
                // parse the result
                int i = PrtNames.IndexOf('|');
                while (i >= 0)
                {
                    String printer = PrtNames.Substring(0, i);
                    System.Diagnostics.Debug.WriteLine(printer);
                    this.toolStripStatusLabel.Text = "Ausgewählter Drucker: " + printer;
                    this.statusStrip.Refresh();
                    PrtNames = PrtNames.Remove(0, i + 1);
                    i = PrtNames.IndexOf('|');
                }
                if (PrtNames.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine(PrtNames);
                }
            }
            else
            {
                this.toolStripStatusLabel.Text = "Achtung: keinen Dymo Label Drucker gefunden!";
                this.statusStrip.Refresh();
            }
        }

        void dataGridNames_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.ColumnIndex);
            if (e.ColumnIndex == 4)
            {
                // Delete
                ((DataGridView)sender).Rows.RemoveAt(e.RowIndex);
            }
            else if (e.ColumnIndex == 5)
            {
                // Print
                DataGridViewRow row = ((DataGridView)sender).Rows[e.RowIndex];
                print(row);
            }
        }

        private void print(DataGridViewRow row)
        {
            String firstname = (String) ((DataGridViewCell)row.Cells["colFirstname"]).Value;
            String lastname = (String)((DataGridViewCell)row.Cells["colLastname"]).Value;
            String lectures = (String)((DataGridViewCell)row.Cells["colLectures"]).Value;
            String seminars = (String)((DataGridViewCell)row.Cells["colSeminars"]).Value;
            System.Diagnostics.Debug.WriteLine(firstname + " " + lastname + " | " + lectures + " | " + seminars);
            
            DymoLabels.SetField("VORNAME", firstname);
            DymoLabels.SetField("NACHNAME", lastname);
            DymoLabels.SetField("VORTRAEGE", lectures);
            DymoLabels.SetField("SEMINARE", seminars);

            // ATTENTION: This call is very important if you're making mutiple calls to the Print() or Print2() function!
            // It's a good idea to always wrap StartPrintJob() and EndPrintJob() around a call to Print() or Print2() function.
            DymoAddIn.StartPrintJob();

            // print two copies
            DymoAddIn.Print(1, false);
            
            // ATTENTION: Every StartPrintJob() must have a matching EndPrintJob()
            DymoAddIn.EndPrintJob();
        }

        void MainForm_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            // clean up DYMO COM objects
            DymoAddIn = null;
            DymoLabels = null;

            // send close message to server!
            try
            {
                TcpClient tcpClient = new TcpClient(MainForm.selectedAddress.ToString(), 9500);
                NetworkStream stream = tcpClient.GetStream();
                byte[] bytesSent = Encoding.UTF8.GetBytes("STOP");
                stream.Write(bytesSent, 0, bytesSent.Length);
                stream.Close();
                tcpClient.Close();
                this.workerThread.Join();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not send STOP to local server: " + ex.Message);
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            String firstname = this.txtFirstname.Text;
            String lastname = this.txtLastname.Text.ToUpper();
            String lectures = this.txtLectures.Text;
            String seminars = this.txtSeminars.Text;
            addAndPrint(firstname, lastname, lectures, seminars);

            clear();
        }

        delegate void SetTextCallback(String firstname, String lastname, String lectures, String seminars);

        public void addAndPrint(String firstname, String lastname, String lectures, String seminars)
        {
            if (firstname.Equals("") && lastname.Equals(""))
            {
                return;
            }

            if (this.dataGridNames.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(addAndPrint);
                this.Invoke(d, new object[] { firstname, lastname, lectures, seminars });
            }
            else
            {
                this.dataGridNames.Rows.Insert(0, new object[] { firstname, lastname, lectures, seminars });
                print(this.dataGridNames.Rows[0]);
            }
        }

        private void clear()
        {
            this.txtFirstname.Clear();
            this.txtLastname.Clear();
            this.txtLectures.Clear();
            this.txtSeminars.Clear();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            registerServer(false);
        }
    }
}
