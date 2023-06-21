using TerrainServer.network;
using TerrainServer.network.packet;
using _3dTerrainGeneration.Engine.Physics;

namespace _3dTerrainGeneration.Engine.Networking
{
    public class PacketHandler
    {
        public PacketHandler()
        {
        }

        public void HandlePacket(Packet packet)
        {
            //switch (packet.packetType)
            //{
            //    case PacketType.ConfirmLogin: Handle((ConfirmLoginPacket)packet); break;
            //    case PacketType.SpawnEntity: Handle((SpawnEntityPacket)packet); break;
            //    case PacketType.DeSpawnEntity: Handle((DeSpawnEntityPacket)packet); break;
            //    case PacketType.Input: Handle((InputPacket)packet); break;

            //    default: break;
            //}
        }
    }
}
