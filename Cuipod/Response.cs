using System;
using System.IO;
using System.Text;

namespace Cuipod
{
    public class Response
    {
        public StatusCode Status  { get; internal set; }

        private string _directoryToServe;
        private string _requestBody = "";

        public Response(string directoryToServe)
        {
            _directoryToServe = directoryToServe;
            Status = StatusCode.Success;
        }

        public void RenderPlainTextLine(string text)
        {
            _requestBody += text + "\r\n";
        }

        public void RenderFileContent(string relativePathToFile)
        {
            _requestBody += File.ReadAllText(_directoryToServe + relativePathToFile, Encoding.UTF8) + "\r\n";
        }

        internal static string WriteHeader(StatusCode statusCode)
        {
            return ((int)statusCode).ToString() + " text/gemini\r\n";
        }

        internal byte[] Encode()
        {
            string wholeResponse = WriteHeader(Status) + _requestBody;
            return Encoding.UTF8.GetBytes(wholeResponse);
        }
    }
}
