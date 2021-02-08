using System;
using System.Collections.Generic;
using System.Text;

namespace Cuipod
{
    public class Request
    {
        public string BaseURL { get; internal set; }
        public string Route { get; internal set; }
        public string Parameters { get; internal set; }

        public Request(string baseURL, string route)
        {
            BaseURL = baseURL;

            int parametersDelimiter = route.IndexOf("?");
            if (parametersDelimiter != -1)
            {
                Parameters = route.Substring(parametersDelimiter + 1);
                Route = route.Substring(0, parametersDelimiter);
            } 
            else
            {
                Route = route;
            }
        }
    }
}
