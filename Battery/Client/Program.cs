using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        List<Battery> batteries = Helpers.FileOperations.LoadData();

        static void Main(string[] args)
        {
            ChannelFactory<IBatteryCommands> factory = new ChannelFactory<IBatteryCommands>("BatteryServer");

            IBatteryCommands proxy = factory.CreateChannel();

            proxy.StartSession(new EisMeta()
            {
                BatteryId = "TestBattery",
                TestId = "Test1",
                SoC = "100",
                FileName = "TestBattery_Test1_100.csv",
                TotalRows = 0
            });

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
