using System.Net;
using System.Net.Sockets;

class TcpServer
{
    private TcpListener listener;
    private bool isRunning;
    private List<TcpClient> clients = new List<TcpClient>();
    private List<PlayerData> playerDatas = new List<PlayerData>();
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

            Thread clientThread = new Thread(() => HandleClient(client, clients.Count));
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client, int id)
    {
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            try
            {
                InitiateHandshake(stream, id);

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ProcessPacket(buffer, bytesRead, stream, id);
                }
            }
            finally
            {
                client.Close();
                clients.Remove(client);
                playerDatas.RemoveAll(player => player.id == id);
                Console.WriteLine("Current amount of player datas: " + playerDatas.Count);
                Console.WriteLine("Client disconnected.");
            }
        }
    }

    private void InitiateHandshake(NetworkStream stream, int id)
    {
        ByteBuffer handshakeRequest = new ByteBuffer();
        handshakeRequest.Put(SERIAL_X);
        handshakeRequest.Put(SERIAL_Y);

        handshakeRequest.Put(id);

        
        int playerCount = playerDatas.Count;
        handshakeRequest.Put(playerCount);

        foreach (var playerData in playerDatas)
        {
            //id
            int playerId = playerData.id;
            handshakeRequest.Put(playerId);

            //name
            string name = playerData.name;
            handshakeRequest.Put(name);

            //appearance
            handshakeRequest.Put(playerData.holo);
            handshakeRequest.Put(playerData.head);
            handshakeRequest.Put(playerData.face);
            handshakeRequest.Put(playerData.gloves);
            handshakeRequest.Put(playerData.upperbody);
            handshakeRequest.Put(playerData.lowerbody);
            handshakeRequest.Put(playerData.boots);

            //player statistics
            handshakeRequest.Put(playerData.kills);
            handshakeRequest.Put(playerData.deaths);
        }

        stream.Write(handshakeRequest.Trim().Get(), 0, handshakeRequest.Trim().Get().Length);
        Console.WriteLine("Handshake initiated with client.");
    }

    private void ProcessPacket(byte[] data, int length, NetworkStream stream, int id)
    {
        ByteBuffer buffer = new ByteBuffer(data);

        byte protocol = buffer.GetByte();

        if (protocol == 0) // Handshake response
        {
            HandleHandshake(buffer, stream, id);
        }
        else if (protocol == 1) // Player position update
        {
            UpdatePlayerPositions(buffer);
        }
        else if (protocol == 2) // Various game actions
        {
            HandleGameActions(buffer, id);
        }
    }

    private void HandleHandshake(ByteBuffer buffer, NetworkStream stream, int id)
    {
        if (buffer.GetInt() == SERIAL_Y && buffer.GetInt() == SERIAL_X)
        {
            string playerName = buffer.GetString();
            PlayerData newPlayer = new PlayerData(playerName);
            newPlayer.id = id;
            playerDatas.Add(newPlayer);

            ByteBuffer response = new ByteBuffer();
            response.Put((byte)2); // protocol type
            response.Put((byte)0); // player Joined
            response.Put(id);
            response.Put(playerName);
            SendToOtherClients(response.Trim().Get(), id); // send to all except self(player id)

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

    private void HandleGameActions(ByteBuffer buffer, int id)
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
                int killerId = buffer.GetInt();
                int criticalCode = buffer.GetInt();
                Console.WriteLine($"Player was killed by {killerId} with critical code {criticalCode}");
                break;

            case 4: // Chat message received
                string message = buffer.GetString(); // Read the length of the message
                Console.WriteLine($"Chat message from Player: {message}");
                //protocol 2, argument 6
                ByteBuffer sendBuffer = new ByteBuffer();
                sendBuffer.Put((byte)2);
                sendBuffer.Put((byte)6);

                sendBuffer.Put(id);

                sendBuffer.Put(message);

                SendToAllClients(sendBuffer.Trim().Get());

                break;

            case 5: // Player appearance change
                playerDatas[id - 1].holo = buffer.GetInt();
                playerDatas[id - 1].head = buffer.GetInt();
                playerDatas[id - 1].face = buffer.GetInt();
                playerDatas[id - 1].gloves = buffer.GetInt();
                playerDatas[id - 1].upperbody = buffer.GetInt();
                playerDatas[id - 1].lowerbody = buffer.GetInt();
                playerDatas[id - 1].boots = buffer.GetInt();

                ByteBuffer appearanceSendBuffer = new ByteBuffer();
                appearanceSendBuffer.Put((byte)2);
                appearanceSendBuffer.Put((byte)7);
                appearanceSendBuffer.Put(id);

                appearanceSendBuffer.Put(playerDatas[id - 1].holo);
                appearanceSendBuffer.Put(playerDatas[id - 1].head);
                appearanceSendBuffer.Put(playerDatas[id - 1].face);
                appearanceSendBuffer.Put(playerDatas[id - 1].gloves);
                appearanceSendBuffer.Put(playerDatas[id - 1].upperbody);
                appearanceSendBuffer.Put(playerDatas[id - 1].lowerbody);
                appearanceSendBuffer.Put(playerDatas[id - 1].boots);

                SendToAllClients(appearanceSendBuffer.Trim().Get());

                Console.WriteLine($"Player changed appearance: Holo {playerDatas[id - 1].holo}, Head {playerDatas[id - 1].head}, Face {playerDatas[id - 1].face}, Gloves {playerDatas[id - 1].gloves}, Upper Body {playerDatas[id - 1].upperbody}, Lower Body {playerDatas[id - 1].lowerbody}, Boots {playerDatas[id - 1].boots}");
                break;

            default:
                Console.WriteLine("Unknown action type.");
                break;
        }
    }

    private void SendToAllClients(byte[] data)
    {
        foreach (var client in clients)
        {
            try
            {
                NetworkStream ns = client.GetStream();

                if (ns.CanWrite)
                {
                    ns.Write(data, 0, data.Length);
                }

            }
            catch (SocketException se)
            {
                Console.WriteLine("SE:" + se);
            }
        }
    }

    private void SendToOtherClients(byte[] data, int allExceptId)
    {
        for (int i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            if (i == allExceptId-1)
            {
                continue;
            }

            try
            {
                NetworkStream ns = client.GetStream();

                if (ns.CanWrite)
                {
                    ns.Write(data, 0, data.Length);
                }

            }
            catch (SocketException se)
            {
                Console.WriteLine("SE:" + se);
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

        Console.WriteLine($"[Config] Starting server on {config.ip}:{config.port}");

        TcpServer server = new TcpServer(config.ip, config.port);
        server.Start();
    }
}
