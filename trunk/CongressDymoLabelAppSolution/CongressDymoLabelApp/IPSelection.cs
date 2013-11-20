using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;

namespace CongressDymoLabelApp
{
    public partial class IPSelection : Form
    {
        public IPSelection()
        {
            InitializeComponent();
            initComboBox();
        }

        private void initComboBox()
        {
            IPAddress[] localPrivateAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress adr in localPrivateAddresses)
            {
                if (adr.IsIPv6LinkLocal || adr.IsIPv6Multicast || adr.IsIPv6SiteLocal || adr.IsIPv6Teredo)
                {
                    continue;
                }
                Debug.WriteLine(adr);
                this.cmbIPAddresses.Items.Add(adr);
            }
            this.cmbIPAddresses.SelectedIndex = 0;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            MainForm.selectedAddress = (IPAddress)this.cmbIPAddresses.Items[this.cmbIPAddresses.SelectedIndex];
            Debug.WriteLine("selectedAddress in click: " + MainForm.selectedAddress);
            this.Close();
        }
    }
}
