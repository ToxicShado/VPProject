using Server.EventSubscribers;
using Server.Services;
using System;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Battery Server with Event System ===");
            Console.WriteLine("Initializing event services...");

            var eventService = BatteryTransferEventService.Instance;
            var consoleSubscriber = new ConsoleEventSubscriber();
            var fileLogSubscriber = new FileLogEventSubscriber();
            var statisticsSubscriber = new StatisticsEventSubscriber();

            consoleSubscriber.Subscribe(eventService);
            fileLogSubscriber.Subscribe(eventService);
            statisticsSubscriber.Subscribe(eventService);

            Console.WriteLine("Event subscribers initialized:");
            Console.WriteLine("   - Console Event Logger");
            Console.WriteLine("   - File Event Logger");
            Console.WriteLine("   - Statistics Monitor");
            Console.WriteLine();

            try
            {
                using (ServiceHost host = new ServiceHost(typeof(BatteryServer)))
                {
                    host.Open();
                    Console.WriteLine("Battery Server is running and ready for connections...");
                    Console.WriteLine("Event monitoring is active");
                    Console.WriteLine("Press any key to display current statistics, or 'q' to quit");
                    
                    ConsoleKeyInfo keyInfo;
                    do
                    {
                        keyInfo = Console.ReadKey(true);
                        
                        switch (keyInfo.Key)
                        {
                            case ConsoleKey.S:
                                statisticsSubscriber.DisplayStatistics();
                                break;
                            case ConsoleKey.H:
                                DisplayHelp();
                                break;
                            case ConsoleKey.Q:
                                Console.WriteLine("Shutting down server...");
                                break;
                            case ConsoleKey.C:
                                if (keyInfo.Modifiers == ConsoleModifiers.Control)
                                {
                                    Console.WriteLine("Ctrl+C pressed, shutting down...");
                                    keyInfo = new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false);
                                }
                                break;
                            default:
                                statisticsSubscriber.DisplayStatistics();
                                break;
                        }
                    } 
                    while (keyInfo.Key != ConsoleKey.Q);

                    host.Close();
                    Console.WriteLine("Battery Server stopped");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
                
                var eventService2 = BatteryTransferEventService.Instance;
                eventService2.RaiseWarning("SERVER_ERROR", $"Server encountered fatal error: {ex.Message}", "CRITICAL");
            }
            finally
            {
                try
                {
                    consoleSubscriber.Unsubscribe(eventService);
                    fileLogSubscriber.Unsubscribe(eventService);
                    statisticsSubscriber.Unsubscribe(eventService);
                    Console.WriteLine("Event subscriptions cleaned up");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during cleanup: {ex.Message}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("\n=== Server Controls ===");
            Console.WriteLine("S - Display statistics");
            Console.WriteLine("H - Show this help");
            Console.WriteLine("Q - Quit server");
            Console.WriteLine("Any other key - Display statistics");
            Console.WriteLine("Ctrl+C - Emergency shutdown");
            Console.WriteLine("=======================\n");
        }
    }
}
