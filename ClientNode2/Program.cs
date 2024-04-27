using System.IO.Pipes;
namespace ClientNode2
{
    public class Program
    {
        private const string PipeName = "MasterPipe";

        static async Task Main(string[] args)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
            {               
                try
                {
                    Console.WriteLine("Connecting to Master application...");
                    pipeClient.Connect();
                    Console.WriteLine("Connected to Master application.");
                    var reader = new StreamReader(pipeClient);

                    while (true && pipeClient.IsConnected)
                    {                        
                        string message = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(message))
                            Console.WriteLine($"Received message from Master: {message}");
                        else
                        {
                            continue;
                        }

                    }

                    reader.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message from Master: {ex.Message}");
                }
                
            }
        }
      
    }
}
