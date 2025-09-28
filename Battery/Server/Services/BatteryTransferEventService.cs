using Common;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace Server.Services
{
    /// <summary>
    /// Configuration helper for reading app.config values
    /// </summary>
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
            
            // Set default values if not found
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

    /// <summary>
    /// Event arguments for transfer started event
    /// </summary>
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

    /// <summary>
    /// Event arguments for sample received event
    /// </summary>
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

    /// <summary>
    /// Event arguments for transfer completed event
    /// </summary>
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

    /// <summary>
    /// Event arguments for warning raised event
    /// </summary>
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

    /// <summary>
    /// Event arguments for temperature spike event
    /// </summary>
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

    /// <summary>
    /// Event arguments for resistance out of bounds event
    /// </summary>
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

    /// <summary>
    /// Event arguments for range mismatch event
    /// </summary>
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

    /// <summary>
    /// Delegate definitions for events
    /// </summary>
    public delegate void TransferStartedEventHandler(object sender, TransferStartedEventArgs e);
    public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
    public delegate void TransferCompletedEventHandler(object sender, TransferCompletedEventArgs e);
    public delegate void WarningRaisedEventHandler(object sender, WarningRaisedEventArgs e);
    public delegate void TemperatureSpikeEventHandler(object sender, TemperatureSpikeEventArgs e);
    public delegate void ResistanceOutOfBoundsEventHandler(object sender, ResistanceOutOfBoundsEventArgs e);
    public delegate void RangeMismatchEventHandler(object sender, RangeMismatchEventArgs e);

    /// <summary>
    /// Event service for monitoring battery data transfer operations
    /// </summary>
    public class BatteryTransferEventService
    {
        private static BatteryTransferEventService _instance;
        private static readonly object _lock = new object();

        // Configuration values from app.config
        private readonly double _voltageThreshold;
        private readonly double _impedanceThreshold;
        private readonly double _deviationPercent;
        private readonly double _temperatureThreshold;
        private readonly double _resistanceMin;
        private readonly double _resistanceMax;
        private readonly double _rangeMin;
        private readonly double _rangeMax;

        // Session tracking
        private DateTime _sessionStartTime;
        private int _validSamplesCount;
        private int _rejectedSamplesCount;
        private EisMeta _currentSessionData;
        
        // Temperature spike detection
        private EisSample _previousSample;

        /// <summary>
        /// Singleton instance
        /// </summary>
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

        /// <summary>
        /// Events
        /// </summary>
        public event TransferStartedEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event TransferCompletedEventHandler OnTransferCompleted;
        public event WarningRaisedEventHandler OnWarningRaised;
        public event TemperatureSpikeEventHandler OnTemperatureSpike;
        public event ResistanceOutOfBoundsEventHandler OnResistanceOutOfBounds;
        public event RangeMismatchEventHandler OnRangeMismatch;

        /// <summary>
        /// Private constructor for singleton
        /// </summary>
        private BatteryTransferEventService()
        {
            // Load configuration values
            _voltageThreshold = double.TryParse(ConfigHelper.GetAppSetting("V_threshold"), out double vThreshold) ? vThreshold : 3.5;
            _impedanceThreshold = double.TryParse(ConfigHelper.GetAppSetting("Z_threshold"), out double zThreshold) ? zThreshold : 0.05;
            _deviationPercent = double.TryParse(ConfigHelper.GetAppSetting("DeviationPercent"), out double devPercent) ? devPercent : 25.0;
            _temperatureThreshold = double.TryParse(ConfigHelper.GetAppSetting("T_threshold"), out double tThreshold) ? tThreshold : 5.0;
            _resistanceMin = double.TryParse(ConfigHelper.GetAppSetting("R_min"), out double rMin) ? rMin : 0.05;
            _resistanceMax = double.TryParse(ConfigHelper.GetAppSetting("R_max"), out double rMax) ? rMax : 0.3;
            _rangeMin = double.TryParse(ConfigHelper.GetAppSetting("Range_min"), out double rangeMin) ? rangeMin : 0.2;
            _rangeMax = double.TryParse(ConfigHelper.GetAppSetting("Range_max"), out double rangeMax) ? rangeMax : 3.5;
        }

        /// <summary>
        /// Raise transfer started event
        /// </summary>
        public void RaiseTransferStarted(EisMeta sessionData, int expectedSamples)
        {
            _sessionStartTime = DateTime.Now;
            _validSamplesCount = 0;
            _rejectedSamplesCount = 0;
            _currentSessionData = sessionData;
            _previousSample = null; // Reset previous sample for new session

            var args = new TransferStartedEventArgs(sessionData, expectedSamples);
            OnTransferStarted?.Invoke(this, args);
        }

        /// <summary>
        /// Raise sample received event
        /// </summary>
        public void RaiseSampleReceived(EisSample sample, EisMeta sessionData, int sampleCount, int totalSamples, bool isValid)
        {
            if (isValid)
                _validSamplesCount++;
            else
                _rejectedSamplesCount++;

            var args = new SampleReceivedEventArgs(sample, sessionData, sampleCount, totalSamples, isValid);
            OnSampleReceived?.Invoke(this, args);

            // Check for warnings based on configuration thresholds
            CheckForWarnings(sample, sessionData);
            
            // Check for temperature spikes if we have a valid sample
            if (isValid && sample != null)
            {
                CheckForTemperatureSpike(sample, sessionData);
                _previousSample = sample; // Store current sample as previous for next comparison
            }
        }

        /// <summary>
        /// Raise transfer completed event
        /// </summary>
        public void RaiseTransferCompleted(EisMeta sessionData, int totalSamples, bool isSuccessful)
        {
            var args = new TransferCompletedEventArgs(sessionData, _sessionStartTime, totalSamples, _validSamplesCount, _rejectedSamplesCount, isSuccessful);
            OnTransferCompleted?.Invoke(this, args);
            
            // Reset previous sample at end of session
            _previousSample = null;
        }

        /// <summary>
        /// Raise warning event
        /// </summary>
        public void RaiseWarning(string warningType, string message, string severity, EisSample sample = null, EisMeta sessionData = null)
        {
            var args = new WarningRaisedEventArgs(warningType, message, severity, sample, sessionData);
            OnWarningRaised?.Invoke(this, args);
        }

        /// <summary>
        /// Raise temperature spike event
        /// </summary>
        public void RaiseTemperatureSpike(EisSample currentSample, EisSample previousSample, EisMeta sessionData, double temperatureDelta, string direction)
        {
            var args = new TemperatureSpikeEventArgs(currentSample, previousSample, sessionData, temperatureDelta, direction, _temperatureThreshold);
            OnTemperatureSpike?.Invoke(this, args);
        }

        /// <summary>
        /// Raise resistance out of bounds event
        /// </summary>
        public void RaiseResistanceOutOfBounds(EisSample sample, EisMeta sessionData, double actualValue, double minThreshold, double maxThreshold, string boundsType)
        {
            var args = new ResistanceOutOfBoundsEventArgs(sample, sessionData, actualValue, minThreshold, maxThreshold, boundsType);
            OnResistanceOutOfBounds?.Invoke(this, args);
        }

        /// <summary>
        /// Raise range mismatch event
        /// </summary>
        public void RaiseRangeMismatch(EisSample sample, EisMeta sessionData, double actualValue, double minThreshold, double maxThreshold)
        {
            var args = new RangeMismatchEventArgs(sample, sessionData, actualValue, minThreshold, maxThreshold);
            OnRangeMismatch?.Invoke(this, args);
        }

        /// <summary>
        /// Validate sample bounds and raise appropriate events
        /// Returns false if validation fails (sample should be rejected)
        /// </summary>
        public bool ValidateSampleBounds(EisSample sample, EisMeta sessionData)
        {
            if (sample == null) return true;

            bool isValid = true;

            // Check resistance bounds
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

            // Check range bounds
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

        /// <summary>
        /// Check for temperature spikes between consecutive measurements
        /// Formula: ?T = T(t) - T(t-?t)
        /// </summary>
        private void CheckForTemperatureSpike(EisSample currentSample, EisMeta sessionData)
        {
            if (_previousSample == null || currentSample == null) return;

            // Calculate ?T = T(t) - T(t-?t)
            double temperatureDelta = currentSample.T_degC - _previousSample.T_degC;
            double absoluteDelta = Math.Abs(temperatureDelta);

            // Check if |?T| > T_threshold
            if (absoluteDelta > _temperatureThreshold)
            {
                string direction = temperatureDelta > 0 ? "porast" : "pad";
                
                // Raise the TemperatureSpike event
                RaiseTemperatureSpike(currentSample, _previousSample, sessionData, temperatureDelta, direction);
                
                // Also raise a warning for logging purposes
                string message = $"Temperature spike detected: {direction} | " +
                               $"Current T: {currentSample.T_degC:F2}°C | " +
                               $"?T: {temperatureDelta:F2}°C | " +
                               $"Frequency: {currentSample.FrequencyHz:F2}Hz | " +
                               $"SoC: {sessionData.SoC}% | " +
                               $"Threshold: {_temperatureThreshold:F2}°C";

                RaiseWarning("TEMPERATURE_SPIKE", message, "WARNING", currentSample, sessionData);
            }
        }

        /// <summary>
        /// Check for warnings based on configuration thresholds
        /// </summary>
        private void CheckForWarnings(EisSample sample, EisMeta sessionData)
        {
            if (sample == null) return;

            // Check voltage threshold
            if (sample.Voltage_V < _voltageThreshold)
            {
                RaiseWarning("VOLTAGE_LOW", 
                    $"Voltage ({sample.Voltage_V:F3}V) below threshold ({_voltageThreshold}V)", 
                    "WARNING", sample, sessionData);
            }

            // Check impedance threshold  
            double totalImpedance = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);
            if (totalImpedance < _impedanceThreshold)
            {
                RaiseWarning("IMPEDANCE_LOW", 
                    $"Total impedance ({totalImpedance:F6}?) below threshold ({_impedanceThreshold}?)", 
                    "WARNING", sample, sessionData);
            }

            // Check temperature range (reasonable battery operating range)
            if (sample.T_degC < -20 || sample.T_degC > 60)
            {
                RaiseWarning("TEMPERATURE_OUT_OF_RANGE", 
                    $"Temperature ({sample.T_degC:F1}°C) outside normal operating range (-20°C to 60°C)", 
                    "CRITICAL", sample, sessionData);
            }

            // Check for abnormal frequency values
            if (sample.FrequencyHz > 100000) // 100kHz
            {
                RaiseWarning("FREQUENCY_HIGH", 
                    $"Frequency ({sample.FrequencyHz:F0}Hz) unusually high", 
                    "INFO", sample, sessionData);
            }

            // Check for deviation in range (if range is significantly different from expected)
            if (sample.Range_ohm > 0 && totalImpedance > 0)
            {
                double deviationPercent = Math.Abs((sample.Range_ohm - totalImpedance) / totalImpedance) * 100;
                if (deviationPercent > _deviationPercent)
                {
                    RaiseWarning("RANGE_DEVIATION", 
                        $"Range deviation ({deviationPercent:F1}%) exceeds threshold ({_deviationPercent}%)", 
                        "WARNING", sample, sessionData);
                }
            }
        }
    }
}