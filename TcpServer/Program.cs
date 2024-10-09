using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class serv
{
    public static void Main()
    {
        try
        {
            IPAddress ipAd = IPAddress.Parse("192.168.31.5");
            // use local m/c IP address, and 
            // use the same in the client

            /* Initializes the Listener */
            TcpListener listener = new TcpListener(ipAd, 8001);

            /* Start Listeneting at the specified port */
            listener.Start();
            Console.WriteLine("The server is running at port 8001...");
            Console.WriteLine("The local End point is  :" +
            listener.LocalEndpoint);
            Console.WriteLine("Waiting for a connection.....");

            while (true)
            {
                // Accept an incoming client connection
                using (TcpClient client = listener.AcceptTcpClient())
                {
                    Console.WriteLine("Client connected.");

                    // Get the network stream
                    using (NetworkStream ns = client.GetStream())
                    {
                        // Buffer to hold the incoming data
                        byte[] buffer = new byte[1024];
                        int bytesRead = ns.Read(buffer, 0, buffer.Length);

                        // Convert the received data to a string
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("Received message: " + message);
                    }
                }
            }

        }


        //    while (true)
        //    {
        //        TcpClient client = await listener.AcceptTcpClientAsync();
        //        Console.WriteLine("Client connected.");
        //        NetworkStream stream = client.GetStream();
        //        byte[] buffer = new byte[8192];
        //        int bytesRead;
        //        try
        //        {
        //            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        //            {
        //                byte[] receivedData = new byte[bytesRead];
        //                Array.Copy(buffer, 0, receivedData, 0, bytesRead);

        //                string receivedText = Encoding.UTF8.GetString(receivedData);
        //                Console.WriteLine($"Received: {receivedText}");

        //                // Echo the data back to the client
        //                //await stream.WriteAsync(receivedData, 0, receivedData.Length);
        //            }
        //        }

        //    }
        //}









        catch (Exception e)
        {
            Console.WriteLine("Error..... " + e.StackTrace);
        }
    }

}
