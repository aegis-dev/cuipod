using Cuipod;

namespace CuipodExample
{
    class Server
    {
        static void Main(string[] args)
        {
            App app = new App(
                "<directory_to_serve>/",            // directory to serve 
                "<dir_with_cert>/certificate.crt",  // path to certificate
                "<dir_with_cert>/privatekey.key"    // path to private Pkcs8 RSA key
            );

            // Serve files
            app.OnRequest("/", response => {
                response.RenderFileContent("index.gmi");
            });

            app.OnRequest("/about/", response => {
                response.RenderFileContent("about_me.gmi");
            });

            // Or dynamically render content
            app.OnRequest("/dynamic/content/", response => {
                response.RenderPlainTextLine("# woah much content!");
                response.RenderPlainTextLine("More utilities to render content will come soon!");
            });

            // Optional but nice. In case it is specified and client will do a bad route 
            // request we will respond with Success status and render result from this lambda
            app.OnBadRequest(response => {
                response.RenderPlainTextLine("# Ohh No!!! Request is bad :(");
            });

            app.Run();
        }
    }
}
