namespace TerrainServer.network.packet
{
    public class DeSpawnEntityPacket : Packet
    {
        public int entityId;

        public DeSpawnEntityPacket(int entityId)
        {
            packetType = PacketType.DeSpawnEntity;
            this.entityId = entityId;
        }

        public DeSpawnEntityPacket(byte[] data) : base(data)
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
