using Common;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace Server.Services
{
    public static class ConfigHelper
    {
        private static NameValueCollection _appSettings;

        static ConfigHelper()
        {
            LoadAppSettings();
        }

        private static void LoadAppSettings()
        {
            _appSettings = new NameValueCollection();
            
            try
            {
                string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                if (File.Exists(configPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(configPath);
                    
                    var appSettingsNode = doc.SelectSingleNode("//appSettings");
                    if (appSettingsNode != null)
                    {
                        foreach (XmlNode node in appSettingsNode.ChildNodes)
                        {
                            if (node.Name == "add" && node.Attributes["key"] != null && node.Attributes["value"] != null)
                            {
                                _appSettings[node.Attributes["key"].Value] = node.Attributes["value"].Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load app.config: {ex.Message}");
            }
            
            if (_appSettings["V_threshold"] == null) _appSettings["V_threshold"] = "3.5";
            if (_appSettings["Z_threshold"] == null) _appSettings["Z_threshold"] = "0.05";
            if (_appSettings["DeviationPercent"] == null) _appSettings["DeviationPercent"] = "25";
            if (_appSettings["T_threshold"] == null) _appSettings["T_threshold"] = "5.0";
            if (_appSettings["R_min"] == null) _appSettings["R_min"] = "0.05";
            if (_appSettings["R_max"] == null) _appSettings["R_max"] = "0.3";
            if (_appSettings["Range_min"] == null) _appSettings["Range_min"] = "0.2";
            if (_appSettings["Range_max"] == null) _appSettings["Range_max"] = "3.5";
            if (_appSettings["EnableDetailedConsoleLogging"] == null) _appSettings["EnableDetailedConsoleLogging"] = "false";
            if (_appSettings["EnableWarningConsoleLogging"] == null) _appSettings["EnableWarningConsoleLogging"] = "true";
            if (_appSettings["EnableFileLogging"] == null) _appSettings["EnableFileLogging"] = "true";
            if (_appSettings["EnableStatistics"] == null) _appSettings["EnableStatistics"] = "true";
            if (_appSettings["LogDirectory"] == null) _appSettings["LogDirectory"] = "Logs";
        }

        public static string GetAppSetting(string key, string defaultValue = null)
        {
            return _appSettings[key] ?? defaultValue;
        }
    }

    public class TransferStartedEventArgs : EventArgs
    {
        public EisMeta SessionData { get; set; }
        public DateTime StartTime { get; set; }
        public int ExpectedSamples { get; set; }

        public TransferStartedEventArgs(EisMeta sessionData, int expectedSamples)
        {
            SessionData = sessionData;
            ExpectedSamples = expectedSamples;
            StartTime = DateTime.Now;
        }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public EisSample Sample { get; set; }
        public EisMeta SessionData { get; set; }
        public int SampleCount { get; set; }
        public int TotalSamples { get; set; }
        public DateTime ReceivedTime { get; set; }
        public bool IsValid { get; set; }

        public SampleReceivedEventArgs(EisSample sample, EisMeta sessionData, int sampleCount, int totalSamples, bool isValid)
        {
            Sample = sample;
            SessionData = sessionData;
            SampleCount = sampleCount;
            TotalSamples = totalSamples;
            IsValid = isValid;
            ReceivedTime = DateTime.Now;
        }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public EisMeta SessionData { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalSamples { get; set; }
        public int ValidSamples { get; set; }
        public int RejectedSamples { get; set; }
        public bool IsSuccessful { get; set; }

        public TransferCompletedEventArgs(EisMeta sessionData, DateTime startTime, int totalSamples, int validSamples, int rejectedSamples, bool isSuccessful)
        {
            SessionData = sessionData;
            StartTime = startTime;
            EndTime = DateTime.Now;
            Duration = EndTime - StartTime;
            TotalSamples = totalSamples;
            ValidSamples = validSamples;
            RejectedSamples = rejectedSamples;
            IsSuccessful = isSuccessful;
        }
    }

    public class WarningRaisedEventArgs : EventArgs
    {
        public string WarningType { get; set; }
        public string Message { get; set; }
        public EisSample Sample { get; set; }
        public EisMeta SessionData { get; set; }
        public DateTime WarningTime { get; set; }
        public string Severity { get; set; }

        public WarningRaisedEventArgs(string warningType, string message, string severity, EisSample sample = null, EisMeta sessionData = null)
        {
            WarningType = warningType;
            Message = message;
            Severity = severity;
            Sample = sample;
            SessionData = sessionData;
            WarningTime = DateTime.Now;
        }
    }

    public class TemperatureSpikeEventArgs : EventArgs
    {
        public EisSample CurrentSample { get; set; }
        public EisSample PreviousSample { get; set; }
        public EisMeta SessionData { get; set; }
        public double TemperatureDelta { get; set; }
        public string Direction { get; set; }
        public double Threshold { get; set; }
        public DateTime DetectionTime { get; set; }

        public TemperatureSpikeEventArgs(EisSample currentSample, EisSample previousSample, EisMeta sessionData, double temperatureDelta, string direction, double threshold)
        {
            CurrentSample = currentSample;
            PreviousSample = previousSample;
            SessionData = sessionData;
            TemperatureDelta = temperatureDelta;
            Direction = direction;
            Threshold = threshold;
            DetectionTime = DateTime.Now;
        }
    }
    public class ResistanceOutOfBoundsEventArgs : EventArgs
    {
        public EisSample Sample { get; set; }
        public EisMeta SessionData { get; set; }
        public double ActualValue { get; set; }
        public double MinThreshold { get; set; }
        public double MaxThreshold { get; set; }
        public string BoundsType { get; set; }
        public DateTime DetectionTime { get; set; }

        public ResistanceOutOfBoundsEventArgs(EisSample sample, EisMeta sessionData, double actualValue, double minThreshold, double maxThreshold, string boundsType)
        {
            Sample = sample;
            SessionData = sessionData;
            ActualValue = actualValue;
            MinThreshold = minThreshold;
            MaxThreshold = maxThreshold;
            BoundsType = boundsType;
            DetectionTime = DateTime.Now;
        }
    }

    public class RangeMismatchEventArgs : EventArgs
    {
        public EisSample Sample { get; set; }
        public EisMeta SessionData { get; set; }
        public double ActualValue { get; set; }
        public double MinThreshold { get; set; }
        public double MaxThreshold { get; set; }
        public DateTime DetectionTime { get; set; }

        public RangeMismatchEventArgs(EisSample sample, EisMeta sessionData, double actualValue, double minThreshold, double maxThreshold)
        {
            Sample = sample;
            SessionData = sessionData;
            ActualValue = actualValue;
            MinThreshold = minThreshold;
            MaxThreshold = maxThreshold;
            DetectionTime = DateTime.Now;
        }
    }

    public delegate void TransferStartedEventHandler(object sender, TransferStartedEventArgs e);
    public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
    public delegate void TransferCompletedEventHandler(object sender, TransferCompletedEventArgs e);
    public delegate void WarningRaisedEventHandler(object sender, WarningRaisedEventArgs e);
    public delegate void TemperatureSpikeEventHandler(object sender, TemperatureSpikeEventArgs e);
    public delegate void ResistanceOutOfBoundsEventHandler(object sender, ResistanceOutOfBoundsEventArgs e);
    public delegate void RangeMismatchEventHandler(object sender, RangeMismatchEventArgs e);

    public class BatteryTransferEventService
    {
        private static BatteryTransferEventService _instance;
        private static readonly object _lock = new object();

        private readonly double _voltageThreshold;
        private readonly double _impedanceThreshold;
        private readonly double _deviationPercent;
        private readonly double _temperatureThreshold;
        private readonly double _resistanceMin;
        private readonly double _resistanceMax;
        private readonly double _rangeMin;
        private readonly double _rangeMax;

        private DateTime _sessionStartTime;
        private int _validSamplesCount;
        private int _rejectedSamplesCount;
        private EisMeta _currentSessionData;
        
        private EisSample _previousSample;

        private double _runningVoltageSum = 0;
        private double _runningImpedanceSum = 0;
        private double _runningResistanceSum = 0;
        private int _averageSampleCount = 0;
        private const int MIN_SAMPLES_FOR_AVERAGE = 5;


        public static BatteryTransferEventService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new BatteryTransferEventService();
                    }
                }
                return _instance;
            }
        }


        public event TransferStartedEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event TransferCompletedEventHandler OnTransferCompleted;
        public event WarningRaisedEventHandler OnWarningRaised;
        public event TemperatureSpikeEventHandler OnTemperatureSpike;
        public event ResistanceOutOfBoundsEventHandler OnResistanceOutOfBounds;
        public event RangeMismatchEventHandler OnRangeMismatch;


        private BatteryTransferEventService()
        {
            _voltageThreshold = double.TryParse(ConfigHelper.GetAppSetting("V_threshold"), out double vThreshold) ? vThreshold : 3.5;
            _impedanceThreshold = double.TryParse(ConfigHelper.GetAppSetting("Z_threshold"), out double zThreshold) ? zThreshold : 0.05;
            _deviationPercent = double.TryParse(ConfigHelper.GetAppSetting("DeviationPercent"), out double devPercent) ? devPercent : 25.0;
            _temperatureThreshold = double.TryParse(ConfigHelper.GetAppSetting("T_threshold"), out double tThreshold) ? tThreshold : 5.0;
            _resistanceMin = double.TryParse(ConfigHelper.GetAppSetting("R_min"), out double rMin) ? rMin : 0.05;
            _resistanceMax = double.TryParse(ConfigHelper.GetAppSetting("R_max"), out double rMax) ? rMax : 0.3;
            _rangeMin = double.TryParse(ConfigHelper.GetAppSetting("Range_min"), out double rangeMin) ? rangeMin : 0.2;
            _rangeMax = double.TryParse(ConfigHelper.GetAppSetting("Range_max"), out double rangeMax) ? rangeMax : 3.5;
        }

        public void RaiseTransferStarted(EisMeta sessionData, int expectedSamples)
        {
            _sessionStartTime = DateTime.Now;
            _validSamplesCount = 0;
            _rejectedSamplesCount = 0;
            _currentSessionData = sessionData;
            _previousSample = null; 

            _runningVoltageSum = 0;
            _runningImpedanceSum = 0;
            _runningResistanceSum = 0;
            _averageSampleCount = 0;

            var args = new TransferStartedEventArgs(sessionData, expectedSamples);
            OnTransferStarted?.Invoke(this, args);
        }


        public void RaiseSampleReceived(EisSample sample, EisMeta sessionData, int sampleCount, int totalSamples, bool isValid)
        {
            if (isValid)
                _validSamplesCount++;
            else
                _rejectedSamplesCount++;

            var args = new SampleReceivedEventArgs(sample, sessionData, sampleCount, totalSamples, isValid);
            OnSampleReceived?.Invoke(this, args);

            if (isValid && sample != null)
            {
                UpdateRunningAverages(sample);
            }

            CheckForWarnings(sample, sessionData);
            
            if (isValid && sample != null)
            {
                CheckForTemperatureSpike(sample, sessionData);
                _previousSample = sample; 
            }
        }

        private void UpdateRunningAverages(EisSample sample)
        {
            _averageSampleCount++;
            _runningVoltageSum += sample.Voltage_V;
            _runningResistanceSum += sample.R_ohm;
            
            double totalImpedance = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);
            _runningImpedanceSum += totalImpedance;
        }

        private void CheckForWarnings(EisSample sample, EisMeta sessionData)
        {
            if (sample == null) return;

            if (sample.Voltage_V < _voltageThreshold)
            {
                RaiseWarning("VOLTAGE_LOW", 
                    $"Voltage ({sample.Voltage_V:F3}V) below threshold ({_voltageThreshold}V)", 
                    "WARNING", sample, sessionData);
            }

            double totalImpedance = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);
            if (totalImpedance < _impedanceThreshold)
            {
                RaiseWarning("IMPEDANCE_LOW", 
                    $"Total impedance ({totalImpedance:F6}Ω) below threshold ({_impedanceThreshold}Ω)", 
                    "WARNING", sample, sessionData);
            }

            if (_averageSampleCount >= MIN_SAMPLES_FOR_AVERAGE)
            {
                CheckAverageDeviations(sample, sessionData, totalImpedance);
            }
        }

        private void CheckAverageDeviations(EisSample sample, EisMeta sessionData, double totalImpedance)
        {
            // Calculate current running averages
            double avgVoltage = _runningVoltageSum / _averageSampleCount;
            double avgResistance = _runningResistanceSum / _averageSampleCount;
            double avgImpedance = _runningImpedanceSum / _averageSampleCount;

            // Check voltage deviation
            double voltageDeviation = Math.Abs((sample.Voltage_V - avgVoltage) / avgVoltage) * 100;
            if (voltageDeviation > _deviationPercent)
            {
                RaiseWarning("VOLTAGE_AVERAGE_DEVIATION", 
                    $"Voltage deviation from average: {voltageDeviation:F1}% | " +
                    $"Current: {sample.Voltage_V:F3}V | Average: {avgVoltage:F3}V | " +
                    $"Threshold: ±{_deviationPercent}% | Sample: {sample.RowIndex}", 
                    "WARNING", sample, sessionData);
            }

            // Check resistance deviation
            double resistanceDeviation = Math.Abs((sample.R_ohm - avgResistance) / avgResistance) * 100;
            if (resistanceDeviation > _deviationPercent)
            {
                RaiseWarning("RESISTANCE_AVERAGE_DEVIATION", 
                    $"Resistance deviation from average: {resistanceDeviation:F1}% | " +
                    $"Current: {sample.R_ohm:F6}Ω | Average: {avgResistance:F6}Ω | " +
                    $"Threshold: ±{_deviationPercent}% | Sample: {sample.RowIndex}", 
                    "WARNING", sample, sessionData);
            }

            // Check impedance deviation
            double impedanceDeviation = Math.Abs((totalImpedance - avgImpedance) / avgImpedance) * 100;
            if (impedanceDeviation > _deviationPercent)
            {
                RaiseWarning("IMPEDANCE_AVERAGE_DEVIATION", 
                    $"Impedance deviation from average: {impedanceDeviation:F1}% | " +
                    $"Current: {totalImpedance:F6}Ω | Average: {avgImpedance:F6}Ω | " +
                    $"Threshold: ±{_deviationPercent}% | Sample: {sample.RowIndex}", 
                    "WARNING", sample, sessionData);
            }
        }

        public void RaiseTransferCompleted(EisMeta sessionData, int totalSamples, bool isSuccessful)
        {
            var args = new TransferCompletedEventArgs(sessionData, _sessionStartTime, totalSamples, _validSamplesCount, _rejectedSamplesCount, isSuccessful);
            OnTransferCompleted?.Invoke(this, args);
            
            _previousSample = null;
        }

        public void RaiseWarning(string warningType, string message, string severity, EisSample sample = null, EisMeta sessionData = null)
        {
            var args = new WarningRaisedEventArgs(warningType, message, severity, sample, sessionData);
            OnWarningRaised?.Invoke(this, args);
        }

        public void RaiseTemperatureSpike(EisSample currentSample, EisSample previousSample, EisMeta sessionData, double temperatureDelta, string direction)
        {
            var args = new TemperatureSpikeEventArgs(currentSample, previousSample, sessionData, temperatureDelta, direction, _temperatureThreshold);
            OnTemperatureSpike?.Invoke(this, args);
        }

        public void RaiseResistanceOutOfBounds(EisSample sample, EisMeta sessionData, double actualValue, double minThreshold, double maxThreshold, string boundsType)
        {
            var args = new ResistanceOutOfBoundsEventArgs(sample, sessionData, actualValue, minThreshold, maxThreshold, boundsType);
            OnResistanceOutOfBounds?.Invoke(this, args);
        }

        public void RaiseRangeMismatch(EisSample sample, EisMeta sessionData, double actualValue, double minThreshold, double maxThreshold)
        {
            var args = new RangeMismatchEventArgs(sample, sessionData, actualValue, minThreshold, maxThreshold);
            OnRangeMismatch?.Invoke(this, args);
        }

        public bool ValidateSampleBounds(EisSample sample, EisMeta sessionData)
        {
            if (sample == null) return true;

            bool isValid = true;

            if (sample.R_ohm < _resistanceMin || sample.R_ohm > _resistanceMax)
            {
                string boundsType = sample.R_ohm < _resistanceMin ? "BELOW_MIN" : "ABOVE_MAX";
                
                RaiseResistanceOutOfBounds(sample, sessionData, sample.R_ohm, _resistanceMin, _resistanceMax, boundsType);
                
                string message = $"Resistance out of bounds: {sample.R_ohm:F6}? " +
                               $"(Expected: {_resistanceMin:F3}? - {_resistanceMax:F3}?) | " +
                               $"Sample: {sample.RowIndex} | SoC: {sessionData?.SoC}% | " +
                               $"BatteryId: {sessionData?.BatteryId} | Frequency: {sample.FrequencyHz:F2}Hz";

                RaiseWarning("RESISTANCE_OUT_OF_BOUNDS", message, "CRITICAL", sample, sessionData);
                isValid = false;
            }

            if (sample.Range_ohm < _rangeMin || sample.Range_ohm > _rangeMax)
            {
                RaiseRangeMismatch(sample, sessionData, sample.Range_ohm, _rangeMin, _rangeMax);
                
                string message = $"Range mismatch: {sample.Range_ohm:F3}? " +
                               $"(Expected: {_rangeMin:F3}? - {_rangeMax:F3}?) | " +
                               $"Sample: {sample.RowIndex} | SoC: {sessionData?.SoC}% | " +
                               $"BatteryId: {sessionData?.BatteryId} | Frequency: {sample.FrequencyHz:F2}Hz";

                RaiseWarning("RANGE_MISMATCH", message, "CRITICAL", sample, sessionData);
                isValid = false;
            }

            return isValid;
        }

        private void CheckForTemperatureSpike(EisSample currentSample, EisMeta sessionData)
        {
            if (_previousSample == null || currentSample == null) return;

            double temperatureDelta = currentSample.T_degC - _previousSample.T_degC;
            double absoluteDelta = Math.Abs(temperatureDelta);

            if (absoluteDelta > _temperatureThreshold)
            {
                string direction = temperatureDelta > 0 ? "porast" : "pad";
                
                RaiseTemperatureSpike(currentSample, _previousSample, sessionData, temperatureDelta, direction);
                
                string message = $"Temperature spike detected: {direction} | " +
                               $"Current T: {currentSample.T_degC:F2}°C | " +
                               $"?T: {temperatureDelta:F2}°C | " +
                               $"Frequency: {currentSample.FrequencyHz:F2}Hz | " +
                               $"SoC: {sessionData.SoC}% | " +
                               $"Threshold: {_temperatureThreshold:F2}°C";

                RaiseWarning("TEMPERATURE_SPIKE", message, "WARNING", currentSample, sessionData);
            }
        }
    }
}