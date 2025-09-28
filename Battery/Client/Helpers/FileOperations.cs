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

        public static Dictionary<EisMeta, List<EisSample>> LoadData(string folderPath = ".\\MockData")
        {
            var data = new Dictionary<EisMeta, List<EisSample>>();
            try
            {
                if (Directory.Exists(folderPath))
                {
                    var csvFiles = Directory.GetFiles(folderPath, "*.csv").ToList();

                    if (csvFiles.Count <= 0)
                    {
                        return data;
                    }

                    foreach (var file in csvFiles)
                    {
                        Console.WriteLine($"Processing file: {file}");

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

                        List<EisSample> batteryData = new List<EisSample>();
                        int rowIndex = 1;

                        // Skip header line (first line)
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
                                    RowIndex = rowIndex++,
                                    FrequencyHz = frequency,
                                    R_ohm = r_ohm,
                                    X_ohm = x_ohm,
                                    Voltage_V = voltage,
                                    T_degC = temperature_celsius,
                                    Range_ohm = range_ohm,
                                    TimestampLocal = DateTime.Now
                                });
                            }
                        }

                        // Parse filename to extract metadata
                        var fileName = Path.GetFileName(file);
                        var splitFilename = fileName.Split('_');
                        
                        if (splitFilename.Length >= 6)
                        {
                            var batteryId = "IFR14500"; // Extract from filename
                            var testId = splitFilename[0]; // "Hk"
                            var soc = splitFilename[3]; // SoC percentage
                            
                            // Try to parse date and time
                            DateTime dateOfTest = DateTime.Now;
                            if (splitFilename.Length > 5)
                            {
                                var dateStr = splitFilename[4];
                                var timeStr = splitFilename[5].Replace(".csv", "");
                                DateTime.TryParse($"{dateStr} {timeStr.Replace("-", ":")}", out dateOfTest);
                            }

                            var metadata = new EisMeta
                            {
                                BatteryId = batteryId,
                                TestId = testId,
                                SoC = soc,
                                FileName = fileName,
                                TotalRows = batteryData.Count
                            };

                            if (metadata != null && batteryData != null && batteryData.Count > 0)
                            {
                                data[metadata] = batteryData;
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
