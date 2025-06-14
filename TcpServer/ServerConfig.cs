using System.IO;
using System.Text.Json;

public class ServerConfig
{
    public string ip { get; set; }
    public int port { get; set; }

    public static ServerConfig Load(string path)
    {
        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<ServerConfig>(json);
        if (config == null)
        {
            throw new Exception("Failed to load config.json!");
        }
        return config;
    }
}
