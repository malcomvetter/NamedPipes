using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            using (var pipe = new NamedPipeServerStream(
                "psexecsvc", 
                PipeDirection.InOut, 
                NamedPipeServerStream.MaxAllowedServerInstances, 
                PipeTransmissionMode.Message))
            {
                Console.WriteLine("[*] Waiting for client connection...");
                pipe.WaitForConnection();
                Console.WriteLine("[*] Client connected.");
                while (true)
                {
                    var messageBytes = ReadMessage(pipe);
                    var line = Encoding.UTF8.GetString(messageBytes);
                    Console.WriteLine("[*] Received: {0}", line);
                    if (line.ToLower() == "exit") return;

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c " + line,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };
                    try
                    {
                        var process = Process.Start(processStartInfo);
                        var output = process.StandardOutput.ReadToEnd();
                        output += process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if (string.IsNullOrEmpty(output))
                        {
                            output = "\n";
                        }
                        var response = Encoding.UTF8.GetBytes(output);
                        pipe.Write(response, 0, response.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        var response = Encoding.UTF8.GetBytes(ex.Message);
                        pipe.Write(response, 0, response.Length);
                    }
                }
            }
        }

        private static byte[] ReadMessage(PipeStream pipe)
        {
            byte[] buffer = new byte[1024];
            using (var ms = new MemoryStream())
            {
                do
                {
                    var readBytes = pipe.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, readBytes);
                }
                while (!pipe.IsMessageComplete);

                return ms.ToArray();
            }
        }
    }
}
