﻿namespace TerrainServer.network
{
    public class Packet
    {
        public PacketType packetType;
        public Packet()
        {

        }

        public Packet(byte[] data)
        {
            Parse(data);
        }

        protected virtual void Parse(byte[] data)
        {
            packetType = (PacketType)data[0];
        }

        public virtual byte[] GetData()
        {
            return new byte[] { (byte)packetType };
        }
    }
}
