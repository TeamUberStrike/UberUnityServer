started Server, went to Unity and clicked join

in the Refresh method, it says: 
    internal void Refresh()
    {
        while (client != null)
        {
            Byte[] bytes = new Byte[8192];

            using (NetworkStream stream = client.GetStream())
            {
                int length;

                while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
				
following error:				

System.IO.IOException: Read failure ---> System.Net.Sockets.SocketException: An existing connection was forcibly closed by the remote host.

it says connection accepted and then it immediately stopped the connection.
It makes total sense as the server needs to be kept alive and is supposed to accept requests and forward it to all players

on the server there should be a stream handler that sends all the streams to players
ns.Write(stream) by player
server receives a stream, (puts it in a queue?)  
when a client asks for the stream, it must be available, ns.Read(data by stream)

keep TCP alive:
https://stackoverflow.com/questions/18488562/tcp-connection-keep-alive

network streams are bytes, no built in way to detect end of bytes:

https://stackoverflow.com/questions/74655289/how-to-read-and-write-data-in-tcp-socket-client-server