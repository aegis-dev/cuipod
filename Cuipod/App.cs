using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Security.Authentication;
using System.IO;

using RequestCallback = System.Action<Cuipod.Request, Cuipod.Response>;

namespace Cuipod
{
    public class App
    {
        private readonly string _directoryToServe;
        private readonly TcpListener _listener;
        private readonly X509Certificate2 _serverCertificate;
        private readonly Dictionary<string, RequestCallback> _requestCallbacks;

        private RequestCallback _onBadRequestCallback;

        //somewhat flaky implementation - probably deprecate it
        public App(string directoryToServe, string certificateFile, string privateRSAKeyFilePath)
        {
            _directoryToServe = directoryToServe;
            _listener = new TcpListener(IPAddress.Any, 1965);
            _requestCallbacks = new Dictionary<string, RequestCallback>();
            _serverCertificate = CertificateUtils.LoadCertificate(certificateFile, privateRSAKeyFilePath);
        }

        public App(string directoryToServe, X509Certificate2 certificate)
        {

            _directoryToServe = directoryToServe;
            _listener = new TcpListener(IPAddress.Any, 1965);
            _requestCallbacks = new Dictionary<string, RequestCallback>();
            _serverCertificate = certificate;
        }

        public void OnRequest(string route, RequestCallback callback)
        {
            _requestCallbacks.Add(route, callback);
        }

        public void OnBadRequest(RequestCallback callback)
        {
            _onBadRequestCallback = callback;
        }

        public int Run()
        {
            int status = 0;
            Console.WriteLine("Serving capsule on 0.0.0.0:1965");
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
                status = 1;
            }
            finally
            {
                _listener.Stop();
            }

            return status;
        }

        private void ProcessRequest(TcpClient client)
        {
            SslStream sslStream = null;

            try
            {
                sslStream = new SslStream(client.GetStream(), false);
                Response response = ProcessRequest(sslStream);
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

        private Response ProcessRequest(SslStream sslStream)
        {
            sslStream.ReadTimeout = 5000;
            sslStream.AuthenticateAsServer(_serverCertificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);

            // Read a message from the client.
            string rawURL = ReadRequest(sslStream);

            Response response = new Response(_directoryToServe);

            if (rawURL == null)
            {
                response.Status = StatusCode.BadRequest;
                return response;
            }

            Console.WriteLine(rawURL);

            int protocolDelimiter = rawURL.IndexOf("://");
            if (protocolDelimiter == -1)
            {
                response.Status = StatusCode.BadRequest;
                return response;
            }

            string protocol = rawURL.Substring(0, protocolDelimiter);
            if (protocol != "gemini")
            {
                response.Status = StatusCode.BadRequest;
                return response;
            }

            string url = rawURL.Substring(protocolDelimiter + 3);
            int domainNameDelimiter = url.IndexOf("/");
            if (domainNameDelimiter == -1)
            {
                response.Status = StatusCode.BadRequest;
                return response;
            }
            string domainName = url.Substring(0, domainNameDelimiter);

            Request request = new Request("gemini://" + domainName , url.Substring(domainNameDelimiter));
            if (response.Status == StatusCode.Success)
            {
                RequestCallback callback;
                _requestCallbacks.TryGetValue(request.Route, out callback);
                if (callback != null)
                {
                    callback(request, response);
                } 
                else if (_onBadRequestCallback != null)
                {
                    _onBadRequestCallback(request, response);
                } 
                else
                {
                    response.Status = StatusCode.BadRequest;
                    return response;
                }
            }

            return response;
        }

        private string ReadRequest(SslStream sslStream)
        {
            byte[] buffer = new byte[2048];
            Decoder decoder = Encoding.UTF8.GetDecoder();

            StringBuilder requestData = new StringBuilder();
            int bytes = sslStream.Read(buffer, 0, buffer.Length);
            char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
            decoder.GetChars(buffer, 0, bytes, chars, 0);
            string line = new string(chars);
            if (line.EndsWith("\r\n"))
            {
                return line.TrimEnd('\r', '\n');
            }
            return null;
        }
    }
}
