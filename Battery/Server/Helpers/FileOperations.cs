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
        private static string currentSessionPath = "";

        public static void InitializeSession(EisMeta sessionData)
        {
            try
            {
                // Create the directory structure: Data/<BatteryId>/<TestId>/<SoC%>/
                string baseDataDir = "Data";
                string batteryDir = Path.Combine(baseDataDir, sessionData.BatteryId);
                string testDir = Path.Combine(batteryDir, sessionData.TestId);
                string socDir = Path.Combine(testDir, $"{sessionData.SoC}%");

                // Create directories if they don't exist
                if (!Directory.Exists(socDir))
                {
                    Directory.CreateDirectory(socDir);
                }

                currentSessionPath = socDir;

                // Create or open session.csv file
                string sessionFile = Path.Combine(socDir, "session.csv");
                if (!File.Exists(sessionFile))
                {
                    // Create header for session.csv
                    string header = "RowIndex,FrequencyHz,R_ohm,X_ohm,Voltage_V,T_degC,Range_ohm,TimestampLocal" + Environment.NewLine;
                    File.WriteAllText(sessionFile, header);
                }

                // Create rejects.csv file if it doesn't exist
                string rejectsFile = Path.Combine(socDir, "rejects.csv");
                if (!File.Exists(rejectsFile))
                {
                    // Create header for rejects.csv
                    string rejectsHeader = "Timestamp,Reason,RowIndex,FrequencyHz,R_ohm,X_ohm,Voltage_V,T_degC,Range_ohm" + Environment.NewLine;
                    File.WriteAllText(rejectsFile, rejectsHeader);
                }

                Console.WriteLine($"Session initialized for BatteryId: {sessionData.BatteryId}, TestId: {sessionData.TestId}, SoC: {sessionData.SoC}%");
                Console.WriteLine($"Session path: {socDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize session: {ex.Message}");
                throw;
            }
        }

        public static List<Battery> AddNewEntry(EisMeta fileInfo, EisSample sample)
        {
            try
            {
                if (string.IsNullOrEmpty(currentSessionPath))
                {
                    throw new InvalidOperationException("Session not initialized. Call InitializeSession first.");
                }

                string sessionFile = Path.Combine(currentSessionPath, "session.csv");
                
                string csvLine = $"{sample.RowIndex},{sample.FrequencyHz},{sample.R_ohm},{sample.X_ohm}," +
                               $"{sample.Voltage_V},{sample.T_degC},{sample.Range_ohm},{sample.TimestampLocal:yyyy-MM-dd HH:mm:ss}" + 
                               Environment.NewLine;

                File.AppendAllText(sessionFile, csvLine);

                Console.WriteLine($"Added sample to session.csv: RowIndex={sample.RowIndex}, " +
                                $"FrequencyHz={sample.FrequencyHz}, R_ohm={sample.R_ohm}, X_ohm={sample.X_ohm}, " +
                                $"Voltage_V={sample.Voltage_V}, T_degC={sample.T_degC}, Range_ohm={sample.Range_ohm}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add entry to session.csv: {ex.Message}");
                throw;
            }

            return new List<Battery>();
        }

        public static void LogFailedSample(EisMeta sessionData, EisSample sample, string errorMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(currentSessionPath))
                {
                    // Fallback to old logging method if session path is not available
                    LogFailedSampleFallback(sessionData, sample, errorMessage);
                    return;
                }

                string rejectsFile = Path.Combine(currentSessionPath, "rejects.csv");
                
                string rejectEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},\"{errorMessage}\"," +
                                   $"{sample?.RowIndex ?? -1},{sample?.FrequencyHz ?? double.NaN}," +
                                   $"{sample?.R_ohm ?? double.NaN},{sample?.X_ohm ?? double.NaN}," +
                                   $"{sample?.Voltage_V ?? double.NaN},{sample?.T_degC ?? double.NaN}," +
                                   $"{sample?.Range_ohm ?? double.NaN}" + Environment.NewLine;

                File.AppendAllText(rejectsFile, rejectEntry);

                Console.WriteLine($"Rejected sample logged to rejects.csv: {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log rejected sample: {ex.Message}");
                // Fallback to old logging method
                LogFailedSampleFallback(sessionData, sample, errorMessage);
            }
        }

        private static void LogFailedSampleFallback(EisMeta sessionData, EisSample sample, string errorMessage)
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
                Console.WriteLine($"Failed to log failed sample (fallback): {ex.Message}");
            }
        }

        public static void CleanupSession()
        {
            currentSessionPath = "";
        }
    }
}
