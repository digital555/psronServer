using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using MySql.Data.MySqlClient;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    class ControlSmartPhone
    {
        public string mIP = " ";
        public int mPort = 0;
        public byte[] mDataHID = new byte[64];
        private int mSleepTime = 1;
        public bool mWork = true;
        private Thread mThread = null;

        public String publiczny = "";

        public string mPacket = " ";

        public bool mServerConnected = false;
        private object ipHostInfo;

        public Semaphore _semaphore = new Semaphore(1, 1);

        public bool flaga1 = true;

        string sqlConnectionData = "Server=localhost; Uid=root; Pwd=; Database=psron";




        ~ControlSmartPhone()
        {
            mWork = false;
            if (mServerConnected)
                Thread.Sleep(250);
        }

        public void Init()
        {
           mThread = new Thread(new ThreadStart(this.OpenSocket));


            // Start the thread
            mThread.Start();
            mThread.Priority = System.Threading.ThreadPriority.Highest;


        }

        private void SqlConn(string Lat, string Lng)
        {
            MySqlConnection connection = new MySqlConnection(sqlConnectionData);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE coordinates SET Lat = " + Lat + " , Lng = " + Lng + "  WHERE id = 1";
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




        private void OpenSocket()
        {

            byte[] bytes = new byte[1024];

            mServerConnected = false;

            IPAddress ipAddress = IPAddress.Parse(mIP); // 
            //ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, mPort);

            Socket sender = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sender.Connect(remoteEP);
                sender.NoDelay = true;
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                return;
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
                return;
            }

            mServerConnected = true;

            bool write = true;

            string str = "x\n";
            byte[] msg = Encoding.ASCII.GetBytes(str);

            byte[] packetSize = new byte[4];
            byte[] dataHID = new byte[64];

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();



            try
            {
                while (mWork)
                {



                    int bytesSent = sender.Send(msg);
                    
                    if (write)
                    {

                        // Receive the response from the remote device.
                        int bytesRec = sender.Receive(packetSize);

                        int s0 = packetSize[0];
                        int s1 = packetSize[1];
                        int s2 = packetSize[2];
                        int s3 = packetSize[3];

                        int size = (s0 << 24) + (s1 << 16) + (s2 << 8)+ s3;
                        //size = 8;

 

                        byte[] packet = new byte[size];
                        bytesRec = sender.Receive(packet);
                        //publiczny = DateTime.Now.ToString() + " ";

                        //publiczny = String.Format("nr paczki {0}", nr_paczki);

                        _semaphore.WaitOne();

                        publiczny = Encoding.ASCII.GetString(packet);        
  
                        _semaphore.Release(1);

                        string[] macierz_danych = publiczny.Split(' ');     

                        SqlConn(macierz_danych[0], macierz_danych[1].Substring(0, macierz_danych[1].Length - 4)); //-4 urywa niepozadane znaki na koncu

                        Thread.Sleep(500);
                        //publiczny = packet[4].ToString();


                        /*mPacket = Encoding.ASCII.GetString(packet, 
                        64, size - 64);*/



                    }



                    
                }
            }
            catch (Exception e)
            {
                /*Console.WriteLine("Unexpected exception : {0}", 
                e.ToString());*/

            }

            // Release the socket.
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
            mServerConnected = false;
        }


    }
}

