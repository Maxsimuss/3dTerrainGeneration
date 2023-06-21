using SuperSimpleTcp;
using System.Diagnostics;
using TerrainServer.network;
using TerrainServer.network.packet;

namespace TerrainServer
{
    public struct EntityInfo
    {
        public EntityType type;
        public float x, y, z;
        public float mx, my, mz;

        public EntityInfo(EntityType type, float x, float y, float z, float mx, float my, float mz)
        {
            this.type = type;
            this.x = x; this.y = y; this.z = z;
            this.mx = mx; this.my = my; this.mz = mz;
        }
    }

    public class Program
    {
        private static Random rand = new Random();
        private static Dictionary<string, int> sessions = new Dictionary<string, int>();
        private static Dictionary<int, EntityInfo> entities = new Dictionary<int, EntityInfo>();
        private static Dictionary<int, string> responsibilities = new Dictionary<int, string>();

        static object sessionLock = new object();

        private static int ENTITY_COUNT = 50, ENTITY_DISTANCE = 200;
        static SimpleTcpServer server = null;
        public static void Main(string[] args)
        {
            bool running = true;

            server = new("0.0.0.0", 12345);
            server.Events.ClientConnected += Events_ClientConnected;
            server.Events.DataReceived += Events_DataReceived;
            server.Events.ClientDisconnected += Events_ClientDisconnected;

            Console.WriteLine("Starting server...");
            Load();

            Timer t = null;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            t = new Timer(o =>
            {
                if (!running) return;
                Tick(sw.ElapsedMilliseconds);
                sw.Restart();
                t.Change(TimeSpan.FromMilliseconds(500), TimeSpan.Zero);
            }, null, TimeSpan.FromMilliseconds(500), TimeSpan.Zero);

            server.StartAsync();
            Console.WriteLine("Server started!");

            while (running)
            {
                Console.Write(" > ");
                string[] cmd = Console.ReadLine().Split(" ");
                switch (cmd[0])
                {
                    case "time":
                        if (cmd.Length != 2)
                        {
                            Console.WriteLine("Usage: time <time>");
                            break;
                        }
                        long add = 0;
                        if (long.TryParse(cmd[1], out add))
                        {
                            time += add * 1000;
                            Console.WriteLine("Time is now set to {0}.", time);
                        }
                        else
                        {
                            Console.WriteLine("Usage: time <time>");
                        }
                        break;
                    case "stop":
                    case "exit":
                        Console.WriteLine("Stopping...");
                        running = false;
                        server.Stop();

                        break;
                    case "entitylimit":
                        if (cmd.Length != 2)
                        {
                            Console.WriteLine("Usage: entitylimit <limit>");
                            break;
                        }
                        if (int.TryParse(cmd[1], out ENTITY_COUNT))
                        {
                            Console.WriteLine("Entity limit is set to {0}.", ENTITY_COUNT);
                        }
                        else
                        {
                            Console.WriteLine("Usage: entitylimit <limit>");
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
        }

        private static void Events_ClientDisconnected(object? sender, ConnectionEventArgs e)
        {
            RemovePlayer(e.IpPort);
            Console.WriteLine("player disconnected! connected: {0}", sessions.Count);
        }

        static Dictionary<string, Queue<byte>> streams = new Dictionary<string, Queue<byte>>();
        static object streamLock = new object();

        static string userFile = "users.cred";

        private static void Load()
        {
            if (!File.Exists(userFile))
            {
                return;
            }

            byte[] data = File.ReadAllBytes(userFile);

            for (int i = 0; i < data.Length / 64; i++)
            {
                byte[] buffer = new byte[64];
                Array.Copy(data, i * 64, buffer, 0, 64);
                accounts.Add(buffer);
            }

            Console.WriteLine("Loaded {0} accounts.", accounts.Count);
        }

        private static void Save(byte[] hash)
        {
            if (!File.Exists(userFile))
            {
                File.WriteAllBytes(userFile, hash);
                return;
            }
            byte[] data = File.ReadAllBytes(userFile);
            byte[] buffer = new byte[data.Length + 64];

            Array.Copy(data, buffer, data.Length);
            Array.Copy(hash, 0, buffer, data.Length, 64);

            File.WriteAllBytes(userFile, buffer);
        }

        static HashSet<byte[]> accounts = new HashSet<byte[]>();

        static int CurrentPhysicsTick = 0;

        private static void Events_DataReceived(object? sender, SuperSimpleTcp.DataReceivedEventArgs e)
        {
            lock (streamLock)
            {
                if (!streams.ContainsKey(e.IpPort))
                {
                    streams.Add(e.IpPort, new Queue<byte>());
                }

                Queue<byte> stream = streams[e.IpPort];

                for (int i = 0; i < e.Data.Length; i++)
                {
                    stream.Enqueue(e.Data[i]);
                }

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

                    //Console.WriteLine("[{0}:{1}] Received {2} Packet", DateTime.UtcNow.ToUniversalTime(), DateTime.UtcNow.Millisecond, type.ToString());

                    switch (type)
                    {
                        case PacketType.Input:
                            {
                                InputPacket packet = (InputPacket)p;
                                if (packet.entityId == -1)
                                    packet.entityId = sessions[e.IpPort];

                                if (entities.ContainsKey(packet.entityId))
                                {
                                    //EntityInfo i = entities[packet.entityId];
                                    //i.x = packet.x;
                                    //i.y = packet.y;
                                    //i.z = packet.z;
                                    //entities[packet.entityId] = i;
                                    CurrentPhysicsTick = Math.Max(CurrentPhysicsTick, packet.Frame);
                                    Broadcast(packet, e.IpPort);
                                }
                                break;
                            }
                        case PacketType.SpawnEntity:
                            {
                                SpawnEntityPacket packet = (SpawnEntityPacket)p;
                                AddEntity(new EntityInfo(packet.entityType, packet.x, packet.y, packet.z, packet.mx, packet.my, packet.mz), e.IpPort);
                                break;
                            }
                        case PacketType.DeSpawnEntity:
                            {
                                RemoveEntity(((DeSpawnEntityPacket)p).entityId);
                                break;
                            }
                        case PacketType.Authentication:
                            {
                                AuthenticationPacket packet = (AuthenticationPacket)p;
                                if (packet.action == AuthAction.Register)
                                {
                                    if (!accounts.Any(x => x.SequenceEqual(packet.hash)))
                                    {
                                        accounts.Add(packet.hash);
                                        Save(packet.hash);
                                        Console.WriteLine("A new user registered!");
                                    }
                                }
                                else
                                {
                                    if (accounts.Any(x => x.SequenceEqual(packet.hash)))
                                    {
                                        Console.WriteLine("A user logged in!");
                                        server.Send(e.IpPort, new ConfirmLoginPacket(sessions[e.IpPort], CurrentPhysicsTick).GetData());
                                    }
                                }
                                break;
                            }
                        default:
                            throw new Exception("Unknown packet received!");
                    }
                }
            }
        }

        private static void Events_ClientConnected(object? sender, ConnectionEventArgs e)
        {
            foreach (KeyValuePair<int, EntityInfo> info in entities)
            {
                server.Send(e.IpPort, new SpawnEntityPacket(info.Value.type, info.Key, info.Value.x, info.Value.y, info.Value.z, 0, 0, 0).GetData());
            }
            AddPlayer(e.IpPort);
            Console.WriteLine("player connected! count: {0}, ip: {1}", sessions.Count, e.IpPort);
        }


        private static double time = 1440 * 1000 / 2;
        private static void Tick(long dT)
        {
            //Console.WriteLine("Tick {0}", DateTime.UtcNow.ToString());

            time += dT;

            Broadcast(new SetTimePacket(time));

            //while (entities.Count < ENTITY_COUNT * sessions.Count)
            //{
            //    lock (sessionLock)
            //    {
            //        foreach (KeyValuePair<string, int> item in sessions)
            //        {
            //            if (entities.Count >= ENTITY_COUNT * sessions.Count)
            //            {
            //                break;
            //            }
            //            EntityInfo playerInfo = entities[item.Value];
            //            Vector3 pos = new Vector3(rand.NextSingle() - .5f, rand.NextSingle() - .5f, rand.NextSingle() - .5f);
            //            pos = pos / pos.Length();
            //            pos = pos * (20 + rand.NextSingle() * (ENTITY_DISTANCE - 20)) * 2;
            //            pos = pos + new Vector3(playerInfo.x, playerInfo.y, playerInfo.z);

            //            switch (rand.Next(4))
            //            {
            //                case 0: AddEntity(new EntityInfo(EntityType.Frog, pos.X, pos.Y, pos.Z, 0, 0, 0), item.Key); break;
            //                case 1: AddEntity(new EntityInfo(EntityType.BlueSlime, pos.X, pos.Y, pos.Z, 0, 0, 0), item.Key); break;
            //                case 2: AddEntity(new EntityInfo(EntityType.Demon, pos.X, pos.Y, pos.Z, 0, 0, 0), item.Key); break;
            //                case 3: AddEntity(new EntityInfo(EntityType.Spider, pos.X, pos.Y, pos.Z, 0, 0, 0), item.Key); break;
            //            }
            //        }
            //    }
            //}

            //List<KeyValuePair<int, EntityInfo>> list = new List<KeyValuePair<int, EntityInfo>>(entities);
            //foreach (KeyValuePair<int, EntityInfo> info in list)
            //{
            //    lock (sessionLock)
            //    {
            //        foreach (KeyValuePair<string, int> item in sessions)
            //        {
            //            EntityInfo playerInfo = entities[item.Value];
            //            if (!IsTooFar(info.Value.x, playerInfo.x) && !IsTooFar(info.Value.z, playerInfo.z))
            //            {
            //                goto InRenderDistance;
            //            }
            //        }
            //    }

            //    RemoveEntity(info.Key);

            //InRenderDistance:;
            //}
        }

        private static bool IsTooFar(float a, float b)
        {
            return Math.Abs(a - b) > ENTITY_DISTANCE;
        }

        private static void Broadcast(Packet packet, string exception = null)
        {
            lock (sessionLock)
            {
                foreach (string session in sessions.Keys)
                {
                    if (session == exception) continue;

                    try
                    {
                        server.Send(session, packet.GetData());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        private static void AddPlayer(string session)
        {
            int entityId = GetEntityId();

            EntityInfo info = new EntityInfo(EntityType.Player, 0, 0, 0, 0, 0, 0);
            entities.Add(entityId, info);

            Broadcast(new SpawnEntityPacket(info.type, entityId, info.x, info.y, info.z, info.mx, info.my, info.mz), session);

            lock (sessionLock)
            {
                sessions.Add(session, entityId);
            }
        }

        private static void RemovePlayer(string session)
        {
            int entityId = sessions[session];
            lock (sessionLock)
            {
                sessions.Remove(session);
            }
            RemoveEntity(entityId);

            if (sessions.Count < 1)
            {
                entities.Clear();
                responsibilities.Clear();
                return;
            }
            KeyValuePair<int, string>[] r = responsibilities.ToArray();
            foreach (KeyValuePair<int, string> resp in r)
            {
                if (resp.Value == session)
                {
                    string newSession = sessions.First().Key;
                    responsibilities.Remove(resp.Key);
                    responsibilities.Add(resp.Key, newSession);
                    server.Send(newSession, new ResponsibilityPacket(resp.Key).GetData());
                }
            }
        }

        private static void RemoveEntity(int entityId)
        {
            entities.Remove(entityId);
            responsibilities.Remove(entityId);
            Broadcast(new DeSpawnEntityPacket(entityId));
        }

        private static int AddEntity(EntityInfo info, string owner = null)
        {
            int entityId = GetEntityId();
            entities.Add(entityId, info);

            Broadcast(new SpawnEntityPacket(info.type, entityId, info.x, info.y, info.z, info.mx, info.my, info.mz));
            if (owner != null)
            {
                responsibilities.Add(entityId, owner);
                server.Send(owner, new ResponsibilityPacket(entityId).GetData());
            }

            return entityId;
        }

        private static int GetEntityId()
        {
            for (int i = 0; i < entities.Count + 1; i++)
            {
                if (!entities.ContainsKey(i))
                {
                    return i;
                }
            }

            throw new Exception("Cannot generate an entity id!");
        }
    }
}