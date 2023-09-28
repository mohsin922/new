using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubPubSub
{
    public interface IPubSub
    {
        public void InitProducer(string connectionString, string eventHubName);
        public void InitConsumer(string connectionString, string eventHubName, string consumerEventOption, int CancelAfterTime, int FromHours);
        public Task Producer(string message);
        public  Task Consumer();
    }
}
