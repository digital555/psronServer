using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice FinalVideo;

        ControlSmartPhone mControlSmartPhone = new ControlSmartPhone();

        public string wid;

        UdpClient udpClient1 = new UdpClient(11000);
        UdpClient udpClient2 = new UdpClient(11001);
        UdpClient udpClient3 = new UdpClient();

        string sqlConnectionData = "Server=localhost; Uid=root; Pwd=; Database=psron";

        /*Image piesStoi = Image.FromFile("stoi.png");
        Image piesSiedzi = Image.FromFile("siedzi.png");*/

        public Form1()
        {
            InitializeComponent();

            mControlSmartPhone.mIP = "10.3.2.19";
            mControlSmartPhone.mPort = 1234;

            mControlSmartPhone.Init();

            webBrowser1.Navigate("http://localhost/jakistest/test.html");
            //webBrowser1.Navigate("http://cyberdog.herokuapp.com/users/sign_in");
            //webBrowser1.Navigate("http://cyberdog.herokuapp.com/operation_map");




        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void showButton_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            /*if (checkBox1.Checked)
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            else
                pictureBox1.SizeMode = PictureBoxSizeMode.Normal;*/
        }


        public void appendText(string str)
        {            
            {
                /*string[] macierz_danych = str.Split(' ');

                label4.Invoke((MethodInvoker)delegate
                {
                    label4.Text = macierz_danych[0];
                });

                label5.Invoke((MethodInvoker)delegate
                {
                    label5.Text = macierz_danych[1];
                });*/

                label6.Invoke((MethodInvoker)delegate
                {
                    label6.Text = wid;
                });
            }


            /*if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<string>(appendText), str);
            }
            else
            {
                richTextBox1.ResetText();
                richTextBox1.AppendText(macierz_danych[0]);
            }   */
        }

        private void startConnection_Click(object sender, EventArgs e)
        {
            Thread thr1 = new Thread(droneDataRefresher);
            thr1.Start();
            Thread thr2 = new Thread(dogDataRefresher);
            thr2.Start();
        }

        void droneDataRefresher()
        {
            while (true)
            {
                    mControlSmartPhone._semaphore.WaitOne();
                    appendText(mControlSmartPhone.publiczny);
                    //appendText(DateTime.Now.ToString());
                    mControlSmartPhone._semaphore.Release();
                    Thread.Sleep(500);                
            }
        }

        void dogDataRefresher()
        {
            IPEndPoint RemoteIpEndPoint2 = new IPEndPoint(IPAddress.Any, 11001);

            string Dog;
            string[] DogGPS;
            string[] DogIMU;

            while (true)
            {
                Byte[] receiveBytesDog = udpClient2.Receive(ref RemoteIpEndPoint2);
                //Console.WriteLine(i.ToString());
                //Console.WriteLine(Encoding.ASCII.GetString(receiveBytesDog));

                Dog = Encoding.ASCII.GetString(receiveBytesDog);

                if (Dog.Substring(0, 3) == "GPS")
                {
                    // 3 i 4 to szerokosc i dlugosc
                    DogGPS = Dog.Split('\t');
                    Console.WriteLine(Dog);
                    if (DogGPS.Length > 4)
                    SqlConn(DogGPS[3], DogGPS[4]);
                }

                if (Dog.Substring(0, 3) == "IMU")
                {
                    DogIMU = Dog.Split('\t');
                    Console.WriteLine(Dog);
                }



            }
        }

        void refresherVideoUdp()
        {

            IPEndPoint RemoteIpEndPoint1 = new IPEndPoint(IPAddress.Any, 11000);
            while (true)
            {
                //IPEndPoint object will allow us to read datagrams sent from any source.


                // Blocks until a message returns on this socket from a remote host.
                Byte[] receiveBytes = udpClient1.Receive(ref RemoteIpEndPoint1);

                using (var ms = new MemoryStream(receiveBytes))
                {
                    pictureBox3.Invoke((MethodInvoker)delegate
                    {
                        pictureBox3.Image = Image.FromStream(ms);
                    });
                    pictureBox4.Invoke((MethodInvoker)delegate
                    {
                        pictureBox4.Image = Image.FromStream(ms);
                    });
                }
            }
        }


        private void upd_Click(object sender, EventArgs e)
        {
            //("rtsp://192.168.42.1/live");
            //("rtsp://mpv.cdn3.bigCDN.com:554/bigCDN/definst/mp4:bigbuckbunnyiphone_400.mp4");
            axVLCPlugin21.playlist.add(textBox2.Text);
            axVLCPlugin21.playlist.play();


            /*axVLCPlugin22.playlist.add("rtsp://mpv.cdn3.bigCDN.com:554/bigCDN/definst/mp4:bigbuckbunnyiphone_400.mp4");
            axVLCPlugin22.playlist.play();
            axVLCPlugin22.volume = 0;*/

            Thread thrVideoUdp = new Thread(refresherVideoUdp);
            thrVideoUdp.Start();

        }

        private void FinalVideo_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap video1 = (Bitmap)eventArgs.Frame.Clone();
            Bitmap video2 = new Bitmap(video1);
            pictureBox1.Image = video1;
            pictureBox2.Image = video1;
            video2.RotateFlip(RotateFlipType.Rotate180FlipNone);
            video2 = Crop(video2);
            video2.RotateFlip(RotateFlipType.Rotate180FlipNone);
            pictureBox5.Image = video2;
            pictureBox6.Image = video2;
            //wid = (String)video.PhysicalDimension.ToString();
        }

        private Bitmap Crop(Bitmap myBitmap)
        {
            // Clone a portion of the Bitmap object.
            var cloneRect = new Rectangle(0, 0, 336, 256);
            System.Drawing.Imaging.PixelFormat format =
                myBitmap.PixelFormat;
            Bitmap cloneBitmap = myBitmap.Clone(cloneRect, format);
            //Console.WriteLine(myBitmap.Width + " " + myBitmap.Height);
            // Draw the cloned portion of the Bitmap object.
            return cloneBitmap;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (FinalVideo.IsRunning == true) FinalVideo.Stop();

            FinalVideo = new VideoCaptureDevice(VideoCaptureDevices[comboBox1.SelectedIndex].MonikerString);
            FinalVideo.NewFrame += FinalVideo_NewFrame;
            FinalVideo.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo VideoCaptureDevice in VideoCaptureDevices)
            {
                comboBox1.Items.Add(VideoCaptureDevice.Name);
            }

            comboBox1.SelectedIndex = 0;
            FinalVideo = new VideoCaptureDevice();
        }

        private void SqlConn(string Lat, string Lng)
        {
            MySqlConnection connection = new MySqlConnection(sqlConnectionData);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE coordinates SET Lat = " + Lat.Substring(0, Lat.Length -1) + " , Lng = " + Lng.Substring(0, Lng.Length - 1) + "  WHERE id = 2";
                //MessageBox.Show(cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
            finally
            {
                connection.Close();
            }

        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            byte[] latarka = new byte[1];
            if (checkBox1.Checked) latarka[0] = (byte)'1';
            if (!checkBox1.Checked) latarka[0] = (byte)'0';

            udpClient3.Connect(textBox3.Text, 11002);
            udpClient3.Send(latarka, latarka.Length);
        }


        private void pictureBox1_Click_1(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }



        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }
    }
}

