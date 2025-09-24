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
    class TheFileReader : IDisposable
    {
        private FileStream fileStream;
        private StreamReader streamReader;
        private bool disposed = false;

        public TheFileReader(string path)
        {
            fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            streamReader = new StreamReader(fileStream);
        }

        public IEnumerable<string> ReadLines()
        {
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (streamReader != null)
                        streamReader.Dispose();
                    if (fileStream != null)
                        fileStream.Dispose();
                }
                disposed = true;
            }
        }

        ~TheFileReader()
        {
            Dispose(false);
        }
    }

    public static class FileOperations
    {
        private static Random random = new Random();

        public static Dictionary<EisMeta, List<EisSample>> LoadData(string folderPath = ".\\SoCEstimation")
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

                                Console.WriteLine($"{hioki}");

                                var filenames = Directory.GetFiles(hioki, "*.csv").ToList();

                                List<EisSample> batteryData = new List<EisSample>();

                                foreach (var file in filenames)
                                {
                                    Console.WriteLine(file);

                                    List<string> lines = new List<string>();
                                    try
                                    {
                                        using (var reader = new TheFileReader(file))
                                        {
                                            foreach (var line in reader.ReadLines())
                                            {
                                                //This can be used to simulate read errors, although mislim da nije to to,
                                                //                                          Demonstrirati zatvaranje resursa i oporavak u slučaju prekida (simuliraj prekid veze usred prenosa).
                                                // Theortically, zelimo da prekinemo vezu somehow, to bi radili u Program.cs
                                                //if (random.Next(0, 100) == 10)
                                                //{
                                                //    throw new IOException("Simulated read error");
                                                //}
                                                lines.Add(line);
                                            }
                                        }
                                    }
                                    catch (IOException ex)
                                    {
                                        Console.WriteLine($"IO Exception while reading {file}: {ex.Message}");
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
                                                Voltage_V = voltage,
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
                Console.WriteLine("IO Exception: " + ex.Message);
            }
            return data;
        }
    }
}
