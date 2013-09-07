using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Web;

namespace CongressDymoLabelApp
{
    class SocketHelper
    {
        TcpClient mscClient;
        string mstrMessage;
        // string mstrResponse;
        // byte[] bytesSent;
        public bool processMsg(TcpClient client, NetworkStream stream, byte[] bytesReceived, MainForm mainForm)
        {
            // Handle the message received and  
            // send a response back to the client.
            mstrMessage = Encoding.UTF8.GetString(bytesReceived, 0, bytesReceived.Length);
            Debug.WriteLine("mstrMessage: " + mstrMessage);
            mscClient = client;
            String control = mstrMessage.Substring(0, 4);
            if (control.Equals("STOP"))
            {
                return true;
            }
            else
            {
                String firstline = mstrMessage.Replace("GET /?", "");
                firstline = Uri.UnescapeDataString(firstline);
                String[] parts = firstline.Split(new String[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                String firstname = "";
                String lastname = "";
                String lectures = "";
                String seminars = "";
                foreach (String part in parts)
                {
                    Debug.WriteLine("parts=" + parts);
                    String[] kv = part.Split(new String[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    if ("firstname".Equals(kv[0]))
                    {
                        firstname = kv.Length > 1 ? kv[1] : "";
                    } 
                    else if ("lastname".Equals(kv[0]))
                    {
                        lastname = kv.Length > 1 ? kv[1] : "";
                    } 
                    else if ("lectures".Equals(kv[0]))
                    {
                        lectures = kv.Length > 1 ? kv[1] : "";
                    }
                    else if ("seminars".Equals(kv[0]))
                    {
                        seminars = kv.Length > 1 ? kv[1] : "";
                    }
                }
                mainForm.addAndPrint(firstname, lastname, lectures, seminars);
                return false;
            }
        }

        private void BuildReqStream(ref WebRequest webrequest, String content)
        //This method build the request stream for WebRequest
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            webrequest.ContentLength = bytes.Length;
            try
            {
                Stream oStreamOut = webrequest.GetRequestStream();
                oStreamOut.Write(bytes, 0, bytes.Length);
                oStreamOut.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
