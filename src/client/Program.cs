using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Client
{
    
    class Program
    {
        private static ILogger<Program> _logger;
        
        static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<Program>();

            if(args.Length == 0)
            {
                _logger.LogWarning("Arguments not specified");
                ShowHelp();
                return;
            }

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddCommandLine(args);
            var config = configBuilder.Build();

            if(!IPAddress.TryParse(config.GetValue<string>("ip"), out IPAddress ipAddress))
                throw new ArgumentException($"Error: IP address is not specified. Use --ip \"{{ip_address}}\"");
            _logger.LogInformation($"IP: {ipAddress.ToString()}");

            if(!int.TryParse(config.GetValue<string>("port"), out int port))
                throw new ArgumentException($"Error: Port is not specified. Use --port {{port_number}}");
            _logger.LogInformation($"Port: {port}");

            var data = config.GetValue<string>("data");
            if(data == null)
                throw new ArgumentException($"Error: Data is not specified. Use --data \"{{data}}\"");
            _logger.LogInformation($"Data: {data}");

            var ssl = config.GetValue<bool>("ssl", false);
            _logger.LogInformation($"Use ssl: {ssl}");

            var sslHost = config.GetValue<string>("ssl-host");
            if(ssl && string.IsNullOrWhiteSpace(sslHost))
                throw new ArgumentException("Error: SSL enabled, but host is not specified. Use: --ssl-host {{\"ssl_host\"}}");
            _logger.LogInformation($"Use ssl: {ssl}");

            using(TcpClient client = new TcpClient())
            {
                _logger.LogInformation("Client created");
                client.Connect(ipAddress, port);
                _logger.LogInformation("Connected to the remote server");
                using(var stream = client.GetStream())
                {                   
                    _logger.LogInformation("Stream is ready");
                    if(ssl)
                    {
                        using(var sslStream = new SslStream(stream, true, CertificateValidationCallback))
                        {
                            sslStream.AuthenticateAsClient("google.com.ua");
                            Communicate(sslStream, data);
                        }
                    }
                    else
                    {
                        Communicate(stream, data);
                    }
                }
                client.Close();
            }
        }

        private static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static void Communicate(Stream stream, string data)
        {
                    using(StreamWriter output = new StreamWriter(stream, leaveOpen: true))
                    {
                        output.WriteLine(data);
                        output.WriteLine();
                    }
                    _logger.LogInformation("Data sent");

                    using(StreamReader input = new StreamReader(stream, leaveOpen: true))
                    {
                        Console.WriteLine(input.ReadToEnd());
                    }
                    _logger.LogInformation("Data recived");
        }

        private static void ShowHelp()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("Utility to connect to the remote server.");
            output.AppendLine("Usage:");
            output.AppendLine("\t dotnet run");
            output.AppendLine("\t--ip \"{ip_address}\"");
            output.AppendLine("\t--port {port_number}");
            output.AppendLine("\t--data \"{data_to_send}\"");
            output.AppendLine("\t[--ssl true|false]");
            output.AppendLine("\t[--ssl-host \"{ssl_host}\"]");
            

            Console.WriteLine(output.ToString());
        }
    }
}
