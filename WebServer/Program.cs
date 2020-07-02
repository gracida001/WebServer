using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WebServer
{
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://192.168.0.102:8080/";
        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "<div class=\"wrap\">" +
        "<div class=\"search\">" +
            "<form method = \"post\" action=\"search\">" +
                    "<input type = \"text\" id=\"searchValue\" name=\"searchValue\"><br><br>" +
                    "<input type = \"submit\" value=\"Submit\">" +
            "</form>" +
        "</div> {0}"+
            "</div>" +
            "  </body>" +
            "</html>";
        public static async Task HandleIncomingConnections()
        {

            while (true)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/search"))
                {
                    StreamReader reader = new StreamReader(req.InputStream);
                    string input = reader.ReadToEnd();
                    string subInput = input.Substring(input.IndexOf('=') + 1, input.Length - (input.IndexOf('=') + 1));
                    IMongoDatabase db = ConnectDbAndGetDb();
                    var collection = db.GetCollection<BsonDocument>("WebSites");
                    var filter = Builders<BsonDocument>.Filter.AnyEq("Url", subInput);

                    var studentDocument = collection.Find(filter).FirstOrDefault();
                    Console.WriteLine(studentDocument.ToString());
                    byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, studentDocument.ToJson()));
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data., 0, data.Length);

                }
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/add"))
                {
                    StreamReader reader = new StreamReader(req.InputStream);
                    string[] input = reader.ReadToEnd().Split('&');
                    for (int i = 0; i < input.Length; i++)
                    {
                        input[i] = input[i].Substring(input[i].IndexOf('=') + 1, input[i].Length - (input[i].IndexOf('=') + 1));
                    }
                    Console.WriteLine();
                    //Connetti e collegati alla collezione
                    IMongoDatabase db = ConnectDbAndGetDb();
                    var collection = db.GetCollection<BsonDocument>("WebSites");
                    //Crea una classe per questo e la addo
                    BsonDocument document = new BsonDocument{ { "Nome", input[0] },{"Url",input[1] },
                        {"Keys",
                            new BsonArray {
                                new BsonDocument { { "Parola", input[2].Split("%2C")[0] } },
                                new BsonDocument { { "Parola", input[2].Split("%2C")[1] } }
                            }
                        }
                    };
                    collection.InsertOne(document);
                    await ShowPage(resp, "HomeSearchEngine.html");
                }

                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();


                if (req.Url.AbsolutePath == "/createsite")
                {
                    await ShowPage(resp, "CreateWebSite.html");
                }
                if (req.Url.AbsolutePath == "/")
                {
                    await ShowPage(resp, "HomeSearchEngine.html");
                }

            }
        }

        private static async Task ShowPage(HttpListenerResponse resp, string pagina)
        {
            byte[] data = Encoding.UTF8.GetBytes(String.Format(File.ReadAllText(pagina)));
            resp.ContentType = "text/html";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }

        public static void Main(string[] args)
        {

            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }

        private static IMongoDatabase ConnectDbAndGetDb()
        {
            var client = new MongoClient("mongodb+srv://admin:admin@cluster0-pbhpe.mongodb.net/Db_WebServer?retryWrites=true&w=majority");
            var db = client.GetDatabase("Db_WebServer");
            return db;
        }
    }
}


