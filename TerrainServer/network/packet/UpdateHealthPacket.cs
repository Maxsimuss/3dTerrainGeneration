using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainServer.network.packet
{
    public class UpdateHealthPacket : Packet
    {
        public int entityId;
        public float health;
        public UpdateHealthPacket(int entityId, float health)
        {
            packetType = PacketType.UpdateHealth;
            this.entityId = entityId;
            this.health = health;
        }

        public UpdateHealthPacket(byte[] data) : base(data)
        {

        }

        public override byte[] GetData()
        {
            byte[] entityId = BitConverter.GetBytes(this.entityId);
            byte[] health = BitConverter.GetBytes(this.health);

            return new byte[] { (byte)packetType, entityId[0], entityId[1], entityId[2], entityId[3], health[0], health[1], health[2], health[3] };
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
            entityId = BitConverter.ToInt32(data, 1);
            health = BitConverter.ToSingle(data, 5);
        }
    }
}
