using System;
using System.IO;
using System.Text;

namespace Cuipod
{
    public class Response
    {
        public StatusCode Status  { get; set; }

        private readonly string _directoryToServe;
        private string _requestBody = "";

        internal Response(string directoryToServe)
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

        public void SetRedirectURL(string route)
        {
            _requestBody = route + "\r\n";
        }

        public void SetInputHint(string hint)
        {
            _requestBody = hint + "\r\n";
        }

        internal byte[] Encode()
        {
            string wholeResponse = ((int)Status).ToString() + " ";
            if (Status == StatusCode.Success)
            {
                wholeResponse += "text/gemini\r\n";
            }

            wholeResponse += _requestBody;
            return Encoding.UTF8.GetBytes(wholeResponse);
        }
    }
}
