using Common;
using Server.Helpers;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class BatteryServer : IBatteryCommands
    {
        public static EisMeta SessionData = new EisMeta();
        static double lastIndex = 0;
        static int totalSamplesReceived = 0;
        static int expectedTotalSamples = 0;

        private readonly BatteryTransferEventService _eventService;

        public BatteryServer()
        {
            _eventService = BatteryTransferEventService.Instance;
        }

        public CommandReturnValues EndSession()
        {
            try
            {
                FileOperations.CleanupSession();
                
                bool isSuccessful = totalSamplesReceived > 0;
                _eventService.RaiseTransferCompleted(SessionData, totalSamplesReceived, isSuccessful);
                
                Console.WriteLine($"[SERVER STATUS] Transfer completed! Total samples processed: {totalSamplesReceived}/{expectedTotalSamples}");
                Console.WriteLine($"[SERVER STATUS] Session ended for BatteryId: {SessionData?.BatteryId}, TestId: {SessionData?.TestId}");
                
                SessionData = new EisMeta();
                totalSamplesReceived = 0;
                expectedTotalSamples = 0;
                lastIndex = 0;
                
                return new CommandReturnValues()
                {
                    State = STATE.ACK,
                    Status = STATUS.COMPLETED
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER STATUS] Error ending session: {ex.Message}");
                _eventService.RaiseWarning("SESSION_END_ERROR", $"Failed to end session: {ex.Message}", "CRITICAL", null, SessionData);
                
                return new CommandReturnValues()
                {
                    State = STATE.NACK,
                    Status = STATUS.COMPLETED
                };
            }
        }

        public CommandReturnValues PushSample(EisSample sample)
        {
            if (SessionData == null || SessionData == new EisMeta())
            {
                Console.WriteLine("[SERVER STATUS] Error: No active session - sample rejected");
                _eventService.RaiseWarning("NO_ACTIVE_SESSION", "Sample received without active session", "CRITICAL", sample);
                
                return new CommandReturnValues()
                {
                    State = STATE.NACK,
                    Status = STATUS.COMPLETED
                };
            }

            bool isValid = false;
            
            try
            {
                validateSample(sample);
                
                bool boundsValid = _eventService.ValidateSampleBounds(sample, SessionData);
                
                if (boundsValid)
                {
                    FileOperations.AddNewEntry(SessionData, sample);
                    totalSamplesReceived++;
                    isValid = true;
                    
                    _eventService.RaiseSampleReceived(sample, SessionData, totalSamplesReceived, expectedTotalSamples, isValid);
                    
                    if (expectedTotalSamples <= 20 || totalSamplesReceived % 10 == 0 || totalSamplesReceived == expectedTotalSamples)
                    {
                        double progressPercentage = expectedTotalSamples > 0 ? (double)totalSamplesReceived / expectedTotalSamples * 100 : 0;
                        Console.WriteLine($"[SERVER STATUS] Transfer in progress... {totalSamplesReceived}/{expectedTotalSamples} samples ({progressPercentage:F1}%)");
                    }
                    
                    return new CommandReturnValues()
                    {
                        State = STATE.ACK,
                        Status = totalSamplesReceived >= expectedTotalSamples ? STATUS.COMPLETED : STATUS.IN_PROGRESS
                    };
                }
                else
                {
                    string rejectReason = "Sample failed bounds validation (resistance or range out of bounds)";
                    FileOperations.LogFailedSample(SessionData, sample, rejectReason);
                    
                    _eventService.RaiseSampleReceived(sample, SessionData, totalSamplesReceived + 1, expectedTotalSamples, isValid);
                    
                    Console.WriteLine($"[SERVER STATUS] Sample {sample.RowIndex} rejected: {rejectReason}");
                    
                    return new CommandReturnValues()
                    {
                        State = STATE.NACK,
                        Status = STATUS.IN_PROGRESS  
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER STATUS] Error processing sample {sample?.RowIndex}: {ex.Message}");
                
                FileOperations.LogFailedSample(SessionData, sample, ex.Message);
                
                _eventService.RaiseSampleReceived(sample, SessionData, totalSamplesReceived + 1, expectedTotalSamples, isValid);
                
                _eventService.RaiseWarning("SAMPLE_VALIDATION_ERROR", ex.Message, "WARNING", sample, SessionData);
                
                return new CommandReturnValues()
                {
                    State = STATE.NACK,
                    Status = STATUS.COMPLETED 
                };
            }
        }

        public CommandReturnValues StartSession(EisMeta data)
        {
            try
            {
                validateSessionData(data);
                
                SessionData = data;
                lastIndex = 0;
                totalSamplesReceived = 0;
                expectedTotalSamples = data.TotalRows;
                
                FileOperations.InitializeSession(data);
                
                _eventService.RaiseTransferStarted(data, expectedTotalSamples);
                
                Console.WriteLine($"[SERVER STATUS] Session started for BatteryId: {data.BatteryId}, TestId: {data.TestId}, SoC: {data.SoC}%");
                Console.WriteLine($"[SERVER STATUS] Expected {expectedTotalSamples} samples - transfer ready to begin...");
                
                return new CommandReturnValues()
                {
                    State = STATE.ACK,
                    Status = STATUS.IN_PROGRESS
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER STATUS] Error starting session: {ex.Message}");
                _eventService.RaiseWarning("SESSION_START_ERROR", $"Failed to start session: {ex.Message}", "CRITICAL", null, data);
                
                return new CommandReturnValues()
                {
                    State = STATE.NACK,
                    Status = STATUS.COMPLETED
                };
            }
        }

        private void validateSessionData(EisMeta data)
        {
            if (data == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Session data is null."), "Validation error");

            if (string.IsNullOrWhiteSpace(data.BatteryId))
                throw new FaultException<ValidationFault>(
                    new ValidationFault("BatteryId cannot be null or empty."), "Validation error");

            if (string.IsNullOrWhiteSpace(data.TestId))
                throw new FaultException<ValidationFault>(
                    new ValidationFault("TestId cannot be null or empty."), "Validation error");

            if (string.IsNullOrWhiteSpace(data.SoC))
                throw new FaultException<ValidationFault>(
                    new ValidationFault("SoC cannot be null or empty."), "Validation error");

            if (data.TotalRows <= 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("TotalRows must be greater than 0."), "Validation error");
        }

        public bool validateSample(EisSample sample)
        {
            if (sample == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sample is null."), "Validation error");

            if (double.IsNaN(sample.R_ohm) || double.IsInfinity(sample.R_ohm))
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("R_ohm must be a real number."), "Data format error");

            if (double.IsNaN(sample.T_degC) || double.IsInfinity(sample.T_degC))
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("T_degC must be a real number."), "Data format error");

            if (sample.FrequencyHz <= 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("FrequencyHz must be greater than 0."), "Validation error");
            
            if (sample.RowIndex != lastIndex+1)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Row index must be in ascending order."), "Validation error");
            
            lastIndex = sample.RowIndex;
            return true;
        }
    }
}


