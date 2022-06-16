using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainServer.network.packet
{
    public class MovementPacket : Packet
    {
        public int entityId = -1;
        public float x, y, z;
        public float mx, my, mz;
        public float yaw;

        public MovementPacket(byte[] data) : base(data)
        {

        }

        public MovementPacket(double x, double y, double z, double mx, double my, double mz, double yaw)
        {
            packetType = PacketType.Movement;
            this.x = (float)x; this.y = (float)y; this.z = (float)z;
            this.mx = (float)mx; this.my = (float)my; this.mz = (float)mz;
            this.yaw = (float)yaw;
        }

        public MovementPacket(int entityId, double x, double y, double z, double mx, double my, double mz, double yaw)
        {
            packetType = PacketType.Movement;
            this.entityId = entityId;
            this.x = (float)x; this.y = (float)y; this.z = (float)z;
            this.mx = (float)mx; this.my = (float)my; this.mz = (float)mz;
            this.yaw = (float)yaw;
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
            entityId = BitConverter.ToInt32(data, 1);
            x = BitConverter.ToSingle(data, 1 + 4);
            y = BitConverter.ToSingle(data, 1 + 4 + 4);
            z = BitConverter.ToSingle(data, 1 + 4 + 8);
            mx = BitConverter.ToSingle(data, 1 + 4 + 12);
            my = BitConverter.ToSingle(data, 1 + 4 + 16);
            mz = BitConverter.ToSingle(data, 1 + 4 + 20);
            yaw = BitConverter.ToInt16(data, 1 + 4 + 24) * 360F / short.MaxValue;
        }

        public override byte[] GetData()
        {
            List<byte> data = new List<byte>();
            data.Add((byte)packetType);
            data.AddRange(BitConverter.GetBytes(entityId));
            data.AddRange(BitConverter.GetBytes(x));
            data.AddRange(BitConverter.GetBytes(y));
            data.AddRange(BitConverter.GetBytes(z));
            data.AddRange(BitConverter.GetBytes(mx));
            data.AddRange(BitConverter.GetBytes(my));
            data.AddRange(BitConverter.GetBytes(mz));
            data.AddRange(BitConverter.GetBytes((short)(yaw / 360F * short.MaxValue)));
            
            return data.ToArray();
        }
    }
}
