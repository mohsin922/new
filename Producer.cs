using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;

namespace EventHubsSender
{
    class Producer
    {

        // The Event Hubs client types are safe to cache and use as a singleton for the lifetime
        // of the application, which is best practice when events are being published or read regularly.
        static EventHubProducerClient producerClient;

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                   .AddJsonFile($"appsettings.json", true, true);

            var config = builder.Build();
            var connectionString = config["ConnectionStrings:senderConnectionString"];
            var eventHUbNamE = config["eventHubName"];

            // Create a producer client that you can use to send events to an event hub
            producerClient = new EventHubProducerClient(connectionString, eventHUbNamE);

            //Console.WriteLine($"Producer client intialized {TimeStamp()}");
            logMessage("Producer client intialized");


            try
            {
                int count = args.Length;
                var inputMessage = args[count - 1];
                //var inputMessage = "Hello";
                do
                {
                    // Create a batch of events 
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
                 
                        if (!eventBatch.TryAdd(new EventData(Convert.ToBase64String(Encoding.UTF8.GetBytes(inputMessage)))))
                        {
                            // if it is too large for the batch
                            throw new Exception($"Event  is too large for the batch and cannot be sent.");
                        }
                       
                    logMessage("Producing the event");

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);

                    logMessage("An event has been published");
                    eventBatch.Dispose();
                    Console.WriteLine("Press Ctrl-C to exit | Enter the next message to continue: ");
                    
                    inputMessage = Console.ReadLine();
                } while (true);
            }

            finally
            {
                await producerClient.DisposeAsync();
            }


        }
        public static string TimeStamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }
        public static void logMessage(string eventMessage)
        {
            Console.WriteLine(TimeStamp() + ":" + eventMessage);
        }
    }
}
