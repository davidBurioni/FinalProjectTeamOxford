using System;
namespace GameServerExample2B
{
    public class Avatar : GameObject
    {
        public Avatar() : base(1)
        {
        }

        public override void Tick()
        {
            Packet packet = new Packet(3, Id, X, Y, Z);
            packet.OneShot = true;
            GameServer.SendToAllClients(packet);
        }
    }
}
