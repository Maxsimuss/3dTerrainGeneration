using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainServer.network.packet
{
    public class ConfirmLoginPacket : Packet
    {
        public ConfirmLoginPacket()
        {
            packetType = PacketType.ConfirmLogin;
        }

        public ConfirmLoginPacket(byte[] data) : base(data)
        {

        }

        public override byte[] GetData()
        {
            return new byte[] { (byte)packetType };
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
        }
    }
}
