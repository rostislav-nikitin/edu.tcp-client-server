using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Server
{
    class Program
    {
        private static ILogger<Program> _logger;
        static void Main(string[] args)
        {
            const string IPDefault = "127.0.0.1";
            const string PortDefault = "7777";

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<Program>();

            if(args.Length == 0)
            {
                _logger.LogWarning("Arguments not specified");
                ShowHelp();
                return;
            }

            ConfigurationBuilder cfgBuilder = new ConfigurationBuilder();
            cfgBuilder.AddCommandLine(args);
            IConfiguration cfg = cfgBuilder.Build();

            if(!IPAddress.TryParse(cfg.GetValue<string>("ip", IPDefault), out IPAddress ipAddress))
                throw new ArgumentException($"Error: IP address is not specified. Use --ip \"{{ip_address}}\"");

            _logger.LogInformation($"IP: {ipAddress.ToString()}");
            
            if(!int.TryParse(cfg.GetValue<string>("port", PortDefault), out int port))
                throw new ArgumentException($"Error: Port is not specified. Use --port {{port_number}}");

            _logger.LogInformation($"Port: {port}");

            var ssl = cfg.GetValue<bool>("ssl", false);
            _logger.LogInformation($"Use ssl: {ssl}");

            X509Certificate2 certificate = null;
            if(ssl)
            {
                certificate = GetCertificate();
                if(certificate == null)
                    throw new ArgumentException("Can not find any valid certificate.");

            	_logger.LogInformation($"Certificate: {certificate.FriendlyName}({certificate.Thumbprint})");
            }


            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();
            while(true)
            {
                var client = listener.AcceptTcpClient();
                _logger.LogInformation("TcpClietn accepted");
                var stream = client.GetStream();
                _logger.LogInformation("Stream is ready");

                if(ssl)
                {
                    using(var sslStream = new SslStream(stream, false, CertificateValidationCallback))
                    {
                        sslStream.AuthenticateAsServer(certificate);
                        Communicate(sslStream);
                    }
                }
                else
                {
                    Communicate(stream);
                }

                stream.Close();
            }

        }

        private static X509Certificate2 GetCertificate()
        {
            X509Store store = new X509Store("MY",StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = store.Certificates;
            var found = collection.Find(X509FindType.FindByTimeValid, DateTime.UtcNow.Date, false);
            foreach(var cert in found)
            {
                return cert;
            }

            return null;
        }

        private static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static void Communicate(Stream stream)
        {
            // In
            using(StreamReader reader = new StreamReader(stream, leaveOpen: true))
            {
                string input;
                do
                {
                    input = reader.ReadLine();
                    _logger.LogInformation($"From client: {input}");
                }
                while(!string.IsNullOrWhiteSpace(input));
            }
            // Out
            using(StreamWriter outputWriter = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                using (var responseHeaderReader = File.OpenText("response_header.txt"))
                using (var responseContentReader = File.OpenText("response_content.txt"))
                {
                    var responseHeaderTemplate = responseHeaderReader.ReadToEnd();
                    var responseContent = responseContentReader.ReadToEnd();

                    var responseHeader = string.Format(responseHeaderTemplate, responseContent.Length);

                    outputWriter.WriteLine(responseHeader);
                    outputWriter.Write(responseContent);
                }

            }
        }

        private static void ShowHelp()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("Utility to connect to the remote server.");
            output.AppendLine("Usage:");
            output.AppendLine("\t dotnet run");
            output.AppendLine("\t--ip \"{ip_address}\"");
            output.AppendLine("\t--port {port_number}");
            output.AppendLine("\t[--ssl true|false]");

            Console.WriteLine(output.ToString());
        }
    }
}
