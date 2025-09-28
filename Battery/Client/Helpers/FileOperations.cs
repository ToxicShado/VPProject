using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Client.Program;

namespace Client.Helpers
{
    class CsvFileReader : IDisposable
    {
        private FileStream fileStream;
        private StreamReader streamReader;
        private bool disposed = false;
        private readonly ILogger logger;

        public CsvFileReader(string path, ILogger logger = null)
        {
            this.logger = logger ?? new FileLogger("log.txt");
            try
            {
                fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                streamReader = new StreamReader(fileStream);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Failed to open file: {path}", ex);
                Dispose();
                throw;
            }
        }

        public IEnumerable<string> ReadLines()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CsvFileReader));

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
                    try
                    {
                        streamReader?.Dispose();
                        fileStream?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError("Error disposing CsvFileReader resources", ex);
                    }
                }
                disposed = true;
            }
        }

        ~CsvFileReader()
        {
            Dispose(false);
        }
    }

    class BatchFileProcessor : IDisposable
    {
        private List<string> fileList;
        private bool disposed = false;
        private readonly ILogger logger;

        public BatchFileProcessor(string folderPath, ILogger logger = null)
        {
            this.logger = logger ?? new FileLogger("log.txt");

            if (!Directory.Exists(folderPath))
            {
                this.logger.LogError($"Directory not found: {folderPath}");
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
            }

            fileList = Directory.GetFiles(folderPath, "*.csv", SearchOption.AllDirectories).ToList();
        }

        public List<(string filePath, List<string> lines)> ProcessFiles()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BatchFileProcessor));

            var results = new List<(string filePath, List<string> lines)>();

            foreach (var file in fileList)
            {
                var fileResult = ProcessSingleFile(file);
                if (fileResult.HasValue)
                {
                    results.Add(fileResult.Value);
                }
            }

            return results;
        }

        private (string filePath, List<string> lines)? ProcessSingleFile(string file)
        {
            List<string> lines = new List<string>();

            try
            {
                using (var reader = new CsvFileReader(file, logger))
                {
                    foreach (var line in reader.ReadLines())
                    {
                        lines.Add(line);
                    }
                }
                return (file, lines);
            }
            catch (IOException ex)
            {
                logger.LogError($"IO Exception while reading {file}", ex);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError($"Access denied for file {file}", ex);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error processing file {file}", ex);
                return null;
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
                    try
                    {
                        fileList?.Clear();
                        fileList = null;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError("Error disposing BatchFileProcessor resources", ex);
                    }
                }
                disposed = true;
            }
        }

        ~BatchFileProcessor()
        {
            Dispose(false);
        }
    }

    public static class FileOperations
    {
        private static Random random = new Random();
        private static readonly ILogger logger = new FileLogger("log.txt");
        private const int REQUIRED_ENTRIES = 28;

        public static Dictionary<EisMeta, List<EisSample>> LoadData(string baseFolderPath = ".\\MockData")
        {
            var data = new Dictionary<EisMeta, List<EisSample>>();

            try
            {
                string searchPattern = Path.Combine(baseFolderPath, "B*", "EIS measurements", "Test_*", "Hioki");
                var directories = Directory.GetDirectories(baseFolderPath, "B*", SearchOption.TopDirectoryOnly);

                foreach (var batteryDir in directories)
                {
                    var eisMeasurementsDir = Path.Combine(batteryDir, "EIS measurements");
                    if (!Directory.Exists(eisMeasurementsDir))
                    {
                        continue;
                    }

                    var testDirs = Directory.GetDirectories(eisMeasurementsDir, "Test_*", SearchOption.TopDirectoryOnly);

                    foreach (var testDir in testDirs)
                    {
                        var hiokiDir = Path.Combine(testDir, "Hioki");
                        if (!Directory.Exists(hiokiDir))
                        {
                            continue;
                        }

                        using (var processor = new BatchFileProcessor(hiokiDir, logger))
                        {
                            foreach (var (filePath, lines) in processor.ProcessFiles())
                            {
                                var processedData = ProcessFileData(filePath, lines);
                                if (processedData.HasValue)
                                {
                                    data[processedData.Value.metadata] = processedData.Value.samples;
                                }
                            }
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.LogError("Directory not found", ex);
            }
            catch (IOException ex)
            {
                logger.LogError("IO Exception during data loading", ex);
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error during data loading", ex);
            }

            return data;
        }

        private static (EisMeta metadata, List<EisSample> samples)? ProcessFileData(string filePath, List<string> lines)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);

                var pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                string batteryId = ExtractBatteryId(pathParts);
                string testId = ExtractTestId(pathParts);

                if (string.IsNullOrEmpty(batteryId) || string.IsNullOrEmpty(testId))
                {
                    logger.LogError($"Failed to extract battery ID or test ID from path: {filePath}");
                    return null;
                }

                string soc = ExtractSoCFromFilename(fileName);
                if (string.IsNullOrEmpty(soc))
                {
                    soc = "Unknown";
                }

                if (lines.Count < REQUIRED_ENTRIES + 1)
                {
                    logger.LogError($"File {fileName} has insufficient data. Expected at least {REQUIRED_ENTRIES} entries, found {lines.Count - 1} (excluding header)");
                }

                List<EisSample> batteryData = new List<EisSample>();
                int rowIndex = 1;

                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var parts = line.Split(',');
                    if (parts.Length < 6)
                    {
                        logger.LogError($"Insufficient columns in row {rowIndex} of file {fileName}. Expected 6, found {parts.Length}");
                        rowIndex++;
                        continue;
                    }

                    if (double.TryParse(parts[0], out double frequency) &&
                        double.TryParse(parts[1], out double r_ohm) &&
                        double.TryParse(parts[2], out double x_ohm) &&
                        double.TryParse(parts[3], out double voltage) &&
                        double.TryParse(parts[4], out double temperature_celsius) &&
                        double.TryParse(parts[5], out double range_ohm))
                    {
                        batteryData.Add(new EisSample
                        {
                            RowIndex = rowIndex,
                            FrequencyHz = frequency,
                            R_ohm = r_ohm,
                            X_ohm = x_ohm,
                            Voltage_V = voltage,
                            T_degC = temperature_celsius,
                            Range_ohm = range_ohm,
                            TimestampLocal = DateTime.Now
                        });
                    }
                    else
                    {
                        logger.LogError($"Invalid numeric data in row {rowIndex} of file {fileName}: {line}");
                    }

                    rowIndex++;
                }
    
                if (batteryData.Count == 0)
                {
                    logger.LogError($"No valid data entries found in file {fileName}");
                    return null;
                }

                var metadata = new EisMeta
                {
                    BatteryId = batteryId,
                    TestId = testId,
                    SoC = soc,
                    FileName = fileName,
                    TotalRows = batteryData.Count
                };

                return (metadata, batteryData);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing file data for {filePath}", ex);
                return null;
            }
        }

        private static string ExtractBatteryId(string[] pathParts)
        {
            var batteryPattern = new Regex(@"^B\d+$", RegexOptions.IgnoreCase);

            foreach (var part in pathParts)
            {
                if (batteryPattern.IsMatch(part))
                {
                    return part;
                }
            }

            return null;
        }

        private static string ExtractTestId(string[] pathParts)
        {
            var testPattern = new Regex(@"^Test_\d+$", RegexOptions.IgnoreCase);

            foreach (var part in pathParts)
            {
                if (testPattern.IsMatch(part))
                {
                    return part;
                }
            }

            return null;
        }

        private static string ExtractSoCFromFilename(string fileName)
        {
            var socPattern = @"SoC_(\d+)";

            var match = Regex.Match(fileName, socPattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value + "%";
            }

            logger.LogError($"Could not extract SoC from filename using SoC_XX pattern: {fileName}");
            return null;
        }
    }
}
