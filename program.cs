using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventHubsReceiver
{
    public class program
    {
     
        public static async Task Main(string[] args)
        {
            await Reciever.ReadEvents();

        }
    }
}
