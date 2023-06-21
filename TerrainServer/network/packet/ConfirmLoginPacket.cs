namespace TerrainServer.network.packet
{
    public class ConfirmLoginPacket : Packet
    {
        public int EntityId = 0;
        public int PhysicsFrame = 0;

        public ConfirmLoginPacket(int EntityId, int PhysicsFrame)
        {
            packetType = PacketType.ConfirmLogin;
            this.EntityId = EntityId;
            this.PhysicsFrame = PhysicsFrame;
        }

        public ConfirmLoginPacket(byte[] data) : base(data)
        {

        }

        public override byte[] GetData()
        {
            List<byte> data = new List<byte>();

            data.Add((byte)packetType);
            data.AddRange(BitConverter.GetBytes(EntityId));
            data.AddRange(BitConverter.GetBytes(PhysicsFrame));

            return data.ToArray();
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];

            EntityId = BitConverter.ToInt32(data, 1);
            PhysicsFrame = BitConverter.ToInt32(data, 5);
        }
    }
}
