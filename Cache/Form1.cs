using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Reflection;

namespace Server
{
    public partial class Form1 : Form
    {

        public static Socket ServerSocket;    //Declares the socket to listen on
        public static Socket socketAccept;    //Declares that the socket of the client is bound  
        public static Socket socket;    //Declares the socket used to communicate with a client

        public static int SFlag = 0;    //Connection success flag
        public static int CFlag = 0;    //Indicates that the client is closed

        Thread th1;     //Declaration thread 1
        Thread th2;     //Declaration thread 2

        Dictionary<string, string> dictionary_file = new Dictionary<string, string>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;    //Cross-thread resource access checking when executing a new thread prompts an error, so turn off the checking here
        }

        /*****************************************************************/
        #region Connect to the client
        private void button_Accpet_Click(object sender, EventArgs e)
        {

            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);    

            richTextBox_Receive.Text += "is connecting to client...\n";
            button_Accpet.Enabled = false;


            IPAddress IP = IPAddress.Parse(textBox_Addr.Text); //1. Bind the IP address and Port
            int Port = int.Parse(textBox_Port.Text);
            IPEndPoint iPEndPoint = new IPEndPoint(IP, Port);

            try
            {

                ServerSocket.Bind(iPEndPoint); //2. Use Bind() function to bind

                ServerSocket.Listen(10);     //3.Start listening           
                th1 = new Thread(Listen);
                th1.IsBackground = true;
                th1.Start(ServerSocket);
                Console.WriteLine("1");
            }
            catch
            {
                MessageBox.Show("The server has problems!");
            }
        }
        #endregion
        /*****************************************************************/

        /*****************************************************************/

        #region Establishing a connection to the client
        void Listen(Object sk)
        {
            socketAccept = sk as Socket;

            try
            {
                while (true)
                {

                    socket = socketAccept.Accept(); //4. Block the connection to client

                    CFlag = 0;  //If the connection is successful, set the client close flag to 0
                    SFlag = 1;  //If the connection succeeds, set the connection success flag to 1

                    richTextBox_Receive.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss  ") + textBox_Addr.Text + "Connecting Successfully!";
                    richTextBox_Receive.Text += "\r\n";
                    // Start the second thread to receive client data
                    th2 = new Thread(Receive);
                    th2.IsBackground = true;
                    th2.Start(socket);
                }
            }
            catch
            {

            }
        }
        #endregion
        /*****************************************************************/



        #region Receive client data
        void Receive(Object sk)
        {
            socket = sk as Socket;
            String s;


            while (true)
            {
                try
                {
                    if (CFlag == 0 && SFlag == 1)
                    {

                        byte[] recieve = new byte[1024*1024];  //5. Receive data
                        int len = socket.Receive(recieve);


                        if (recieve.Length > 0)  //6. Print received data
                        {
                            // If you receive a client stop flag
                            if (Encoding.ASCII.GetString(recieve, 0, len) == "*close*")
                            {
                                richTextBox_Receive.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss  ") + "The client has exited!" + "\n";
                                CFlag = 1;      // Set the client shutdown flag to 1

                                break;
                            }


                            //richTextBox_Receive.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss  ") + "Receive the data from the client：";
                            //richTextBox_Receive.Text += Encoding.ASCII.GetString(recieve, 0, len);
                            //richTextBox_Receive.Text += "\r\n";

                            
                            
                            string fileName = Encoding.ASCII.GetString(recieve, 0, len);


                            if (!dictionary_file.ContainsKey(fileName))
                            {
                                // IP Address to listen on. Loopback is the localhost
                                IPAddress ipAddr = IPAddress.Loopback;
                                // Port to listen on
                                int port = 8081;


                                byte command = 1;
                                // text need to be sent as binary number
                                // so, we store file name in a binary aray
                                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);

                                // store the number of bytes representing the file name as a byte array with 4 elements as int is 4 bytes long
                                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);

                                // Create a new byte array for holding the data to be sent to the server
                                // element 0 is the command
                                // element 1 to 4 is the length of the bytes representing the file name
                                // the remaining elements represent the file name
                                byte[] data = new byte[5 + fileNameBytes.Length];

                                // Copy the command, the length and the filename to the array to be sent to the server
                                data[0] = command;
                                Array.Copy(fileNameLengthBytes, 0, data, 1, fileNameLengthBytes.Length);
                                Array.Copy(fileNameBytes, 0, data, 5, fileNameBytes.Length);
                                TcpClient client = new TcpClient(ipAddr.ToString(), port); // Create a new connection
                                using (NetworkStream stream = client.GetStream())
                                {
                                    // Send the data to the server
                                    stream.Write(data, 0, data.Length);
                                    stream.Flush();

                                    // the StreamReader object is used to receive reply from the server
                                    string line;
                                   
                                    StreamReader reader = new StreamReader(stream);
                                    // print out the contents of the file received from the server
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        richTextBox_Send.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss  ") + line + "\n";
                                        s = line + "\r\n";
                                        dictionary_file.Add(fileName,line);
                                        byte[] File_send = Encoding.UTF8.GetBytes(line);
                                        socket.Send(File_send);
                                    }

                                    richTextBox_Send.Text += "user request: file " + fileName + " at " + DateTime.Now.ToString(" hh:mm:ss yyyy-MM-dd ") + "\n";
                                    richTextBox_Send.Text += "response: file " + fileName + " downloaded from the server" + "\n";
                                    reader.Close();
                                    stream.Close();
                                    client.Close();

                                }
                            }
                            else
                            {
                                
                                
                                //Send file fileName with buffers and default flags to the remote device.
                               
                               
                                byte[] FileContent = Encoding.UTF8.GetBytes(dictionary_file[fileName]);
                                socket.Send(FileContent);
                                
                               
                                richTextBox_Send.Text += "user request: file " + fileName + " at " + DateTime.Now.ToString(" hh:mm:ss yyyy-MM-dd ") + "\n";
                                richTextBox_Send.Text += "response: cached file " + fileName + "\n";
                            }
                           

                        }
                    }
                    else
                    {
                       
                        break;
                    }
                }
                catch
                {
                    MessageBox.Show("Cannot receive message!");
                }
            }
        }
        #endregion
        /*****************************************************************/

        /*****************************************************************/

        #region Shut down the server
        private void button_Close_Click(object sender, EventArgs e)
        {

            if (CFlag == 1) //If the client is connected, close thread 2 and socket. If the client is not connected, close thread 1 and other sockets directly
            {
                th2.Abort();
                socket.Close();
            }

            ServerSocket.Close();
            socketAccept.Close();
            th1.Abort();

            CFlag = 0;  //Reset the client flag to 0 to indicate that the connection is open
            SFlag = 0;  //Set the connection success flag program to 0, indicating that the connection exits
            button_Accpet.Enabled = true;
            richTextBox_Receive.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss  ");
            richTextBox_Receive.Text += "The server has closed" + "\n";
            MessageBox.Show("The server has closed!");
        }

        
         
        


        private void button_Clear_Click(object sender, EventArgs e)
        {
            richTextBox_Receive.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss  ")+"The cache memory clear!" + "\n";
            dictionary_file.Clear();
        }

        private void button_cache_file_Click(object sender, EventArgs e)
        {
            richTextBox_Receive.Text += DateTime.Now.ToString("yy-MM-dd hh:mm:ss  ");
            foreach (var kvp in dictionary_file)
            {               
                richTextBox_Receive.Text += "The cache file:"+ kvp.Key+" " + "\n";
            }
           
        }
        #endregion
        /*****************************************************************/

        /*****************************************************************/
    }
}
