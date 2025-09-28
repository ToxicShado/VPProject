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
            ChannelFactory<IBatteryCommands> factory = null;
            IBatteryCommands proxy = null;

            try
            {
                factory = new ChannelFactory<IBatteryCommands>("BatteryServer");
                proxy = factory.CreateChannel();

                var keys = batteries.Keys.ToList();

                foreach (var key in keys)
                {
                    Console.WriteLine("\n\n");
                    Console.WriteLine(key);
                    
                    try
                    {
                        proxy.StartSession(key);
                        
                        foreach (var sample in batteries[key])
                        {
                            Console.WriteLine(sample);
                            proxy.PushSample(sample);
                        }
                        
                        proxy.EndSession();
                    }
                    catch (CommunicationException ex)
                    {
                        Console.WriteLine($"Communication error during session for {key}: {ex.Message}");
                        // Try to recover connection if possible
                        try
                        {
                            if (proxy != null)
                            {
                                ((ICommunicationObject)proxy).Abort();
                            }
                            
                            // Recreate connection
                            proxy = factory.CreateChannel();
                            Console.WriteLine("Connection recovered, continuing...");
                        }
                        catch (Exception recoveryEx)
                        {
                            Console.WriteLine($"Failed to recover connection: {recoveryEx.Message}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing session for {key}: {ex.Message}");
                        continue;
                    }
                }

                Console.WriteLine("Processing completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (proxy != null)
                    {
                        var communicationObject = proxy as ICommunicationObject;
                        if (communicationObject?.State == CommunicationState.Faulted)
                        {
                            communicationObject.Abort();
                        }
                        else
                        {
                            communicationObject?.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing proxy: {ex.Message}");
                    ((ICommunicationObject)proxy)?.Abort();
                }

                try
                {
                    if (factory != null)
                    {
                        if (factory.State == CommunicationState.Faulted)
                        {
                            factory.Abort();
                        }
                        else
                        {
                            factory.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing factory: {ex.Message}");
                    factory?.Abort();
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
