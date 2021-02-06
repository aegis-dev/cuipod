using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Security.Authentication;
using System.IO;

namespace Cuipod
{
    public class App
    {
        private readonly string _directoryToServe;
        private readonly TcpListener _listener;
        private readonly X509Certificate2 _serverCertificate;
        private readonly Dictionary<string, Action<Response>> _requestCallbacks;

        private Action<Response> _onBadRequestCallback;

        public App(string directoryToServe, string certificateFile, string privateRSAKeyFilePath)
        {
            _directoryToServe = directoryToServe;
            IPAddress localAddress = IPAddress.Parse("127.0.0.1");
            _listener = new TcpListener(localAddress, 1965);
            _requestCallbacks = new Dictionary<string, Action<Response>>();
            _serverCertificate = CertificateUtils.LoadCertificate(certificateFile, privateRSAKeyFilePath);
        }

        public void OnRequest(string route, Action<Response> callback)
        {
            _requestCallbacks.Add(route, callback);
        }

        public void OnBadRequest(Action<Response> callback)
        {
            _onBadRequestCallback = callback;
        }

        public void Run()
        {
            Console.WriteLine("Serving capsule on 127.0.0.1:1965");
            try
            {
                _listener.Start();
                while (true)
                {
                    ProcessRequest(_listener.AcceptTcpClient());
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                _listener.Stop();
            }
        }

        private void ProcessRequest(TcpClient client)
        {
            SslStream sslStream = new SslStream(client.GetStream(), false);
            try
            {
                sslStream.AuthenticateAsServer(_serverCertificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false);

                // Read a message from the client.
                string rawURI = ReadRequest(sslStream);

                Response response = new Response(_directoryToServe);

                int protocolDelimiter = rawURI.IndexOf("://");
                if (protocolDelimiter == -1)
                {
                    response.Status = StatusCode.BadRequest;
                }

                string protocol = rawURI.Substring(0, protocolDelimiter);
                if (protocol != "gemini")
                {
                    response.Status = StatusCode.BadRequest;
                }

                string url = rawURI.Substring(protocolDelimiter + 3);
                int domainNameDelimiter = url.IndexOf("/");
                if (domainNameDelimiter == -1)
                {
                    response.Status = StatusCode.BadRequest;
                }
                string domainName = url.Substring(0, domainNameDelimiter);
                // TODO: validate domain name from cert?

                string route = url.Substring(domainNameDelimiter);

                if (response.Status == StatusCode.Success)
                {
                    Action<Response> callback = null;
                    _requestCallbacks.TryGetValue(route, out callback);
                    if (callback != null)
                    {
                        callback(response);
                    } else if (_onBadRequestCallback != null)
                    {
                        _onBadRequestCallback(response);
                    } else
                    {
                        response.Status = StatusCode.BadRequest;
                    }
                } 
           
                sslStream.Write(response.Encode());
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
            }
            catch (IOException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            finally
            {
                sslStream.Close();
                client.Close();
            }
        }

        private string ReadRequest(SslStream sslStream)
        {
            byte[] buffer = new byte[2048];
            Decoder decoder = Encoding.UTF8.GetDecoder();

            StringBuilder requestData = new StringBuilder();
            string line;
            do
            {
                int bytes = sslStream.Read(buffer, 0, buffer.Length);
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                line = new string(chars);
                requestData.Append(line);
            } while (!line.EndsWith("\r\n"));

           
            return requestData.ToString().TrimEnd('\r', '\n'); ;
        }
    }
}
