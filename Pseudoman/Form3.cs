using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Pseudoman
{
    public partial class Form3 : Form
    {
        public bool isClient, forcedClosing = true, toEnd = false;
        public Thread servering;
        public TcpListener[] servers = new TcpListener[4];
        public TcpClient[] clients = new TcpClient[4];
        public NetworkStream[] streams = new NetworkStream[4];
        public BinaryWriter[] writers = new BinaryWriter[4];
        public BinaryReader[] readers = new BinaryReader[4];
        public int clientPort;
        string localIP, networkIP;

        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            isClient = false;
            StepForward();
            servering = new Thread(DoServer);

            try
            {
                localIP = LocalIPAddress().ToString();

                DomainUpDown.DomainUpDownItemCollection collection = domainUpDown1.Items;
                collection.Add("127.0.0.1");
                collection.Add(localIP);
            }
            catch (Exception)
            {

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            isClient = true;
            StepForward();
            servering = new Thread(DoClient);
        }

        private void DoServer()
        {

            TcpListener server = null;
            TcpClient client = null;
            NetworkStream stream = null;
            BinaryWriter writer = null;
            BinaryReader reader = null;
            bool stillon = true;
            byte what = 0, x = 200, y = 200;
            string[] strs = domainUpDown1.Text.Split('.');
            byte[] ipByte = new byte[strs.Length];
            int port = 1300;

            for (int i=0; i< strs.Length; i++)
            {
                ipByte[i] = byte.Parse(strs[i]);
            }

            try
            {
                server = new TcpListener(new IPAddress(ipByte), port);
                server.Start();

                this.Invoke((Action)delegate { textBox2.Text = "waiting for players to join..."; });

                for (int i = 0; i < 4; i++)
                {
                    while (!server.Pending())
                    {
                        if (toEnd) break;
                    }
                    if (server.Pending()) client = server.AcceptTcpClient();
                    else
                    {
                        for (int k = 0; k < servers.Length; k++)
                        {
                            if (streams[k] != null)
                            {
                                writers[k] = new BinaryWriter(streams[k]);
                                writers[k].Write(1);
                            }
                        };
                        break;
                    }
                    
                    stream = client.GetStream();
                    writer = new BinaryWriter(stream);
                    this.Invoke((Action)delegate { textBox2.Text = "Player "+ (i + 1) + " joining..."; });

                    servers[i] = new TcpListener(new IPAddress(ipByte), port + i + 1);
                    servers[i].Start();

                    writer.Write(i + 1);
                    
                    client.Close();

                    clients[i] = servers[i].AcceptTcpClient();

                    
                    streams[i] = clients[i].GetStream();
                   
                    writers[i] = new BinaryWriter(streams[i]);
                    readers[i] = new BinaryReader(streams[i]);

                    this.Invoke((Action)delegate { textBox2.Text = "Player " + (i + 1) + " successfully joined."; });
                }

                /*while (stillon)
                {
                    var recieved = reader.ReadString();
                    switch (recieved)
                    {
                        case "left": x = 118; y = 128; break;
                        case "right": x = 138; y = 128; break;
                        case "up": x = 128; y = 118; break;
                        case "down": x = 128; y = 138; break;
                        case "stop": stillon = false; break;
                        default: break;
                    }
                    what = (stillon) ? (byte)1 : (byte)2;
                    writer.Write(new byte[] { what, x, y });
                    Thread.Sleep(500);
                }

                this.Invoke((Action)delegate { textBox2.Text = "servering attempting to end..."; });*/


            }
            finally
            {
                if(client!=null) client.Close();
                server.Stop();
            }
        }

        private void DoClient()
        {
            
            TcpListener server = null;
            TcpClient client = null;
            NetworkStream stream = null;
            BinaryWriter writer = null;
            BinaryReader reader = null;
            byte[] arr = new byte[3];
            byte what, x, y;
            bool notsent = true;

            try
            {
                client = new TcpClient(textBox1.Text, 1300);
                //Console.WriteLine("connection was established");
                this.Invoke((Action)delegate { textBox2.Text = "Joining the server..."; });

                stream = client.GetStream();
                //Console.WriteLine("stream was retrieved from the connection");

                writer = new BinaryWriter(stream);
                reader = new BinaryReader(stream);

                int port = reader.Read();

                clients[0] = new TcpClient(textBox1.Text, 1300 + port);
                clientPort = 1300 + port;

                this.Invoke((Action)delegate { textBox2.Text = "Waiting for other players..."; });

                streams[0] = clients[0].GetStream();
                reader = new BinaryReader(streams[0]);

                byte[] bytes = new byte[10];
                bool lol = false;

                while (reader.Read()!=1)
                {
                    /*reader.ReadBytes(10);
                    for (int i = 0; i<10; i++)
                    {
                        if (bytes[i] == 1) lol = true;
                    }*/
                }
                    forcedClosing = false;
                    Invoke((Action) delegate { Close(); });
                
                /*
                arr = reader.ReadBytes(3);
                what = arr[0];
                x = arr[1];
                y = arr[2];

                if (what == 0)
                {
                    this.Invoke((Action)delegate { button1.Location = new Point(x, y); });
                }
                while (what != 3)
                {
                    if (what == 1)
                    {
                        this.Invoke((Action)delegate { button1.Location = new Point(x - 128 + button1.Location.X, y - 128 + button1.Location.Y); });
                    }

                    while (notsent)
                    {
                        if (Keyboard.IsKeyDown(Key.Escape)) { writer.Write("stop"); notsent = false; }
                        else if (Keyboard.IsKeyDown(Key.Left)) { writer.Write("left"); notsent = false; }
                        else if (Keyboard.IsKeyDown(Key.Right)) { writer.Write("right"); notsent = false; }
                        else if (Keyboard.IsKeyDown(Key.Up)) { writer.Write("up"); notsent = false; }
                        else if (Keyboard.IsKeyDown(Key.Down)) { writer.Write("down"); notsent = false; }
                        else Thread.Sleep(10);
                    }

                    arr = reader.ReadBytes(3);
                    what = arr[0];
                    x = arr[1];
                    y = arr[2];
                    notsent = true;
                }*/
            }
            catch (Exception)
            {
                client.Close();
            }

        }

        private void StepForward()
        {
            button1.Visible = false;
            button2.Visible = false;
            
            button3.Visible = true;
            button4.Visible = true;
            textBox2.Visible = true;
            if (isClient) textBox1.Visible = true;
            else
            {
                domainUpDown1.Visible = true;
                button5.Visible = true;
            }
        }

        private void StepBack()
        {

            button1.Visible = true;
            button2.Visible = true;

            button3.Visible = false;
            button4.Visible = false;
            button5.Visible = false;
            textBox1.Visible = false;
            textBox2.Visible = false;
            domainUpDown1.Visible = false;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            StepBack();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!servering.IsAlive)
            servering.Start();
        }
        private void button5_Click(object sender, EventArgs e)
        {
            toEnd = true;
            forcedClosing = false;


            this.Close();
        }

        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private string GetComputer_InternetIP()
        {
            // check IP using DynDNS's service
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org");
            // IMPORTANT: set Proxy to null, to drastically INCREASE the speed of request
            request.Proxy = null;
            WebResponse response = request.GetResponse();
            StreamReader stream = new StreamReader(response.GetResponseStream());


            // read complete response
            string ipAddress = stream.ReadToEnd();

            // replace everything and keep only IP
            return ipAddress.
                Replace("<html><head><title>Current IP Check</title></head><body>Current IP Address: ", string.Empty).
                Replace("</body></html>", string.Empty);
        }
    }
}
