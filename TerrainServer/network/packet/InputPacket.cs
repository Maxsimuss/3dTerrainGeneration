using System.Numerics;

namespace TerrainServer.network.packet
{
    public class InputPacket : Packet
    {
        public int entityId;
        public int Frame = 0;
        public Vector3 Movement;
        public float Yaw, Pitch;

        public InputPacket(byte[] data) : base(data)
        {

        }

        public InputPacket(Vector3 Movement, float Yaw, float Pitch, int frame, int entityId = -1)
        {
            packetType = PacketType.Input;
            this.Movement = Movement;
            this.Yaw = Yaw;
            this.Pitch = Pitch;
            this.Frame = frame;
            this.entityId = entityId;
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
            entityId = BitConverter.ToInt32(data, 1);
            Movement.X = BitConverter.ToSingle(data, 1 + 4);
            Movement.Y = BitConverter.ToSingle(data, 1 + 4 + 4);
            Movement.Z = BitConverter.ToSingle(data, 1 + 4 + 8);
            Yaw = BitConverter.ToSingle(data, 1 + 4 + 12);
            Pitch = BitConverter.ToSingle(data, 1 + 4 + 16);
            Frame = BitConverter.ToInt32(data, 1 + 4 + 20);
        }

        public override byte[] GetData()
        {
            List<byte> data = new List<byte>();
            data.Add((byte)packetType);
            data.AddRange(BitConverter.GetBytes(entityId));
            data.AddRange(BitConverter.GetBytes(Movement.X));
            data.AddRange(BitConverter.GetBytes(Movement.Y));
            data.AddRange(BitConverter.GetBytes(Movement.Z));
            data.AddRange(BitConverter.GetBytes(Yaw));
            data.AddRange(BitConverter.GetBytes(Pitch));
            data.AddRange(BitConverter.GetBytes(Frame));

            return data.ToArray();
        }
    }
}
