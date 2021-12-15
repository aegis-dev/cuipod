# cuipod
Simple yet flexible framework for Gemini protocol servers written in C# (.NET 5.0)

## Example 
For testing purposes you can generate certificate with this command
```
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout privatekey.key -out certificate.crt
```

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Cuipod;

namespace CuipodExample
{
    class Server
    {
        static int Main(string[] args)
        {
            X509Certificate2 cert = CertificateUtils.LoadCertificate(
                "<dir_with_cert>/certificate.crt",  // Path to certificate
                "<dir_with_cert>/privatekey.key"    // Path to private Pkcs8 RSA key
            );

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                builder
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                })
                .SetMinimumLevel(LogLevel.Debug)
            );
            ILogger<App> logger = loggerFactory.CreateLogger<App>();

            App app = new App(
                "pages/", // Directory to serve
                certificate,
                logger
            );

            // Serve files
            app.OnRequest("/", (request, response, logger) => {
                response.RenderFileContent("index.gmi");
            });

            // Input example
            app.OnRequest("/input", (request, response, logger) => {
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

            app.OnRequest("/show", (request, response, logger) => {
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
            app.OnRequest("/dynamic/content", (request, response, logger) => {
                response.RenderPlainTextLine("# woah much dynamic content!");
            });

            // Optional but nice. In case it is specified and client will do a bad route 
            // request we will respond with Success status and render result from this lambda
            app.OnBadRequest((request, response, logger) => {
                response.RenderPlainTextLine("# Ohh No!!! Request is bad :(");
            });

            app.Run();

            return 0;
        }
    }
}
```

Full example project is in `CuipodExample` directory

# Contribution
Feel free to raise an issue ticket or even raise a pull request.