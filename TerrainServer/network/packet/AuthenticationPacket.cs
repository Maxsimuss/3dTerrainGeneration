using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainServer.network.packet
{
    public class AuthenticationPacket : Packet
    {
        public byte[] hash;
        public AuthAction action;

        public AuthenticationPacket(AuthAction action, byte[] hash)
        {
            packetType = PacketType.Authentication;
            this.hash = hash;
            this.action = action;
        }

        public AuthenticationPacket(byte[] data) : base(data)
        {

        }

        public override byte[] GetData()
        {
            List<byte> data = new List<byte>();

            data.Add((byte)packetType);
            data.Add((byte)action);
            data.AddRange(hash);

            return data.ToArray();
        }

        protected override void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
            action = (AuthAction)data[1];
            hash = new byte[64];
            Array.Copy(data, 2, hash, 0, 64);
        }
    }

    public enum AuthAction
    {
        Register = 0,
        Login = 1,
    }
}
