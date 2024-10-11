using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                InitiateHandshake(stream);

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

    private void InitiateHandshake(NetworkStream stream)
    {
        ByteBuffer handshakeRequest = new ByteBuffer();
        //handshakeRequest.Put((byte)0); // Handshake request protocol
        handshakeRequest.Put(SERIAL_X);
        handshakeRequest.Put(SERIAL_Y);
        int id = 1;
        handshakeRequest.Put(id);
        int playerCount = 1;
        handshakeRequest.Put(playerCount);

        int playerId = 1;
        handshakeRequest.Put(playerId);
        string playerJoined = "HaZard";
        handshakeRequest.Put(playerJoined);
        handshakeRequest.Put(playerId);

        int appearance = -1; // 7 times
        handshakeRequest.Put(appearance);
        handshakeRequest.Put(appearance);
        handshakeRequest.Put(appearance);
        handshakeRequest.Put(appearance);
        handshakeRequest.Put(appearance);
        handshakeRequest.Put(appearance);
        handshakeRequest.Put(appearance);

        int playerStats = 0; // 2 times
        handshakeRequest.Put(playerStats);
        handshakeRequest.Put(playerStats);

        stream.Write(handshakeRequest.Trim().Get(), 0, handshakeRequest.Trim().Get().Length);
        Console.WriteLine("Handshake initiated with client.");
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
            string playerName = buffer.GetString();


            //// Send back a confirmation and other player data
            //ByteBuffer response = new ByteBuffer();
            //response.Put((byte)0); // Response type
            //response.Put(SERIAL_Y);
            //response.Put(SERIAL_X);
            //response.Put(playerId);
            //// Send player count and other necessary data
            //response.Put(1); // For example, number of players currently connected
            //response.Put(playerId); // Send the player's ID for confirmation
            //stream.Write(response.Trim().Get(), 0, response.Trim().Get().Length);

            Console.WriteLine($"Handshake completed for player: {playerName}");
        }
    }

    private void UpdatePlayerPositions(ByteBuffer buffer)
    {
        float x = buffer.GetFloat();
        float y = buffer.GetFloat();
        float z = buffer.GetFloat();

        float xr = buffer.GetFloat();
        float yr = buffer.GetFloat();
        float zr = buffer.GetFloat();

        float xc = buffer.GetFloat();
        float yc = buffer.GetFloat();
        float zc = buffer.GetFloat();

        //Console.WriteLine($"Player moved to: {x}, {y}, {z}");

    }

    private void HandleGameActions(ByteBuffer buffer)
    {
        int actionType = buffer.GetInt();

        switch (actionType)
        {
            case 0: // Change weapon
                int weaponId = buffer.GetInt();
                Console.WriteLine($"Player changed weapon to {weaponId}");
                break;

            case 1: // Fire weapon
                Console.WriteLine("Player fired their weapon.");
                break;

            case 2: // Damage dealt to another player
                int receiverId = buffer.GetInt();
                float damageAmount = buffer.GetFloat();
                int damageCriticalCode = buffer.GetInt();
                float posX = buffer.GetFloat();
                float posY = buffer.GetFloat();
                float posZ = buffer.GetFloat();
                Console.WriteLine($"Player dealt {damageAmount} damage to {receiverId} (critical code {damageCriticalCode}) at position ({posX}, {posY}, {posZ})");
                break;

            case 3: // Player died
                int killedId = buffer.GetInt();
                int killerId = buffer.GetInt();
                int criticalCode = buffer.GetInt();
                Console.WriteLine($"Player {killedId} was killed by {killerId} with critical code {criticalCode}");
                break;

            case 4: // Chat message received
                string message = buffer.GetString(); // Read the length of the message
                Console.WriteLine($"Chat message from Player: {message}");
                //protocol 2, argument 6
                ByteBuffer sendBuffer = new ByteBuffer();
                sendBuffer.Put((byte)2);
                sendBuffer.Put((byte)6);

                int playerId = 1;
                sendBuffer.Put(playerId);

                sendBuffer.Put(message);

                foreach (var client in clients)
                {
                    try
                    {
                        NetworkStream ns = client.GetStream();

                        if (ns.CanWrite)
                        {
                            ns.Write(sendBuffer.Trim().Get(), 0, sendBuffer.Trim().Get().Length);
                        }

                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("SE:" + se);
                    }
                }

                //Send(buffer.Trim().Get());
                //try
                //{
                //    NetworkStream ns = client.GetStream();

                //    if (ns.CanWrite)
                //    {
                //        ns.Write(data, 0, data.Length);
                //    }

                //}
                //catch (SocketException se)
                //{
                //    Debug.LogError("SE:" + se);
                //}

                break;

            case 5: // Player appearance change
                int appearancePlayerId = buffer.GetInt();
                int holo = buffer.GetInt();
                int head = buffer.GetInt();
                int face = buffer.GetInt();
                int gloves = buffer.GetInt();
                int upperBody = buffer.GetInt();
                int lowerBody = buffer.GetInt();
                int boots = buffer.GetInt();
                Console.WriteLine($"Player {appearancePlayerId} changed appearance: Holo {holo}, Head {head}, Face {face}, Gloves {gloves}, Upper Body {upperBody}, Lower Body {lowerBody}, Boots {boots}");
                break;

            //case 6: // Player left
            //    int leftPlayerId = buffer.GetInt();
            //    Console.WriteLine($"Player left: ID {leftPlayerId}");
            //    break;

            //case 7: // Change appearance
            //    int appearancePlayerId = buffer.GetInt();
            //    int holo = buffer.GetInt();
            //    int head = buffer.GetInt();
            //    int face = buffer.GetInt();
            //    int gloves = buffer.GetInt();
            //    int upperBody = buffer.GetInt();
            //    int lowerBody = buffer.GetInt();
            //    int boots = buffer.GetInt();
            //    Console.WriteLine($"Player {appearancePlayerId} changed appearance: Holo {holo}, Head {head}, Face {face}, Gloves {gloves}, Upper Body {upperBody}, Lower Body {lowerBody}, Boots {boots}");
            //    break;

            // Add more cases for additional actions as needed

            default:
                Console.WriteLine("Unknown action type.");
                break;

                //private void HandlePlayerJoined(int playerId, string playerName)
                //{
                //    Console.WriteLine($"Player joined: ID {playerId}, Name {playerName}");

                //    // Create a buffer to send to all clients
                //    ByteBuffer buffer = new ByteBuffer();
                //    buffer.Put((byte)2); // Protocol for player actions
                //    buffer.Put((byte)0); // Argument for player joined
                //    buffer.Put(playerId);
                //    buffer.Put(playerName);

                //    // Send the buffer to all connected clients
                //    foreach (var client in clients)
                //    {
                //        SendToClient(client, buffer);
                //    }
                //}

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
