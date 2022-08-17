using _3dTerrainGeneration.world;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network.packet;
using TerrainServer.network;
using _3dTerrainGeneration.entity;
using System.Net.Sockets;
using System.IO;
using OpenTK;
using SuperSimpleTcp;
using System.Collections.Concurrent;

namespace _3dTerrainGeneration.network
{
    public class Network
    {
        bool IsOnline = false;
        bool Connecting = false;

        World world;
        //TcpClient client;

        public SimpleTcpClient client = null;
        public Network(World world)
        {
            this.world = world;

            if(IsOnline)
            {
                //client = new SimpleTcpClient("127.0.0.1", 12345);
                client = new SimpleTcpClient("141.144.239.198", 12345);
                client.Events.Connected += Events_Connected;
                client.Events.DataReceived += Events_DataReceived;
                client.Events.Disconnected += Events_Disconnected;

                while(!client.IsConnected)
                {
                    try
                    {
                        client.Connect();
                    }
                    catch { }
                }
            }
            else
            {
                Window.login = true;
            }
        }

        private void Events_Disconnected(object sender, ConnectionEventArgs e)
        {
            Window.message = "Reconnecting...";

            foreach (Dictionary<int, DrawableEntity> en in world.entities.Values)
            {
                en.Clear();
            }
            buffer.Clear();
        }

        public void SendPacket(AuthenticationPacket packet)
        {
            client.Send(packet.GetData());
        }

        Queue<byte> stream = new Queue<byte>();
        object streamLock = new object();

        private void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            lock(streamLock)
                for (int i = 0; i < e.Data.Length; i++)
                {
                    stream.Enqueue(e.Data[i]);
                }
        }

        private void Events_Connected(object sender, ConnectionEventArgs e)
        {
            Window.message = "";
        }

        private double tickCounter = 0;

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
            if(IsOnline)
            {
                if (!client.IsConnected)
                {
                    if (!Connecting)
                    {
                        Connecting = true;

                        Task.Run(() =>
                        {
                            try
                            {
                                client.Connect();
                            } catch { }
                            Connecting = false;
                        });
                    }

                    return;
                }

                if (Window.login && (tickCounter += fT) >= .02)
                {
                    Send(new MovementPacket(
                    world.player.x, world.player.y, world.player.z,
                    world.player.motionX, world.player.motionY, world.player.motionZ, world.player.yaw).GetData());
                    tickCounter = 0;
                    Flush();
                }

                lock(streamLock)
                {
                    while (true)
                    {
                        if(stream.Count < 1) break;

                        PacketType type = (PacketType)stream.Peek();
                        int len = type.GetLength();

                        if (len > stream.Count) break;

                        byte[] buffer = new byte[len];
                        for (int i = 0; i < len; i++)
                        {
                            buffer[i] = stream.Dequeue();
                        }

                        Packet p = type.GetPacket(buffer);

                        switch (type)
                        {
                            case PacketType.Movement:
                                {
                                    MovementPacket packet = (MovementPacket)p;
                                    foreach (Dictionary<int, DrawableEntity> item in world.entities.Values)
                                    {
                                        if (item.ContainsKey(packet.entityId))
                                        {
                                            EntityBase entity = item[packet.entityId];
                                            entity.yaw = packet.yaw;
                                            entity.x = packet.x;
                                            entity.y = packet.y;
                                            entity.z = packet.z;
                                            entity.motionX = packet.mx;
                                            entity.motionY = packet.my;
                                            entity.motionZ = packet.mz;
                                            break;
                                        }
                                    }

                                    break;
                                }

                            case PacketType.SpawnEntity:
                                {
                                    SpawnEntityPacket packet = (SpawnEntityPacket)p;
                                    world.SpawnEntity(packet.entityId, packet.entityType, packet.x, packet.y, packet.z, packet.mx, packet.my, packet.mz);
                                    break;
                                }
                            case PacketType.DeSpawnEntity:
                                {
                                    DeSpawnEntityPacket packet = (DeSpawnEntityPacket)p;
                                    world.DespawnEntity(packet.entityId);
                                    break;
                                }
                            case PacketType.Responsibility:
                                {
                                    ResponsibilityPacket packet = (ResponsibilityPacket)p;
                                    world.SetResponsible(packet.entityId);
                                    break;
                                }
                            case PacketType.SetTime:
                                {
                                    SetTimePacket packet = (SetTimePacket)p;
                                    world.Time = packet.time;
                                    break;
                                }
                            case PacketType.ConfirmLogin:
                                {
                                    Window.login = true;
                                    break;
                                }
                        }
                    }
                }
            }
        }

        public void DespawnEntity(int entityId)
        {
            if (IsOnline)
            {
                Send(new DeSpawnEntityPacket(entityId).GetData());
            }
            else
            {
                world.DespawnEntity(entityId);
            }
        }

        public void SpawnEntity(EntityType type, double x, double y, double z, double mx, double my, double mz)
        {
            if (IsOnline)
            {
                Send(new SpawnEntityPacket(type, (float)x, (float)y, (float)z, (float)mx, (float)my, (float)mz).GetData());
            }
            else
            {
                int id = world.GetEntityId();
                world.SpawnEntity(id, type, (float)x, (float)y, (float)z, (float)mx, (float)my, (float)mz);
                world.SetResponsible(id);
            }
        }

        public void UpdateEntity(int entityId, double x, double y, double z, double motionX, double motionY, double motionZ, double yaw)
        {
            if(IsOnline)
                Send(new MovementPacket(entityId, x, y, z, motionX, motionY, motionZ, yaw).GetData());
        }
    }
}
