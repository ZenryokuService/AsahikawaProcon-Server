// 2015/10/20 18:47 ポート番号、ＩＰアドレス、チーム名を引数で指定できるように改良
// 2015/10/21  8:38 Portプロパティを実装
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CHaser
{
    public class Client
    {
        public static Client Create()
        {
            IPAddress ipAddress = null;
            int port = 0;
            string name = string.Empty;

            string[] args = Environment.GetCommandLineArgs();

            // Client.exe p:50000 a:192.168.0.1 n:name
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i][0])
                {
                    case 'p':
                        int.TryParse(args[i].Substring(2, args[i].Length - 2), out port);
                        break;
                    case 'a':
                        IPAddress.TryParse(args[i].Substring(2, args[i].Length - 2), out ipAddress);
                        break;
                    case 'n':
                        name = args[i].Substring(2, args[i].Length - 2);
                        break;
                }
            }

            if (port == 0)
            {
                Console.WriteLine("ポート番号を入力してください。");
                while (!int.TryParse(Console.ReadLine(), out port))
                {
                    Console.WriteLine("文字列が不正です。再入力してください。");
                }
            }
            if (ipAddress == null)
            {
                Console.WriteLine("IPアドレスを入力してください。");
                while (!IPAddress.TryParse(Console.ReadLine(), out ipAddress))
                {
                    Console.WriteLine("文字列が不正です。再入力してください。");
                }
            }
            if (name.Length == 0)
            {
                Console.WriteLine("チーム名を入力してください。");
                name = Console.ReadLine();
            }

            return new Client(ipAddress, port, name);
        }

        private readonly IPAddress IP_ADDRESS = IPAddress.Parse("127.0.0.1");
        private static readonly System.Text.Encoding ENCODE = System.Text.Encoding.GetEncoding(932);

        private Socket _socket;
        private int _port;

        private Client()
        {
            IPAddress ipAddress;
            int port;
            string name;

            Console.WriteLine("ＩＰアドレスを入力してください。");
            while (!IPAddress.TryParse(Console.ReadLine(), out ipAddress))
            {
                Console.WriteLine("ＩＰアドレスが不正です。再入力してください。");
            }

            Console.WriteLine("ポート番号を入力してください。");
            while (!int.TryParse(Console.ReadLine(), out port))
            {
                Console.WriteLine("ポート番号が不正です。再入力してください。");
            }

            Console.WriteLine("チーム名を入力してください。");
            name = Console.ReadLine();

            Init(ipAddress, port, name);
        }

        public Client(int port, string name)
        {
            Init(IP_ADDRESS, port, name);
        }

        public Client(IPAddress ipAddress, int port, string name)
        {
            Init(ipAddress, port, name);
        }

        protected bool Connected
        {
            get
            {
                return _socket != null;
                //			if(_socket != null) return _socket.Connected;
                //			else return false;
            }
        }

        protected void Init(IPAddress ipAddress, int port, string name)
        {
            _port = port;
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(ipAddress, _port);
                Thread.Sleep(100);
                byte[] nameBytes = ENCODE.GetBytes(name + "\r\n");
                _socket.Send(nameBytes);
                Console.WriteLine("チーム名：{0}", name);
            }
            catch (SocketException)
            {
                Console.WriteLine("接続失敗");
                Environment.Exit(1);
            }
        }

        public int[] GetReady()
        {
            return Ready();
        }
        public int[] Ready()
        {
            int[] value = null, ret = null;

            try
            {
                int dataSize = 0;
                byte[] buffer = new byte[100], data;

                dataSize = _socket.Receive(buffer);
                data = new byte[dataSize];
                Array.Copy(buffer, data, dataSize);

                _socket.Send(ENCODE.GetBytes("gr\r\n"));
                dataSize = _socket.Receive(buffer);
                data = new byte[dataSize];
                Array.Copy(buffer, data, dataSize);
                ret = Convert(data);

                if (ret[0] == 0) // ゲーム終了時
                {
                    Close();
                    Console.WriteLine("ゲーム終了");
                    System.Environment.Exit(0);
                }
            }
            catch (Exception) // サーバー異常終了時
            {
                if (Connected)
                {
                    Close();
                    Console.WriteLine("ゲーム異常終了");
                    System.Environment.Exit(-1);
                }
            }

            value = new int[ret.Length - 1];
            Array.Copy(ret, 1, value, 0, value.Length);
            return value;
        }

        public int[] WalkRight()
        {
            return Input("wr");
        }

        public int[] WalkLeft()
        {
            return Input("wl");
        }

        public int[] WalkUp()
        {
            return Input("wu");
        }

        public int[] WalkDown()
        {
            return Input("wd");
        }

        public int[] LookRight()
        {
            return Input("lr");
        }

        public int[] LookLeft()
        {
            return Input("ll");
        }

        public int[] LookUp()
        {
            return Input("lu");
        }

        public int[] LookDown()
        {
            return Input("ld");
        }

        public int[] SearchRight()
        {
            return Input("sr");
        }

        public int[] SearchLeft()
        {
            return Input("sl");
        }

        public int[] SearchUp()
        {
            return Input("su");
        }

        public int[] SearchDown()
        {
            return Input("sd");
        }

        public int[] PutRight()
        {
            return Input("pr");
        }

        public int[] PutLeft()
        {
            return Input("pl");
        }

        public int[] PutUp()
        {
            return Input("pu");
        }

        public int[] PutDown()
        {
            return Input("pd");
        }

        public int[] Input(string input)
        {
            int[] value = null, ret = null;

            try
            {
                int dataSize = 0;
                byte[] buffer = new byte[100], data;

                _socket.Send(ENCODE.GetBytes(input + "\r\n"));
                dataSize = _socket.Receive(buffer);
                data = new byte[dataSize];
                Array.Copy(buffer, data, dataSize);
                ret = Convert(data);
                _socket.Send(ENCODE.GetBytes("#\r\n"));

                if (ret[0] == 0) // ゲーム終了時
                {
                    Close();
                    Console.WriteLine("ゲーム終了");
                    System.Environment.Exit(0);
                }
            }
            catch (Exception) // サーバー異常終了時
            {
                if (Connected)
                {
                    Close();
                    Console.WriteLine("ゲーム異常終了");
                    System.Environment.Exit(-1);
                }
            }

            value = new int[ret.Length - 1];
            Array.Copy(ret, 1, value, 0, value.Length);
            return value;
        }

        protected int[] Convert(byte[] data)
        {
            string str = ENCODE.GetString(data);
            int[] ret = new int[str.Length - 2]; // \rと\nの分を差し引く

            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = str[i] - '0';
            }

            return ret;
        }

        protected void Close()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public int Port
        {
            get
            {
                return _port;
            }
        }
    }
}
