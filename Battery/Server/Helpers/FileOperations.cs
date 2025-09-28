using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Helpers
{
    public static class FileOperations
    {
        public static List<Battery> AddNewEntry(EisMeta fileInfo, EisSample sample)
        {
            //Console.WriteLine($"Adding new entry for BatteryId: {fileInfo.BatteryId}, TestId: {fileInfo.TestId}, SoC: {fileInfo.SoC}, FileName: {fileInfo.FileName}, TotalRows: {fileInfo.TotalRows}");
            Console.WriteLine($"Adding new entry for BatteryId: {fileInfo.BatteryId}, TestId: {fileInfo.TestId}, SoC: {fileInfo.SoC}, FileName: {fileInfo.FileName}, TotalRows: {fileInfo.TotalRows}, Sample: FrequencyHz={sample.FrequencyHz}, R_ohm={sample.R_ohm}, X_ohm={sample.X_ohm}, Voltage_V={sample.Voltage_V}, T_degC={sample.T_degC}, Range_ohm={sample.Range_ohm}");
            return new List<Battery>();
        }

        public static void LogFailedSample(EisMeta sessionData, EisSample sample, string errorMessage)
        {
            try
            {
                string logDirectory = "FailedSamples";
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string logFileName = Path.Combine(logDirectory, "failed_samples_log.txt");
                
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                                 $"BatteryId: {sessionData?.BatteryId ?? "N/A"} | " +
                                 $"TestId: {sessionData?.TestId ?? "N/A"} | " +
                                 $"SoC: {sessionData?.SoC ?? "N/A"} | " +
                                 $"FileName: {sessionData?.FileName ?? "N/A"} | " +
                                 $"Sample: RowIndex={sample?.RowIndex ?? -1}, " +
                                 $"FrequencyHz={sample?.FrequencyHz ?? double.NaN}, " +
                                 $"R_ohm={sample?.R_ohm ?? double.NaN}, " +
                                 $"X_ohm={sample?.X_ohm ?? double.NaN}, " +
                                 $"Voltage_V={sample?.Voltage_V ?? double.NaN}, " +
                                 $"T_degC={sample?.T_degC ?? double.NaN}, " +
                                 $"Range_ohm={sample?.Range_ohm ?? double.NaN} | " +
                                 $"Error: {errorMessage}" + Environment.NewLine;

                File.AppendAllText(logFileName, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log failed sample: {ex.Message}");
            }
        }
    }
}
