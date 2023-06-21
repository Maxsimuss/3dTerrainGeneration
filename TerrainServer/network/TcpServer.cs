using System.Net.Sockets;

namespace TerrainServer.network
{
    public class TcpServer
    {
        public event OnClientConnected ClientConnected;
        public event OnPacketReceived PacketReceived;
        public event OnClientDisconnected ClientDisconnected;

        public delegate void OnClientConnected(TcpClient client, Stream session);
        public delegate void OnPacketReceived(Stream stream, Packet packet);
        public delegate void OnClientDisconnected(Stream stream);

        TcpListener listener;
        public TcpServer(int port)
        {
            listener = new TcpListener(port);
        }

        public void Start()
        {
            listener.Start();
            Console.WriteLine("Listening on 12345");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                RunClientAsync(client);
            }
        }

        private void RunClientAsync(TcpClient client)
        {
            Task.Run(() =>
            {
                NetworkStream stream = client.GetStream();

                ClientConnected(client, stream);
                while (true)
                {
                    byte[] buffer = new byte[1];
                    try
                    {
                        Console.WriteLine("Reading 1 byte");
                        stream.Read(buffer, 0, 1);
                    }
                    catch { break; }

                    PacketType type = (PacketType)buffer[0];
                    Console.WriteLine("Read packet type {0}", type);
                    byte[] packetData = new byte[type.GetLength() - 1];
                    try
                    {
                        Console.WriteLine("Reading {0} bytes", packetData.Length);
                        stream.Read(packetData, 0, packetData.Length);
                    }
                    catch { break; }
                    Console.WriteLine("Read the packet");

                    byte[] packetBuffer = new byte[packetData.Length + 1];
                    buffer.CopyTo(packetBuffer, 0);
                    packetData.CopyTo(packetBuffer, 1);
                    Console.WriteLine("Copied data to a buffer");

                    Packet packet = type.GetPacket(packetBuffer);

                    PacketReceived(stream, packet);
                }
                ClientDisconnected(stream);
            });
        }
    }
}
