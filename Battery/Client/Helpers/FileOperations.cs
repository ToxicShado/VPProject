using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using static Client.Program;

namespace Client.Helpers
{
    public static class FileOperations
    {

        public static Dictionary<EisMeta,List<EisSample>> LoadData(string folderPath = ".\\SoCEstimation")
        {
            var data = new Dictionary<EisMeta, List<EisSample>>();
            try
            {
                if (Directory.Exists(folderPath))
                {
                    var dirs = Directory.GetDirectories(folderPath);
                    string batteryId = "";
                    if(dirs.Length <= 0)
                    {
                        return data;
                    }

                    foreach (var dir in dirs)
                    {
                        batteryId = dir.Split('\\')[2];
                        Console.WriteLine($"{batteryId}");

                        var eis = dir + "\\EIS measurements";
                        if (Directory.Exists(eis))
                        {
                            var eisPaths = Directory.GetDirectories(eis);
                            if (eisPaths.Length <= 0)
                            {
                                return data;
                            }

                            foreach (var path in eisPaths)
                            {
                                var hioki = path + "\\Hioki";
                                if (!Directory.Exists(hioki))
                                {
                                    return data;
                                }

                                Console.WriteLine($"{ hioki }");

                                var filenames = Directory.GetFiles(hioki, "*.csv").ToList();

                                List<EisSample> batteryData = new List<EisSample>();

                                foreach (var file in filenames)
                                {
                                    Console.WriteLine(file);

                                    string[] lines;
                                    try
                                    {
                                        lines = File.ReadAllLines(file);
                                    }
                                    catch (IOException)
                                    {
                                        // Log or skip this file
                                        continue;
                                    }

                                    foreach (var line in lines.Skip(1))
                                    {
                                        var parts = line.Split(',');
                                        if (parts.Length >= 6 &&
                                            double.TryParse(parts[0], out double frequency) &&
                                            double.TryParse(parts[1], out double r_ohm) &&
                                            double.TryParse(parts[2], out double x_ohm) &&
                                            double.TryParse(parts[3], out double voltage) &&
                                            double.TryParse(parts[4], out double temperature_celsius) &&
                                            double.TryParse(parts[5], out double range_ohm))
                                        {
                                            batteryData.Add(new EisSample
                                            {
                                                FrequencyHz = frequency,
                                                Range_ohm = r_ohm,
                                                X_ohm = x_ohm,
                                                Voltage_V= voltage,
                                                T_degC = temperature_celsius,
                                                R_ohm = range_ohm
                                            });
                                        }
                                    }

                                    var splitFilename = file.Split('_');
                                    EisMeta metadata;
                                    DateTime dateOfTest;
                                    if (splitFilename.Length > 5 && DateTime.TryParse(splitFilename[4] + " " + splitFilename[5].Replace(".csv", ""), out dateOfTest))
                                    { 
                                        metadata = new EisMeta
                                        {
                                            BatteryId = batteryId,
                                            TestId = splitFilename[1],
                                            SoC = splitFilename[3],
                                            FileName = Path.GetFileName(file),
                                            TotalRows = batteryData.Count
                                        };
                                        if (metadata != null && batteryData != null)
                                        {
                                            data[metadata] = batteryData;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                // Log the exception or handle as needed
                Console.WriteLine("IO Exception: " + ex.Message);
                // Optionally return empty data or rethrow
            }
            return data;
        }
    }
}
