using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public int id;
    public TCP tcp;
    public UDP udp;
    public static int dataBufferSize = 4096;

    public Player player;

    public Client(int clientId)
    {
        id = clientId;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    public class TCP
    {
        public TcpClient socket;

        private readonly int id;

        private NetworkStream stream;
        private byte[] receiveBuffer;
        private Packet receivedDataPacket;

        public TCP(int newId)
        {
            id = newId;
        }

        public void Connect(TcpClient newSocket)
        {
            socket = newSocket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            stream = socket.GetStream();

            receivedDataPacket = new Packet();
            receiveBuffer = new byte[dataBufferSize];

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            ServerSend.Welcome(id, "Welcome to the server!");
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {e}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int receivedDataByteLength = stream.EndRead(result);
                //If no data received, this means lost connection, so disconnect.
                if (receivedDataByteLength <= 0)
                {
                    //Disconnect.
                    Server.clients[id].Disconnect();
                    return;
                }

                byte[] receivedData = new byte[receivedDataByteLength];
                Array.Copy(receiveBuffer, receivedData, receivedDataByteLength);

                //Handle received data.
                receivedDataPacket.Reset(HandleData(receivedData));

                //Keep reading more data from network stream.
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            }
            catch (Exception e)
            {
                Debug.Log($"Error recieving TCP data from network stream: {e}");
                //Disconnect.
                Server.clients[id].Disconnect();
            }
        }

        private bool HandleData(byte[] data)
        {
            int packetLength = 0;

            receivedDataPacket.SetBytes(data);

            if (receivedDataPacket.UnreadLength() >= 4)
            {
                packetLength = receivedDataPacket.ReadInt();
                if (packetLength <= 0)
                {
                    return true;
                }
            }

            while (packetLength > 0 && packetLength <= receivedDataPacket.UnreadLength())
            {
                byte[] packetBytes = receivedDataPacket.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        Server.packetHandlers[packetId](id, packet);
                    }
                });

                packetLength = 0;
                if (receivedDataPacket.UnreadLength() >= 4)
                {
                    packetLength = receivedDataPacket.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (packetLength <= 1)
            {
                return true;
            }

            return false;
        }

        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedDataPacket = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public IPEndPoint endPoint;

        private int id;

        public UDP(int newId)
        {
            id = newId;
        }

        public void Connect(IPEndPoint newEndpoint)
        {
            endPoint = newEndpoint;
            //ServerSend.UDPTest(id); //Used for testing UDP.
        }

        public void SendData(Packet packet)
        {
            Server.SendUDPData(endPoint, packet);
        }

        public void HandleData(Packet receivedPacket)
        {
            int packetLength = receivedPacket.ReadInt();
            byte[] packetBytes = receivedPacket.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(packetBytes))
                {
                    int packetId = packet.ReadInt();
                    Server.packetHandlers[packetId](id, packet);
                }
            });
        }

        public void Disconnect()
        {
            endPoint = null;
        }
    }

    public void SendIntoGame(string newPlayerName)
    {
        player = NetworkManager.instance.InstantiatePlayer();
        player.Initialize(id, newPlayerName);

        foreach (Client client in Server.clients.Values)
        {
            if (client.player != null)
            {
                if (client.id != id)
                {
                    ServerSend.SpawnPlayer(id, client.player);
                }
            }
        }

        foreach (Client client in Server.clients.Values)
        {
            if (client.player != null)
            {

                ServerSend.SpawnPlayer(client.id, player);

            }
        }
    }

    private void Disconnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

        ThreadManager.ExecuteOnMainThread(() =>
        {
            UnityEngine.Object.Destroy(player.gameObject);
            player = null;
        });

        tcp.Disconnect();
        udp.Disconnect();
    }

}
