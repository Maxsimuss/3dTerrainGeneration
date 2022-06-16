using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network.packet;

namespace TerrainServer.network
{
    public enum PacketType
    {
        SpawnEntity = 0x0,
        DeSpawnEntity = 0x1,
        Movement = 0x2,
        Responsibility = 0x3,
        UpdateHealth = 0x4,
        SetTime = 0x5,
        Authentication = 0x6,
        ConfirmLogin = 0x7,
    }

    public static class PacketExtensions
    {
        public static int GetLength(this PacketType packetType)
        {
            switch (packetType)
            {
                case PacketType.SpawnEntity:
                    return 30;
                case PacketType.DeSpawnEntity:
                    return 5;
                case PacketType.Movement:
                    return 31;
                case PacketType.Responsibility:
                    return 5;
                case PacketType.UpdateHealth:
                    return 9;
                case PacketType.SetTime:
                    return 9;
                case PacketType.Authentication:
                    return 66;
                case PacketType.ConfirmLogin:
                    return 1;
                default:
                    throw new Exception("Unknown packet received");
            }
        }

        public static Packet GetPacket(this PacketType packetType, byte[] data)
        {
            switch (packetType)
            {
                case PacketType.SpawnEntity:
                    return new SpawnEntityPacket(data);
                case PacketType.DeSpawnEntity:
                    return new DeSpawnEntityPacket(data);
                case PacketType.Movement:
                    return new MovementPacket(data);
                case PacketType.Responsibility:
                    return new ResponsibilityPacket(data);
                case PacketType.UpdateHealth:
                    return new UpdateHealthPacket(data);
                case PacketType.SetTime:
                    return new SetTimePacket(data);
                case PacketType.Authentication:
                    return new AuthenticationPacket(data);
                case PacketType.ConfirmLogin:
                    return new ConfirmLoginPacket(data);
                default:
                    throw new Exception("Unknown packet received");
            }
        }
    }
}
