using SuperSimpleTcp;
using System.Collections.Generic;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.Engine.Networking
{
    public class NetworkEngine
    {
        private static NetworkEngine instance;
        public static NetworkEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NetworkEngine();
                }

                return instance;
            }
        }

        bool Connecting = false;

        public SimpleTcpClient client = null;
        private PacketHandler packetHandler = new PacketHandler();

        private NetworkEngine()
        {
            //client = new SimpleTcpClient("141.144.239.198", 12345);
            client = new SimpleTcpClient("127.0.0.1", 12345);
            client.Events.Connected += Events_Connected;
            client.Events.DataReceived += Events_DataReceived;
            client.Events.Disconnected += Events_Disconnected;
        }

        public void ConnectAsync()
        {
            Task.Run(Connect);
        }

        public void Connect()
        {
            Connecting = true;
            while (!client.IsConnected)
            {
                try
                {
                    client.Connect();
                }
                catch { }
            }
            Connecting = false;
        }

        private void Events_Disconnected(object sender, ConnectionEventArgs e)
        {
            buffer.Clear();
        }

        public void SendPacket(Packet packet)
        {
            client.Send(packet.GetData());
        }

        Queue<byte> stream = new Queue<byte>();
        object streamLock = new object();

        private void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (streamLock)
                for (int i = 0; i < e.Data.Length; i++)
                {
                    stream.Enqueue(e.Data[i]);
                }
        }

        private void Events_Connected(object sender, ConnectionEventArgs e)
        {

        }


        List<byte> buffer = new List<byte>();
        private void Flush()
        {
            if (!client.IsConnected) return;
            if (buffer.Count < 1) return;

            try
            {
                client.Send(buffer.ToArray());
            }
            catch
            {
                client.Disconnect();
            }
            buffer.Clear();
        }

        private void Send(byte[] data)
        {
            if (!client.IsConnected) return;
            buffer.AddRange(data);
            if (buffer.Count >= 32768)
            {
                Flush();
            }
        }

        public void Update(double fT)
        {
            if (!client.IsConnected)
            {
                if (!Connecting)
                {
                    ConnectAsync();
                }

                return;
            }

            lock (streamLock)
            {
                while (true)
                {
                    if (stream.Count < 1) break;

                    PacketType type = (PacketType)stream.Peek();
                    int len = type.GetLength();

                    if (len > stream.Count) break;

                    byte[] buffer = new byte[len];
                    for (int i = 0; i < len; i++)
                    {
                        buffer[i] = stream.Dequeue();
                    }

                    Packet p = type.GetPacket(buffer);

                    packetHandler.HandlePacket(p);
                }
            }
        }
    }
}
