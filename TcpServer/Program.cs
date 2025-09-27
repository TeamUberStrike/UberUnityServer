using System.Net;
using System.Net.Sockets;

class TcpServer
{
    private TcpListener listener;
    private bool isRunning;
    private Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
    private Dictionary<int, PlayerData> playerDatas = new Dictionary<int, PlayerData>();
    private int nextPlayerId = 1; // stable, unique IDs
    private const int SERIAL_X = 12345;
    private const int SERIAL_Y = 67890;

    public TcpServer(int port)
    {
        listener = new TcpListener(IPAddress.IPv6Any, port);
        listener.Server.DualMode = true; // Support both IPv4 and IPv6
        isRunning = true;
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("Server started, waiting for connections...");

        while (isRunning)
        {
            TcpClient client = listener.AcceptTcpClient();
            int playerId = nextPlayerId++;
            Console.WriteLine($"Client connected with id: \"{playerId}\"");

            lock (clients)
            {
                clients[playerId] = client;
            }

            Thread clientThread = new Thread(() => HandleClient(client, playerId));
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client, int id)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                InitiateHandshake(stream, id);

                byte[] buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ProcessPacket(buffer, id);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Client: \"{id}\" exception: \"{ex.Message}\"");
        }
        finally
        {
            DisconnectClient(id);
        }
    }

    private void DisconnectClient(int id)
    {
        lock (clients)
        {
            if (clients.ContainsKey(id))
            {
                clients[id].Close();
                clients.Remove(id);
            }
        }

        lock (playerDatas)
        {
            if (playerDatas.ContainsKey(id))
            {
                playerDatas.Remove(id);
            }
        }

        Console.WriteLine($"Client \"{id}\" disconnected. Current count of players: \"{playerDatas.Count}\"");

        // Broadcast leave message
        ByteBuffer response = new ByteBuffer();
        response.Put((byte)2); // protocol
        response.Put((byte)1); // player left
        response.Put(id);
        SendToAllClients(response.Trim().Get());
    }

    private void InitiateHandshake(NetworkStream stream, int id)
    {
        ByteBuffer handshakeRequest = new ByteBuffer();
        handshakeRequest.Put(SERIAL_X);
        handshakeRequest.Put(SERIAL_Y);
        handshakeRequest.Put(id);

        lock (playerDatas)
        {
            handshakeRequest.Put(playerDatas.Count);
            foreach (var playerData in playerDatas.Values)
            {
                handshakeRequest.Put(playerData.id);
                handshakeRequest.Put(playerData.name);
                handshakeRequest.Put(playerData.holo);
                handshakeRequest.Put(playerData.head);
                handshakeRequest.Put(playerData.face);
                handshakeRequest.Put(playerData.gloves);
                handshakeRequest.Put(playerData.upperbody);
                handshakeRequest.Put(playerData.lowerbody);
                handshakeRequest.Put(playerData.boots);
                handshakeRequest.Put(playerData.kills);
                handshakeRequest.Put(playerData.deaths);
            }
        }

        stream.Write(handshakeRequest.Trim().Get());
        Console.WriteLine($"Handshake initiated with client \"{id}\".");
    }

    private void ProcessPacket(byte[] data, int id)
    {
        ByteBuffer buffer = new ByteBuffer(data);

        byte protocol = buffer.GetByte();

        if (protocol == 0) // Handshake response
        {
            HandleHandshake(buffer, id);
        }
        else if (protocol == 1) // Player position update
        {
            UpdatePlayerPositions(buffer, id);
        }
        else if (protocol == 2) // Game actions
        {
            HandleGameActions(buffer, id);
        }
    }

    private void HandleHandshake(ByteBuffer buffer, int id)
    {
        if (buffer.GetInt() == SERIAL_Y && buffer.GetInt() == SERIAL_X)
        {
            string playerName = buffer.GetString();
            PlayerData newPlayer = new PlayerData(playerName) { id = id };

            lock (playerDatas)
            {
                playerDatas[id] = newPlayer;
            }

            ByteBuffer response = new ByteBuffer();
            response.Put((byte)2); // protocol
            response.Put((byte)0); // player joined
            response.Put(id);
            response.Put(playerName);

            SendToOtherClients(response.Trim().Get(), id);

            Console.WriteLine($"Handshake completed for player \"{id}\": \"{playerName}\"");
        }
    }

    private void UpdatePlayerPositions(ByteBuffer buffer, int id)
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

        lock (playerDatas)
        {
            if (playerDatas.TryGetValue(id, out var player))
            {
                player.x = x; player.y = y; player.z = z;
                player.xr = xr; player.yr = yr; player.zr = zr;
                player.xc = xc; player.yc = yc; player.zc = zc;
            }
        }

        ByteBuffer response = new ByteBuffer();
        response.Put((byte)1);
        lock (playerDatas)
        {
            response.Put(playerDatas.Count);
            foreach (var player in playerDatas.Values)
            {
                response.Put(player.id);
                response.Put(player.x); response.Put(player.y); response.Put(player.z);
                response.Put(player.xr); response.Put(player.yr); response.Put(player.zr);
                response.Put(player.xc); response.Put(player.yc); response.Put(player.zc);
            }
        }

        SendToAllClients(response.Trim().Get());
    }

    private void HandleGameActions(ByteBuffer buffer, int id)
    {
        int actionType = buffer.GetInt();

        switch (actionType)
        {
            case 0: // Change weapon
                int weaponId = buffer.GetInt();
                Console.WriteLine($"Player \"{id}\" changed weapon to \"{weaponId}\"");
                break;

            case 1: // Fire weapon
                Console.WriteLine($"Player \"{id}\" fired their weapon.");
                break;

            case 2: // Damage dealt to another player
                int receiverId = buffer.GetInt();
                float damageAmount = buffer.GetFloat();
                int damageCriticalCode = buffer.GetInt();
                float posX = buffer.GetFloat();
                float posY = buffer.GetFloat();
                float posZ = buffer.GetFloat();
                Console.WriteLine($"Player \"{id}\" dealt \"{damageAmount}\" damage to Player \"{receiverId}\" (critical code \"{damageCriticalCode}\") at (\"{posX}\", \"{posY}\", \"{posZ}\")");
                break;

            case 3: // Player died
                int killerId = buffer.GetInt();
                int criticalCode = buffer.GetInt();
                Console.WriteLine($"Player \"{id}\" was killed by Player \"{killerId}\" with critical code \"{criticalCode}\"");
                break;

            case 4: // Chat message
                string message = buffer.GetString();
                Console.WriteLine($"Chat message from Player \"{id}\": \"{message}\"");

                ByteBuffer sendBuffer = new ByteBuffer();
                sendBuffer.Put((byte)2);
                sendBuffer.Put((byte)6);
                sendBuffer.Put(id);
                sendBuffer.Put(message);

                SendToAllClients(sendBuffer.Trim().Get());
                break;

            case 5: // Appearance change
                if (playerDatas.TryGetValue(id, out var player))
                {
                    player.holo = buffer.GetInt();
                    player.head = buffer.GetInt();
                    player.face = buffer.GetInt();
                    player.gloves = buffer.GetInt();
                    player.upperbody = buffer.GetInt();
                    player.lowerbody = buffer.GetInt();
                    player.boots = buffer.GetInt();

                    ByteBuffer appearanceSendBuffer = new ByteBuffer();
                    appearanceSendBuffer.Put((byte)2);
                    appearanceSendBuffer.Put((byte)7);
                    appearanceSendBuffer.Put(id);
                    appearanceSendBuffer.Put(player.holo);
                    appearanceSendBuffer.Put(player.head);
                    appearanceSendBuffer.Put(player.face);
                    appearanceSendBuffer.Put(player.gloves);
                    appearanceSendBuffer.Put(player.upperbody);
                    appearanceSendBuffer.Put(player.lowerbody);
                    appearanceSendBuffer.Put(player.boots);

                    SendToAllClients(appearanceSendBuffer.Trim().Get());

                    Console.WriteLine($"Player \"{id}\" changed appearance: Holo \"{player.holo}\", Head \"{player.head}\", Face \"{player.face}\", Gloves \"{player.gloves}\", Upper \"{player.upperbody}\", Lower \"{player.lowerbody}\", Boots \"{player.boots}\"");
                }
                break;

            default:
                Console.WriteLine($"Unknown action type from player \"{id}\"");
                break;
        }
    }

    private void SendToAllClients(byte[] data)
    {
        lock (clients)
        {
            foreach (var client in clients.Values.ToList())
            {
                try
                {
                    NetworkStream ns = client.GetStream();
                    if (ns.CanWrite)
                        ns.Write(data, 0, data.Length);
                }
                catch { }
            }
        }
    }

    private void SendToOtherClients(byte[] data, int exceptId)
    {
        lock (clients)
        {
            foreach (var client in clients)
            {
                if (client.Key == exceptId) continue;
                try
                {
                    NetworkStream ns = client.Value.GetStream();
                    if (ns.CanWrite)
                        ns.Write(data, 0, data.Length);
                }
                catch { }
            }
        }
    }

    public void Stop()
    {
        isRunning = false;
        listener.Stop();
    }

    static void Main(string[] args)
    {
        ServerConfig config = ServerConfig.Load("config.json");

        Console.WriteLine($"[Config] Starting server on port \"{config.port}\"");

        TcpServer server = new TcpServer(config.port);
        server.Start();
    }
}
