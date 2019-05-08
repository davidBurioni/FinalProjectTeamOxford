using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;

namespace GameServerExample2B
{
    public static class GameServer
    {

        private delegate void GameCommand(byte[] data, EndPoint sender);

        private static Dictionary<byte, GameCommand> commandsTable;

        private static Dictionary<EndPoint, GameClient> clientsTable;
        private static Dictionary<uint, GameObject> gameObjectsTable;

        static void Join(byte[] data, EndPoint sender)
        {
            // check if the client has already joined
            if (clientsTable.ContainsKey(sender))
            {
                GameClient badClient = clientsTable[sender];
                badClient.Malus++;
                return;
            }

            GameClient newClient = new GameClient(sender);
            clientsTable[sender] = newClient;
            Avatar avatar = new Avatar();
            avatar.SetOwner(newClient);
            Packet welcome = new Packet(1, avatar.ObjectType, avatar.Id, avatar.X, avatar.Y, avatar.Z);
            welcome.NeedAck = true;
            newClient.Enqueue(welcome);

            // spawn all server's objects in the new client
            foreach (GameObject gameObject in gameObjectsTable.Values)
            {
                Packet spawn = new Packet(2, gameObject.ObjectType, gameObject.Id, gameObject.X, gameObject.Y, gameObject.Z);
                spawn.NeedAck = true;
                newClient.Enqueue(spawn);
            }


            // informs the other clients about the new one
            Packet newClientSpawned = new Packet(2, avatar.ObjectType, avatar.Id, avatar.X, avatar.Y, avatar.Z);
            newClientSpawned.NeedAck = true;
            SendToAllClientsExceptOne(newClientSpawned, newClient);

            Console.WriteLine("client {0} joined with avatar {1}", newClient, avatar.Id);
        }

        static void Ack(byte[] data, EndPoint sender)
        {
            if (!clientsTable.ContainsKey(sender))
            {
                return;
            }

            GameClient client = clientsTable[sender];
            uint packetId = BitConverter.ToUInt32(data, 1);
            client.Ack(packetId);
        }

        static void Update(byte[] data, EndPoint sender)
        {
            if (!clientsTable.ContainsKey(sender))
            {
                return;
            }
            GameClient client = clientsTable[sender];
            uint netId = BitConverter.ToUInt32(data, 1);
            if (gameObjectsTable.ContainsKey(netId))
            {
                GameObject gameObject = gameObjectsTable[netId];
                if (gameObject.IsOwnedBy(client))
                {
                    float x = BitConverter.ToSingle(data, 5);
                    float y = BitConverter.ToSingle(data, 9);
                    float z = BitConverter.ToSingle(data, 13);
                    gameObject.SetPosition(x, y, z);
                }
            }
        }

        static GameServer()
        {
            clientsTable = new Dictionary<EndPoint, GameClient>();
            gameObjectsTable = new Dictionary<uint, GameObject>();
            commandsTable = new Dictionary<byte, GameCommand>();
            commandsTable[0] = Join;
            commandsTable[3] = Update;
            commandsTable[255] = Ack;
        }


        public static void Init(IGameTransport gameTransport)
        {
            transport = gameTransport;
        }

        private static IGameTransport transport;

        private static float currentClock;
        public static void Start()
        {
            if (transport == null)
            {
                throw new Exception("please specify a transport");
            }
            Console.WriteLine("server started");
            while (true)
            {
                currentClock = Stopwatch.GetTimestamp() / (float)Stopwatch.Frequency;
                EndPoint sender = transport.CreateEndPoint();
                byte[] data = transport.Recv(256, ref sender);
                if (data != null)
                {
                    byte gameCommand = data[0];
                    if (commandsTable.ContainsKey(gameCommand))
                    {
                        commandsTable[gameCommand](data, sender);
                    }
                }

                foreach (GameClient client in clientsTable.Values)
                {
                    client.Process();
                }

                foreach (GameObject gameObject in gameObjectsTable.Values)
                {
                    gameObject.Tick();
                }
            }
        }

        public static bool Send(Packet packet, EndPoint endPoint)
        {
            return transport.Send(packet.GetData(), endPoint);
        }

        public static float Now
        {
            get
            {
                return currentClock;
            }
        }

        public static void SendToAllClients(Packet packet)
        {
            foreach (GameClient client in clientsTable.Values)
            {
                client.Enqueue(packet);
            }
        }

        public static void SendToAllClientsExceptOne(Packet packet, GameClient except)
        {
            foreach (GameClient client in clientsTable.Values)
            {
                if (client != except)
                    client.Enqueue(packet);
            }
        }

        public static void RegisterGameObject(uint id, GameObject gameObject)
        {
            gameObjectsTable[id] = gameObject;
        }
    }
}
