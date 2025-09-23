using System;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {

        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(BatteryServer)))
            {
                host.Open();
                Console.WriteLine("Service is running...");
                Console.ReadLine();

                host.Close();
                Console.WriteLine("Service is closed");
            }
        }
    }
}
