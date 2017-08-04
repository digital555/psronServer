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
using System.Drawing.Imaging;

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
        UdpClient udpClient4 = new UdpClient();

        ImageConverter converter = new ImageConverter();

        private byte[] imagear;
        private byte[] arr;

        string sqlConnectionDataLocal = "Server=localhost; Uid=root; Pwd=; Database=psron";
        

        Image piesStoi = Image.FromFile("stoi.png");
        Image piesSiedzi = Image.FromFile("siedzi.png");

        public Form1()
        {
            InitializeComponent();

            mControlSmartPhone.mIP = "10.3.2.46";
            mControlSmartPhone.mPort = 1234;

            mControlSmartPhone.Init();

            InitializePB(pictureBox1);
            InitializePB(pictureBox2);
            InitializePB(pictureBox5);
            InitializePB(pictureBox6);
            pictureBox5.SizeMode = PictureBoxSizeMode.Zoom;

            webBrowser1.Navigate("http://localhost/jakistest/test.html");
            //webBrowser1.Navigate("http://cyberdog.herokuapp.com/users/sign_in");
            //webBrowser1.Navigate("http://cyberdog.herokuapp.com/operation_map");

            textBox2.Text = "udp://@192.168.1.100:1234";
            textBox3.Text = "192.168.1.120";
        }

        /// <summary>
        /// Funkcja uruchamia dwa oddzielne wątki dla odswieżania danych telemetrycznych z psa i z drona. Wywoływana jest przez uzycie buttona "Start dane".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void startConnection_Click(object sender, EventArgs e)
        {
            Thread thr1 = new Thread(droneDataRefresher);
            thr1.Start();
            Thread thr2 = new Thread(dogDataRefresher);
            thr2.Start();
        }

        /// <summary>
        /// Funkcja odpowiedzialna jest za pobieranie z klasy "ControlSmartPhone" zmiennej o nazwie "publiczny", w której zawarte sa dane telemetryczne
        /// </summary>

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

        /// <summary>
        /// Funkcja nasłuchuje dane telemetryczne z IMU na psie przychodzące po UDP na port 11001. Wywołując funkcję SqlConn, aktualizuje wartości w bazie danych sql. Na podstawie odczytów z IMU funkcja określa położenie grzbietu psa oraz wizualizuje czy pies stoi czy siedzi.
        /// </summary>

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
                    if (DogIMU.Length > 3)
                    {
                        int pieseueue = Int32.Parse(DogIMU[3].Substring(0, 3));
                        
                        if (pieseueue > 185 | pieseueue < 165)
                        {
                            pictureBox7.Image = new Bitmap(piesSiedzi);
                        }
                        else
                        {
                            pictureBox7.Image = new Bitmap(piesStoi);
                        }
                    }

                    //Console.WriteLine(Dog);
                }
            }
        }

        /// <summary>
        /// Funkcja odpowiada za odbiór danych zawierających obraz pochodzący z jednej z kamrer z psa. Wyświetla go na ekranie podzielonym oraz w oknie dedykowanym dla tej kamery.
        /// </summary>

        void refresherVideoUdp()
        {
            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint1 = new IPEndPoint(IPAddress.Any, 11000);
            while (true)
            {                
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

        /// <summary>
        /// Funkcja w skutek uzycia buttona "Start video" uruchamia wyświetlanie video z urządzeń umieszczonych na psie. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void upd_Click(object sender, EventArgs e)
        {
            //("rtsp://192.168.42.1/live");
            //("rtsp://mpv.cdn3.bigCDN.com:554/bigCDN/definst/mp4:bigbuckbunnyiphone_400.mp4");
            if (textBox2.Text.Length > 0)
            {
                axVLCPlugin22.playlist.add(textBox2.Text);
                axVLCPlugin22.playlist.play();
            }
            /*axVLCPlugin22.playlist.add("rtsp://mpv.cdn3.bigCDN.com:554/bigCDN/definst/mp4:bigbuckbunnyiphone_400.mp4");
            axVLCPlugin22.playlist.play();
            axVLCPlugin22.volume = 0;*/

            Thread thrVideoUdp = new Thread(refresherVideoUdp);
            thrVideoUdp.Start();

        }

        /// <summary>
        /// Funkcja inicjalizująca pola "PictureBox" odpowiadające za wyświetlanie obrazu.
        /// </summary>
        /// <param name="pb">Wybrany PictureBox</param>

        private void InitializePB(PictureBox pb)
        {
            if (pb.Image == null)
            {
                Bitmap bmp = new Bitmap(pb.Width, pb.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Black);
                }
                pb.Image = bmp;
            }
        }

        /// <summary>
        /// Funkcja wyświetla wybraną bitmapę na wskazanym PictureBoxie
        /// </summary>
        /// <param name="bmpbmp">Wybrana bitmapa</param>
        /// <param name="pict">Wskazany picturebox</param>

        void bitmaptopicture(Bitmap bmpbmp, PictureBox pict)
        {
            try
            {
                using (Graphics g = Graphics.FromImage(pict.Image))
                {
                    g.DrawImage((Bitmap)bmpbmp, 0, 0, pict.Width, pict.Height);
                    //g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    //g.DrawImage(eventArgs.Frame, new Rectangle(Point.Empty, mBmp.Size));
                }
                pict.Invalidate();

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// Funcja tworzy crop fragmentu obrazu pobieranego z pilota drona i wyświetla go na picturebox odpowiedzialnym za kamerę termowizyjną drona.
        /// </summary>
        /// <param name="bmpbmp">Obraz pierwotny</param>
        /// <param name="pict">Wskazany picturebox</param>

        void bitmaptopicturecropp(Bitmap bmpbmp, PictureBox pict)
        {
            try
            {
                using (Graphics g = Graphics.FromImage(pict.Image))
                {
                    var sourcerect = new Rectangle(1380, 750, 1920, 1080);
                    var cloneRect = new Rectangle(0,0, 2300, 2200);

                    
                    g.DrawImage((Bitmap)(bmpbmp), cloneRect, sourcerect, GraphicsUnit.Pixel);
                    //g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    //g.DrawImage(eventArgs.Frame, new Rectangle(Point.Empty, mBmp.Size));
                }
                pict.Invalidate();

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// Funkcja pochodząca z biblioteki 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        
        private void FinalVideo_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap video1 = (Bitmap)eventArgs.Frame.Clone();
            Bitmap video2 = (Bitmap)eventArgs.Frame.Clone();
            Bitmap video3 = (Bitmap)eventArgs.Frame.Clone();
            /*pictureBox1.Image = video1;
            pictureBox2.Image = video1;*/

            bitmaptopicture(eventArgs.Frame, pictureBox1);
            bitmaptopicture(eventArgs.Frame, pictureBox2);

            bitmaptopicturecropp(eventArgs.Frame, pictureBox6);
            bitmaptopicturecropp(eventArgs.Frame, pictureBox5);


            /*video2.RotateFlip(RotateFlipType.Rotate180FlipNone);
            video2 = Crop(video2);
            video2.RotateFlip(RotateFlipType.Rotate180FlipNone);
            pictureBox5.Image = video2;
            pictureBox6.Image = video2;*/

            //wid = (String)video.PhysicalDimension.ToString();

            imagear = (byte[])converter.ConvertTo(video3, typeof(byte[]));

            MemoryStream ms = new MemoryStream();
            Image image = Image.FromStream(new MemoryStream(imagear));
            image.Save(ms, ImageFormat.Jpeg);
            arr = ms.ToArray();

            

            if (textBox4.Text.Length > 0)
                udpClient4.Send(arr, arr.Length);

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

        /// <summary>
        /// Funkcja korzystająca z bibliotegi AForge do uruchomienia wyświetlania video z urządzeń USB.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void button1_Click(object sender, EventArgs e)
        {
            if (FinalVideo.IsRunning == true) FinalVideo.Stop();

            FinalVideo = new VideoCaptureDevice(VideoCaptureDevices[comboBox1.SelectedIndex].MonikerString);
            FinalVideo.NewFrame += FinalVideo_NewFrame;
            FinalVideo.Start();

            if (textBox4.TextLength > 0)
                udpClient4.Connect(textBox4.Text, 11004);
        }

        /// <summary>
        /// Funkcja wyszukująca urządzenia USB, z których można pobrać video.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

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

        /// <summary>
        /// Funkcja odpowiedzialna za przesyłanie położenia geograficznego do bazy mysql.
        /// </summary>
        /// <param name="Lat">Długość geograficzna</param>
        /// <param name="Lng">Szerokość geograficzna</param>

        private void SqlConn(string Lat, string Lng)
        {
            MySqlConnection connectionLocal = new MySqlConnection(sqlConnectionDataLocal);

            MySqlCommand cmdL;

            connectionLocal.Open();

            try
            {
                cmdL = connectionLocal.CreateCommand();

                cmdL.CommandText = "UPDATE coordinates SET Lat = " + Lat.Substring(0, Lat.Length -1) + " , Lng = " + Lng.Substring(0, Lng.Length - 1) + "  WHERE id = 2";
                //MessageBox.Show(cmd.CommandText);
                cmdL.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
            finally
            {
                connectionLocal.Close();
            }

        }

        /// <summary>
        /// Funkcja opdowiedzialna za zapalanie i gaszenie latarki lub innego urządzenia znajdującego się na kamizelce psa.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            byte[] latarka = new byte[1];
            if (checkBox1.Checked) latarka[0] = (byte)'1';
            if (!checkBox1.Checked) latarka[0] = (byte)'0';

            udpClient3.Connect(textBox3.Text, 11002);
            udpClient3.Send(latarka, latarka.Length);
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
            /*{
                string[] macierz_danych = str.Split(' ');

                label4.Invoke((MethodInvoker)delegate
                {
                    label4.Text = macierz_danych[0];
                });

                label5.Invoke((MethodInvoker)delegate
                {
                    label5.Text = macierz_danych[1];
                });

                label6.Invoke((MethodInvoker)delegate
                {
                    label6.Text = wid;
                });
            }*/


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

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
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
