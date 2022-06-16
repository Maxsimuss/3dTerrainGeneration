using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainServer.network.packet
{
    public class ResponsibilityPacket : Packet
    {
        public int entityId;
        public ResponsibilityPacket(int entityId)
        {
            packetType = PacketType.Responsibility;
            this.entityId = entityId;
        }

        public ResponsibilityPacket(byte[] data) : base(data)
        {

        }

        public override byte[] GetData()
        {
            byte[] entityId = BitConverter.GetBytes(this.entityId);

            return new byte[] { (byte)packetType, entityId[0], entityId[1], entityId[2], entityId[3] };
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
            entityId = BitConverter.ToInt32(data, 1);
        }
    }
}
