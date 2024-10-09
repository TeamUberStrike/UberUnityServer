using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class TcpServer
{
    private TcpListener listener;
    private bool isRunning;
    private List<TcpClient> clients = new List<TcpClient>();
    private const int SERIAL_X = 12345;
    private const int SERIAL_Y = 67890;

    public TcpServer(string ip, int port)
    {
        listener = new TcpListener(IPAddress.Parse(ip), port);
        isRunning = true;
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("Server started, waiting for connections...");

        while (isRunning)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");
            clients.Add(client);

            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client)
    {
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ProcessPacket(buffer, bytesRead, stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling client: " + ex.Message);
            }
            finally
            {
                client.Close();
                clients.Remove(client);
                Console.WriteLine("Client disconnected.");
            }
        }
    }

    private void ProcessPacket(byte[] data, int length, NetworkStream stream)
    {
        ByteBuffer buffer = new ByteBuffer(data);

        //if (buffer.GetPointer() == 0) return; // No data

        byte protocol = buffer.GetByte();

        if (protocol == 0) // Handshake response
        {
            HandleHandshake(buffer, stream);
        }
        else if (protocol == 1) // Player position update
        {
            UpdatePlayerPositions(buffer);
        }
        else if (protocol == 2) // Various game actions
        {
            HandleGameActions(buffer);
        }
    }

    private void HandleHandshake(ByteBuffer buffer, NetworkStream stream)
    {
        if (buffer.GetInt() == SERIAL_Y && buffer.GetInt() == SERIAL_X)
        {
            int playerId = buffer.GetInt();
            string playerName = buffer.GetString();

            // Send back a confirmation and other player data
            ByteBuffer response = new ByteBuffer();
            response.Put((byte)0); // Response type
            response.Put(SERIAL_Y);
            response.Put(SERIAL_X);
            response.Put(playerId);
            // Send player count and other necessary data
            response.Put(1); // For example, number of players currently connected
            response.Put(playerId); // Send the player's ID for confirmation

            stream.Write(response.Trim().Get(), 0, response.Trim().Get().Length);
            Console.WriteLine($"Handshake completed for player: {playerName}");
        }
    }

    private void UpdatePlayerPositions(ByteBuffer buffer)
    {
        int playerCount = buffer.GetInt();
        for (int i = 0; i < playerCount; i++)
        {
            int playerId = buffer.GetInt();
            float x = buffer.GetFloat();
            float y = buffer.GetFloat();
            float z = buffer.GetFloat();
            // Here you would update your player positions on the server
            Console.WriteLine($"Player {playerId} moved to: {x}, {y}, {z}");
        }
    }

    private void HandleGameActions(ByteBuffer buffer)
    {
        byte actionType = buffer.GetByte();
        //int playerId = buffer.GetInt();

        switch (actionType)
        {
            case 0: // Change weapon
                int weaponId = buffer.GetInt();
                Console.WriteLine($"Player changed weapon to {weaponId}");
                break;
            case 1: // Fire weapon
                Console.WriteLine($"Player fired their weapon.");
                break;
            // Add more cases for other actions as defined in your client
            default:
                Console.WriteLine("Unknown action type.");
                break;
        }
    }

    public void Stop()
    {
        isRunning = false;
        listener.Stop();
    }

    static void Main(string[] args)
    {
        TcpServer server = new TcpServer("192.168.31.5", 8001);
        server.Start();
    }
}
