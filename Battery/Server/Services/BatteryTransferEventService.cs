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
    /// Delegate definitions for events
    /// </summary>
    public delegate void TransferStartedEventHandler(object sender, TransferStartedEventArgs e);
    public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
    public delegate void TransferCompletedEventHandler(object sender, TransferCompletedEventArgs e);
    public delegate void WarningRaisedEventHandler(object sender, WarningRaisedEventArgs e);

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

        // Session tracking
        private DateTime _sessionStartTime;
        private int _validSamplesCount;
        private int _rejectedSamplesCount;
        private EisMeta _currentSessionData;

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

        /// <summary>
        /// Private constructor for singleton
        /// </summary>
        private BatteryTransferEventService()
        {
            // Load configuration values
            _voltageThreshold = double.TryParse(ConfigHelper.GetAppSetting("V_threshold"), out double vThreshold) ? vThreshold : 3.5;
            _impedanceThreshold = double.TryParse(ConfigHelper.GetAppSetting("Z_threshold"), out double zThreshold) ? zThreshold : 0.05;
            _deviationPercent = double.TryParse(ConfigHelper.GetAppSetting("DeviationPercent"), out double devPercent) ? devPercent : 25.0;
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
        }

        /// <summary>
        /// Raise transfer completed event
        /// </summary>
        public void RaiseTransferCompleted(EisMeta sessionData, int totalSamples, bool isSuccessful)
        {
            var args = new TransferCompletedEventArgs(sessionData, _sessionStartTime, totalSamples, _validSamplesCount, _rejectedSamplesCount, isSuccessful);
            OnTransferCompleted?.Invoke(this, args);
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