using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventHubPubSub
{
    public class PubSub : IPubSub
    {
        string _connectionString = string.Empty;
        string _eventHUbNamE = string.Empty;
        string _ConsumerConnectionString = string.Empty;
        string _ConsumerEventHUbNamE = string.Empty;
        string _consumerEventOption = string.Empty;
        int _CancelAfterTime;
        int _FromHours;
        public async Task Consumer()
        {
            //var builder = new ConfigurationBuilder()
            //       .AddJsonFile($"appsettings.json", true, true);

            //var config = builder.Build();
            //string type = config["consumerEventOption"];
            //var connectionString = config["ConnectionStrings:Connection"];
            //var eventHUbNamE = config["eventHubName"];
            //int hours = Convert.ToInt32(config["FromHours"]);

            var consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

            var consumer = new EventHubConsumerClient(
                consumerGroup,
                _ConsumerConnectionString,
                _ConsumerEventHUbNamE);
            try
            {
                using CancellationTokenSource cancellationSource = new CancellationTokenSource();
                cancellationSource.CancelAfter(TimeSpan.FromSeconds(Convert.ToDouble(_CancelAfterTime)));
                IAsyncEnumerable<PartitionEvent> eventList = Events(_consumerEventOption, consumer, cancellationSource, _FromHours);
                await foreach (PartitionEvent partitionEvent in eventList)
                {
                    string readFromPartition = partitionEvent.Partition.PartitionId;
                    byte[] eventBodyBytes = partitionEvent.Data.EventBody.ToArray();

                    Console.WriteLine($"Read event of length { eventBodyBytes.Length } from { readFromPartition }");
                    var encodedEventMessage = (Encoding.Default.GetString(eventBodyBytes));
                    var decodedEventMessage = String.Empty;
                    try
                    {
                        decodedEventMessage = Encoding.UTF8.GetString(Convert.FromBase64String(encodedEventMessage));
                    }
                    catch
                    {
                        decodedEventMessage = encodedEventMessage;
                    }
                    string TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    Console.WriteLine($"{TimeStamp}:{decodedEventMessage}");

                }
            }
            catch (TaskCanceledException)
            {
                // This is expected if the cancellation token is
                // signaled.
            }
            finally
            {
                await consumer.CloseAsync();
            }

        }
        private static IAsyncEnumerable<PartitionEvent> Events(string consumerEventOption, EventHubConsumerClient consumer, CancellationTokenSource cancellationSource, int hours)
        {
            //IAsyncEnumerable<PartitionEvent> partitionEvents = new IAsyncEnumerable<PartitionEvent>();
            var eventList = (dynamic)null;
            string firstPartition = String.Empty;
            EventPosition startingPosition;
            PartitionProperties properties;
            switch (consumerEventOption)
            {
                case "ReadAllEvents":
                    eventList = consumer.ReadEventsAsync(cancellationSource.Token);
                    break;
                case "LatestEvents":
                    eventList = consumer.ReadEventsAsync(startReadingAtEarliestEvent: false,
                    cancellationToken: cancellationSource.Token);
                    break;
                case "ConsumerEventsFromAPartition":
                    firstPartition = consumer.GetPartitionIdsAsync(cancellationSource.Token).Result.First();
                    startingPosition = EventPosition.Earliest;
                    eventList = consumer.ReadEventsFromPartitionAsync(
                    firstPartition,
                    startingPosition,
                    cancellationSource.Token);
                    break;
                case "ReadEventsFromSpecificDateTime":
                    DateTimeOffset timePeriod = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(hours));
                    startingPosition = EventPosition.FromEnqueuedTime(timePeriod);
                    firstPartition = consumer.GetPartitionIdsAsync(cancellationSource.Token).Result.First();
                    eventList = consumer.ReadEventsFromPartitionAsync(firstPartition, startingPosition, cancellationSource.Token);
                    break;
                case "ReadEventsFromSpecificOffSet":
                    firstPartition = consumer.GetPartitionIdsAsync(cancellationSource.Token).Result.First();
                    properties = consumer.GetPartitionPropertiesAsync(firstPartition, cancellationSource.Token).Result;
                    startingPosition = EventPosition.FromOffset(properties.LastEnqueuedOffset);
                    eventList = consumer.ReadEventsFromPartitionAsync(firstPartition, startingPosition, cancellationSource.Token);
                    break;
                case "ReadEventsFromSpecificSeqNumber":
                    firstPartition = consumer.GetPartitionIdsAsync(cancellationSource.Token).Result.First();
                    properties = consumer.GetPartitionPropertiesAsync(firstPartition, cancellationSource.Token).Result;
                    startingPosition = EventPosition.FromSequenceNumber(properties.LastEnqueuedSequenceNumber);
                    eventList = consumer.ReadEventsFromPartitionAsync(firstPartition, startingPosition, cancellationSource.Token);
                    break;
                default:
                    break;
            }
            return eventList;
        }

       

        public async Task Producer(string message)
        {
            EventHubProducerClient producerClient;


            //var builder = new ConfigurationBuilder()
            //       .AddJsonFile($"appsettings.json", true, true);

            //var config = builder.Build();
            

            // Create a producer client that you can use to send events to an event hub
            producerClient = new EventHubProducerClient(_connectionString, _eventHUbNamE);
            logMessage("Producer client intialized");

            try
            {
                do
                {
                    // Create a batch of events 
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
                    if (!eventBatch.TryAdd(new EventData(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)))))
                    {
                        // if it is too large for the batch
                        throw new Exception($"Event  is too large for the batch and cannot be sent.");
                    }
                    
                    logMessage("Producing the event");

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);

                    logMessage("An event has been published");
                    eventBatch.Dispose();
                    Console.WriteLine("Move to reciever?(Y/N)");
                    string rec = Console.ReadLine().ToLower();
                    if (rec == "y")
                    {
                        break;
                    }
                    Console.WriteLine("Press Ctrl-C to exit | Enter the next message to continue: ");

                    message = Console.ReadLine();
                } while (true);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        public void InitProducer(string connectionString, string eventHubName)
        {
            if (_connectionString == string.Empty)
            {
                this._connectionString = connectionString;
            }
            this._eventHUbNamE = eventHubName;
        }

        public void InitConsumer(string connectionString, string eventHubName, string consumerEventOption, int CancelAfterTime, int FromHours)
        {
            if (_ConsumerConnectionString == string.Empty)
            {
                this._ConsumerConnectionString = connectionString;
            }
            this._ConsumerEventHUbNamE = eventHubName;
            this._consumerEventOption = consumerEventOption;
            this._CancelAfterTime = CancelAfterTime;
            this._FromHours = FromHours;
        }
    }
}
