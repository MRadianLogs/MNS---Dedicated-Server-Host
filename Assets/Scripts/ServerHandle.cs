using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(int clientOrigin, Packet packet)
    {
        int clientId = packet.ReadInt();
        string username = packet.ReadString();

        Debug.Log($"{Server.clients[clientOrigin].tcp.socket.Client.RemoteEndPoint} connected and is now player {clientOrigin}.");
        if (clientOrigin != clientId)
        {
            Debug.Log($"Player \"{username}\" (ID: {clientOrigin}) has assumed the wrong client ID ({clientId})!");
        }

        //Send player into game.
        Server.clients[clientOrigin].SendIntoGame(username);
    }

    public static void UDPTestReceived(int clientOrigin, Packet packet)
    {
        string msg = packet.ReadString();

        Debug.Log($"Received packet via UDP: {msg}");
    }

    public static void PlayerMovement(int clientOrigin, Packet packet)
    {
        bool[] inputs = new bool[packet.ReadInt()];

        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = packet.ReadBool();
        }
        Quaternion playerRotation = packet.ReadQuaternion();

        Server.clients[clientOrigin].player.SetInput(inputs, playerRotation);
    }
}
