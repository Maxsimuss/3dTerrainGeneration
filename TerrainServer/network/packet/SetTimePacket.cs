using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainServer.network.packet
{
    public class SetTimePacket : Packet
    {
        public double time;
        public SetTimePacket(double time)
        {
            packetType = PacketType.SetTime;
            this.time = time;
        }

        public SetTimePacket(byte[] data) : base(data)
        {

        }

        public override byte[] GetData()
        {
            byte[] v = BitConverter.GetBytes(time);
            return new byte[] { (byte)packetType, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7] };
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
            time = BitConverter.ToDouble(data, 1);
        }
    }
}
