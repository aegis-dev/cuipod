namespace Cuipod
{
    public class Request
    {
        public string BaseURL { get; internal set; }
        public string Route { get; internal set; }
        public string Parameters { get; internal set; }

        internal Request(string baseURL, string route, string parameters)
        {
            BaseURL = baseURL;
            Route = route;
            Parameters = parameters;
        }
    }
}
