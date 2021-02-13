# cuipod
Simple yet flexible framework for Gemini protocol servers

Framework is written in C# and based on .NET 5.0 framework. 
The project is still in very early stage so bugs are expected. Feel free to raise an issue ticket or even raise PR!

## Example 

```csharp
using Cuipod;

namespace CuipodExample
{
    class Server
    {
        static int Main(string[] args)
        {
            App app = new App(
                "<directory_to_serve>/",            // directory to serve 
                "<dir_with_cert>/certificate.crt",  // path to certificate
                "<dir_with_cert>/privatekey.key"    // path to private Pkcs8 RSA key
            );

            // Serve files
            app.OnRequest("/", (request, response) => {
                response.RenderFileContent("index.gmi");
            });

            // Input example
            app.OnRequest("/input", (request, response) => {
                if (request.Parameters == null)
                {
                    response.SetInputHint("Please enter something: ");
                    response.Status = StatusCode.Input;
                }
                else
                {
                    // redirect to show/ route with input parameters
                    response.SetRedirectURL(request.BaseURL + "/show?" + request.Parameters);
                    response.Status = StatusCode.RedirectTemp;
                }
            });

            app.OnRequest("/show", (request, response) => {
                if (request.Parameters == null)
                {
                    // redirect to input
                    response.SetRedirectURL(request.BaseURL + "/input");
                    response.Status = StatusCode.RedirectTemp;
                }
                else
                {
                    // show what has been entered
                    response.RenderPlainTextLine("# " + request.Parameters);
                }
            });

            // Or dynamically render content
            app.OnRequest("/dynamic/content", (request, response) => {
                response.RenderPlainTextLine("# woah much content!");
                response.RenderPlainTextLine("More utilities to render content will come soon!");
            });

            // Optional but nice. In case it is specified and client will do a bad route 
            // request we will respond with Success status and render result from this lambda
            app.OnBadRequest((request, response) => {
                response.RenderPlainTextLine("# Ohh No!!! Request is bad :(");
            });

            return app.Run();
        }
    }
}
```
