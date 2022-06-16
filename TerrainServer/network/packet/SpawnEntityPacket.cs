using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainServer.network.packet
{
    public class SpawnEntityPacket : Packet
    {
        public EntityType entityType;
        public int entityId = -1;
        public float x, y, z, mx, my, mz;
        public SpawnEntityPacket(EntityType entityType, float x, float y, float z, float mx, float my, float mz)
        {
            packetType = PacketType.SpawnEntity;
            this.entityType = entityType;
            this.x = x; this.y = y; this.z = z;
            this.mx = mx; this.my = my; this.mz = mz;
        }

        public SpawnEntityPacket(EntityType entityType, int entityId, float x, float y, float z, float mx, float my, float mz)
        {
            packetType = PacketType.SpawnEntity;
            this.entityType = entityType;
            this.entityId = entityId;
            this.x = x; this.y = y; this.z = z;
            this.mx = mx; this.my = my; this.mz = mz;
        }

        public SpawnEntityPacket(byte[] data) : base(data)
        {
            
        }

        public override byte[] GetData()
        {
            List<byte> data = new List<byte>();
            data.Add((byte)packetType);
            data.Add((byte)entityType);
            data.AddRange(BitConverter.GetBytes(entityId));
            data.AddRange(BitConverter.GetBytes(x));
            data.AddRange(BitConverter.GetBytes(y));
            data.AddRange(BitConverter.GetBytes(z));
            data.AddRange(BitConverter.GetBytes(mx));
            data.AddRange(BitConverter.GetBytes(my));
            data.AddRange(BitConverter.GetBytes(mz));

            return data.ToArray();
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
            entityType = (EntityType)data[1];
            entityId = BitConverter.ToInt32(data, 2);
            x = BitConverter.ToSingle(data, 6);
            y = BitConverter.ToSingle(data, 10);
            z = BitConverter.ToSingle(data, 14);
            mx = BitConverter.ToSingle(data, 18);
            my = BitConverter.ToSingle(data, 22);
            mz = BitConverter.ToSingle(data, 26);
        }
    }
}
