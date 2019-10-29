using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace GrpcClient
{
    public class Program
    {
        static async Task Main()
        {
            // The port number(5001) must match the port of the gRPC server.
            var channel = GrpcChannel.ForAddress("https://localhost:44327");
            var client = new Push.PushClient(channel);
            var reply = await client.PushAsync(new PushRequest
            {
                CompanyId = 109090080uL,
                InvoiceCode = "sdfg",
                InvoiceNo = "efoehgirht",
                InvoiceType = PushRequest.Types.InvoiceType.Electric,
                Jshj = "1000.11",
                Kprq = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Pdf = "dffgggh",
                RequestId = "dsdfdf",
                Ticket = PushRequest.Types.Ticket.Blue
            });
            Console.WriteLine("Greeting: " + reply.Code);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
