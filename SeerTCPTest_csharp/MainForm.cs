using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SeerTCPTest_csharp
{
    public struct SeerMessageHead
    {
        public byte sync;
        public byte version;
        public UInt16 number;
        public UInt32 length;
        public UInt16 type;
        private byte ref0;      //保留
        private byte ref1;      //保留
        private byte ref2;      //保留
        private byte ref3;      //保留
        private byte ref4;      //保留
        private byte ref5;      //保留

    };
    public struct SeerMessage
    {
        public SeerMessageHead head;
        public byte[] data;
        public int length()
        {
            return data.Length + Marshal.SizeOf(head);
        }
    }

    public partial class MainForm : Form
    {

        internal bool _queueStop { get; set; }

        //
        public static T bytesToStructure<T>(byte[] bytesBuffer)
        {
            if (bytesBuffer.Length < Marshal.SizeOf(typeof(T)))
            {
                throw new ArgumentException("size error");
            }

            IntPtr bufferHandler = Marshal.AllocHGlobal(bytesBuffer.Length);
            for (int index = 0; index < bytesBuffer.Length; index++)
            {
                Marshal.WriteByte(bufferHandler, index, bytesBuffer[index]);
            }
            T structObject = (T)Marshal.PtrToStructure(bufferHandler, typeof(T));
            Marshal.FreeHGlobal(bufferHandler);
            return structObject;
        }
        
        public byte[] seerMessageToBytes(SeerMessage msg)
        {
            MessageBox.Show(msg.length().ToString());
            byte[] bytes = new byte[msg.length()];
            IntPtr structPtr = Marshal.AllocHGlobal(msg.length());
            Marshal.StructureToPtr(msg, structPtr, false);
            Marshal.Copy(structPtr, bytes, 0, msg.length());
            Marshal.FreeHGlobal(structPtr);
            return bytes;
        }

        public byte[] seerMessageHeadToBytes(SeerMessageHead msg)
        {
            var hsize = Marshal.SizeOf(msg);
            byte[] bytes = new byte[hsize];
            IntPtr structPtr = Marshal.AllocHGlobal(hsize);
            Marshal.StructureToPtr(msg, structPtr, false);
            Marshal.Copy(structPtr, bytes, 0, hsize);
            Marshal.FreeHGlobal(structPtr);
            return bytes;
        }

        private byte[] hexStrTobyte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Trim(), 16);
            return returnBytes;
        }

        private byte[] normalStrToHexByte(string str)
        {
            byte[] result = new byte[str.Length];

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
            for (int i = 0; i < buffer.Length; i++)
            {
               result[i] = Convert.ToByte(buffer[i].ToString("X2"),16);
            }
            return result;
        }

        private string normalStrToHexStr(string str)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
            string result = string.Empty;
            for (int i = 0; i < buffer.Length; i++)
            {
                result += buffer[i].ToString("X2") + " ";
            }
            return result;
        }


        public MainForm()
        {
            InitializeComponent();
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            textBox_recv_data.Invoke(new EventHandler(delegate { textBox_recv_data.Text = "loading..."; }));
     
            try
            {
                TcpClient client = new TcpClient(textBox_ip.Text.Trim(), Convert.ToInt32(textBox_port.Text.Trim()));
                if (client.Connected)
                {
                    NetworkStream serverStream = client.GetStream();

                    var newmsg = new SeerMessage();

                    newmsg.head = bytesToStructure<SeerMessageHead>(hexStrTobyte(textBox_req_head.Text.Trim()));
                    newmsg.data = normalStrToHexByte(textBox_req_data.Text.Trim());

                    serverStream.Write(seerMessageHeadToBytes(newmsg.head), 0, Marshal.SizeOf(newmsg.head));
                    serverStream.Write(newmsg.data, 0, newmsg.data.Length);
                    serverStream.Flush();

                    byte[] inStream = new byte[16];
                    while (16 != serverStream.Read(inStream, 0, 16))
                    {
                        Thread.Sleep(20);
                    }

                    var recv_head = bytesToStructure<SeerMessageHead>(inStream);

                    byte[] recvbyte = BitConverter.GetBytes(recv_head.length);

                    Array.Reverse(recvbyte);

                    var dsize = BitConverter.ToUInt32(recvbyte, 0);

                    const int bufferSize = 512;
                    List<byte> datalist = new List<byte>();
                    int count = 0;
                    
                    while (true)
                    {
                        byte[] buffer = new byte[bufferSize];
                        int readSize = serverStream.Read(buffer, 0, bufferSize);

                        count += readSize;
                        datalist.AddRange(buffer);

                        if (count == dsize) {
                            break;
                        }

                        Thread.Sleep(10);
                    }
                    
                    textBox_recv_head.Text = BitConverter.ToString(seerMessageHeadToBytes(recv_head)).Replace("-", " ");//normalStrToHexStr(Encoding.UTF8.GetString(seerMessageHeadToBytes(recv_head)));

                    string str = System.Text.Encoding.UTF8.GetString(datalist.ToArray());

                    textBox_recv_data.Invoke(new EventHandler(delegate { textBox_recv_data.Text = str; }));

                    client.Close();
                }
            }
            catch (SocketException)
            {
                textBox_recv_data.Invoke(new EventHandler(delegate { textBox_recv_data.Text = ""; }));
                MessageBox.Show("Connect Error!");
            }
            catch (IOException)
            {
                textBox_recv_data.Invoke(new EventHandler(delegate { textBox_recv_data.Text = ""; }));
                MessageBox.Show("");
            }
        }

        private void textBox_req_data_TextChanged(object sender, EventArgs e)
        {
            var dsize = textBox_req_data.Text.Trim().Length;

            var head = bytesToStructure<SeerMessageHead>(hexStrTobyte(textBox_req_head.Text.Trim()));
                
            byte[] vv = hexStrTobyte(dsize.ToString("X8"));
             
            head.length = BitConverter.ToUInt32(vv,0);

            textBox_req_head.Invoke(new EventHandler(delegate 
            {
                textBox_req_head.Text = BitConverter.ToString(seerMessageHeadToBytes(head)).Replace("-", " ");
            }));      
        }
    }
}
