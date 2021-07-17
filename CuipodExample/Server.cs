using Cuipod;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Security.Cryptography.X509Certificates;

namespace CuipodExample
{
    class Server
    {
        static int Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication();
            commandLineApplication.HelpOption("-h | --help");
            CommandArgument directoryToServe = commandLineApplication.Argument(
                "directory",
                "Directory to server (required)"
            );
            CommandArgument certificateFile = commandLineApplication.Argument(
                "pfx certificate file",
                "Path to certificate (required)"
            );
            CommandArgument pfxPassword = commandLineApplication.Argument(
               "pfx password",
               "pfx password"
            );
            commandLineApplication.OnExecute(() =>
            {
                if (directoryToServe.Value == null || certificateFile.Value == null )
                {
                    commandLineApplication.ShowHelp();
                    return 1;
                }

                var pass = (pfxPassword != null)  ? pfxPassword.Value.ToString() : "";
                var cert = new X509Certificate2(certificateFile.Value.ToString(), pass);

                return AppMain(directoryToServe.Value, cert);
            });

            try
            {
                return commandLineApplication.Execute(args);
            } catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return 1;
            }
        }

        private static int AppMain(string directoryToServe, X509Certificate2 certificate)
        {
            App app = new App(
                directoryToServe,
                certificate
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