/*void doSomething()
        {
            //Tworzymy gniazdo do połączenia
            Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //ustalamy port i ip na ktorym ma działać nasz serwer 0 oznacza "127.0.0.1"
            System.Net.IPAddress ipaddress = System.Net.IPAddress.Parse("10.3.2.75");
            sck.Bind(new IPEndPoint(ipaddress, 1234));
            sck.Listen(1);//maksymalna ilosc polaczen oczekujacych

            //Tworzymy gniezdo nasłuchu które czaka na jakieś dane wysłane od jakiegoś klienta

            
                Socket accepted = sck.Accept();

            while (true)
            {

                //tworzy tablicę o takim rozmiarze jak rozmiar buforu danych czyli >= jak otrzymane dane
                byte[] Buffer = new byte[accepted.SendBufferSize];

                //zapisuje do Buffer pobrane dane i dodatkowo zrwaca ilość pobranych bajtów
                int bytesRead = accepted.Receive(Buffer);

                //tworzymy tablicę o takim rozmiarze jak otrzymane dane (nie jak całkowyty rozmiar buffora)
                byte[] formatted = new byte[bytesRead];

                //przepisujemy ze starej tablicy do nowej tylko przesłane dane
                for (int i = 0; i < bytesRead; i++)
                    formatted[i] = Buffer[i];

                //formatujemy dane do stringa
                string strData = Encoding.ASCII.GetString(formatted);
                //Console.Write(strData + "\r\n");
                appendText(strData + "\r\n");

                //oczekiwanie na zamknięcie konsoli
                //Console.Read();

                //zamknięcie gniazda
                sck.Close();
            }
        }*/
