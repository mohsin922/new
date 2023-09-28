using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventHubsReceiver
{
    public class Reciever
    {
        public static async Task ReadEvents()
        {
            var builder = new ConfigurationBuilder()
                  .AddJsonFile($"appsettings.json", true, true);

            var config = builder.Build();
            string type = config["consumerEventKey"];
            var connectionString = config["ConnectionStrings:Connection"];
            var eventHUbNamE = config["eventHubName"];
            int hours = Convert.ToInt32(config["FromHours"]);

            var consumerGroup = "cg1";

            azvar consumer = new EventHubConsumerClient(
                consumerGroup,
                connectionString,
                eventHUbNamE);
            try
            {
                using CancellationTokenSource cancellationSource = new CancellationTokenSource();
                cancellationSource.CancelAfter(TimeSpan.FromSeconds(Convert.ToDouble(config["CancelAfterTime"])));
                IAsyncEnumerable<PartitionEvent> eventList = Events(type, consumer, cancellationSource,hours);
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

        private static IAsyncEnumerable<PartitionEvent> Events(string type, EventHubConsumerClient consumer, CancellationTokenSource cancellationSource,int hours)
        {
            var eventList = (dynamic)null;
            string firstPartition = String.Empty;
            EventPosition startingPosition ;
            PartitionProperties properties;
            switch (type)
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
                    eventList = consumer.ReadEventsFromPartitionAsync(firstPartition, startingPosition,cancellationSource.Token);
                    break;
                case "ReadEventsFromSpecificOffSet":
                    firstPartition = consumer.GetPartitionIdsAsync(cancellationSource.Token).Result.First();
                    properties = consumer.GetPartitionPropertiesAsync(firstPartition, cancellationSource.Token).Result;
                    startingPosition = EventPosition.FromOffset(properties.LastEnqueuedOffset);
                    eventList=consumer.ReadEventsFromPartitionAsync(firstPartition, startingPosition, cancellationSource.Token);
                    break;
                case "ReadEventsFromSpecificSeqNumber":
                    firstPartition = consumer.GetPartitionIdsAsync(cancellationSource.Token).Result.First();
                    properties = consumer.GetPartitionPropertiesAsync(firstPartition, cancellationSource.Token).Result;
                    startingPosition = EventPosition.FromSequenceNumber(properties.LastEnqueuedSequenceNumber);
                    eventList= consumer.ReadEventsFromPartitionAsync(firstPartition,startingPosition,cancellationSource.Token);
                    break;
                default:
                    break;
            }
            return eventList;
        }
    }
}
