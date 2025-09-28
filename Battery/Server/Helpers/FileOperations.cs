using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Helpers
{
    /// <summary>
    /// Disposable wrapper for writing to CSV files with proper resource management
    /// </summary>
    public class CsvFileWriter : IDisposable
    {
        private FileStream fileStream;
        private StreamWriter streamWriter;
        private bool disposed = false;

        public CsvFileWriter(string filePath, bool append = true)
        {
            fileStream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
            streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
        }

        public void WriteLine(string line)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CsvFileWriter));

            streamWriter.WriteLine(line);
            streamWriter.Flush(); 
        }

        public void Write(string text)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CsvFileWriter));

            streamWriter.Write(text);
            streamWriter.Flush();
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
                    streamWriter?.Dispose();
                    fileStream?.Dispose();
                }
                disposed = true;
            }
        }

        ~CsvFileWriter()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Disposable wrapper for session file management
    /// </summary>
    public class SessionFileManager : IDisposable
    {
        private string sessionPath;
        private string sessionFilePath;
        private string rejectsFilePath;
        private bool disposed = false;
        private static Random random = new Random();

        public SessionFileManager(string sessionPath)
        {
            this.sessionPath = sessionPath;
            this.sessionFilePath = Path.Combine(sessionPath, "session.csv");
            this.rejectsFilePath = Path.Combine(sessionPath, "rejects.csv");
        }

        public void WriteSessionEntry(EisSample sample)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SessionFileManager));

            try
            {

                // Simulate file I/O failure
                if (random.Next(0, 100) < 8)
                {
                    throw new IOException("Simulated disk write failure");
                }

                using (var writer = new CsvFileWriter(sessionFilePath, true))
                {
                    string csvLine = $"{sample.RowIndex},{sample.FrequencyHz},{sample.R_ohm},{sample.X_ohm}," +
                                   $"{sample.Voltage_V},{sample.T_degC},{sample.Range_ohm},{sample.TimestampLocal:yyyy-MM-dd HH:mm:ss}";
                    writer.WriteLine(csvLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write session entry: {ex.Message}");
                throw;
            }
        }

        public void WriteRejectEntry(EisSample sample, string errorMessage)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SessionFileManager));

            try
            {
                using (var writer = new CsvFileWriter(rejectsFilePath, true))
                {
                    string rejectEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},\"{errorMessage}\"," +
                                       $"{sample?.RowIndex ?? -1},{sample?.FrequencyHz ?? double.NaN}," +
                                       $"{sample?.R_ohm ?? double.NaN},{sample?.X_ohm ?? double.NaN}," +
                                       $"{sample?.Voltage_V ?? double.NaN},{sample?.T_degC ?? double.NaN}," +
                                       $"{sample?.Range_ohm ?? double.NaN}";
                    writer.WriteLine(rejectEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write reject entry: {ex.Message}");
                throw;
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
                // No managed resources to dispose in this case, but pattern is here for consistency
                disposed = true;
            }
        }

        ~SessionFileManager()
        {
            Dispose(false);
        }
    }

    public static class FileOperations
    {
        private static string currentSessionPath = "";
        private static SessionFileManager currentSessionManager = null;

        public static void InitializeSession(EisMeta sessionData)
        {
            try
            {
                // Clean up previous session if exists
                CleanupSession();

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
                currentSessionManager = new SessionFileManager(socDir);

                // Create or open session.csv file
                string sessionFile = Path.Combine(socDir, "session.csv");
                if (!File.Exists(sessionFile))
                {
                    using (var writer = new CsvFileWriter(sessionFile, false))
                    {
                        writer.WriteLine("RowIndex,FrequencyHz,R_ohm,X_ohm,Voltage_V,T_degC,Range_ohm,TimestampLocal");
                    }
                }

                // Create rejects.csv file if it doesn't exist
                string rejectsFile = Path.Combine(socDir, "rejects.csv");
                if (!File.Exists(rejectsFile))
                {
                    using (var writer = new CsvFileWriter(rejectsFile, false))
                    {
                        writer.WriteLine("Timestamp,Reason,RowIndex,FrequencyHz,R_ohm,X_ohm,Voltage_V,T_degC,Range_ohm");
                    }
                }

                Console.WriteLine($"Session initialized for BatteryId: {sessionData.BatteryId}, TestId: {sessionData.TestId}, SoC: {sessionData.SoC}%");
                Console.WriteLine($"Session path: {socDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize session: {ex.Message}");
                CleanupSession();
                throw;
            }
        }

        public static List<Battery> AddNewEntry(EisMeta fileInfo, EisSample sample)
        {
            try
            {
                if (string.IsNullOrEmpty(currentSessionPath) || currentSessionManager == null)
                {
                    throw new InvalidOperationException("Session not initialized. Call InitializeSession first.");
                }

                currentSessionManager.WriteSessionEntry(sample);

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
                if (string.IsNullOrEmpty(currentSessionPath) || currentSessionManager == null)
                {
                    LogFailedSampleFallback(sessionData, sample, errorMessage);
                    return;
                }

                currentSessionManager.WriteRejectEntry(sample, errorMessage);
                Console.WriteLine($"Rejected sample logged to rejects.csv: {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log rejected sample: {ex.Message}");
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
                
                using (var writer = new CsvFileWriter(logFileName, true))
                {
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
                                     $"Error: {errorMessage}";
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log failed sample (fallback): {ex.Message}");
            }
        }

        public static void CleanupSession()
        {
            currentSessionManager?.Dispose();
            currentSessionManager = null;
            currentSessionPath = "";
        }
    }
}
