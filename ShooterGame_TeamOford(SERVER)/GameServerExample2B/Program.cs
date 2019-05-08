using System;

namespace GameServerExample2B
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            GameTransportIPv4 transport = new GameTransportIPv4();
            transport.Bind("127.0.0.1", 9999);

            GameServer.Init(transport);

            //Cube cube001 = new Cube();
            //cube001.SetPosition(0, 0, 5);

            //Cube cube002 = new Cube();
            //cube002.SetPosition(0, 3, 0);

            //Cube cube003 = new Cube();
            //cube003.SetPosition(8, 0, 0);

            GameServer.Start();
        }
    }
}
