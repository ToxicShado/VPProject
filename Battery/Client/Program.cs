using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        static Dictionary<EisMeta, List<EisSample>> batteries = Helpers.FileOperations.LoadData();

        static void Main(string[] args)
        {
            ChannelFactory<IBatteryCommands> factory = new ChannelFactory<IBatteryCommands>("BatteryServer");

            IBatteryCommands proxy = factory.CreateChannel();

            var keys = batteries.Keys.ToList();

            var sendSample = new EisSample();
            foreach (var key in keys)
            {
                Console.WriteLine("\n\n");
                Console.WriteLine(key);
                proxy.StartSession(key);
                foreach (var sample in batteries[key])
                {
                    Console.WriteLine(sample);
                    proxy.PushSample(sample);
                }
                proxy.EndSession();
            }


            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
