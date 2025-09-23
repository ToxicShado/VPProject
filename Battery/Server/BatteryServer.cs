using Common;
using Server.Helpers;
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
        static double lastIndex = -1;

        public CommandReturnValues EndSession()
        {
            SessionData = new EisMeta();
            return new CommandReturnValues()
            {
                State = STATE.ACK,
                Status = STATUS.COMPLETED
            };
        }

        public CommandReturnValues PushSample(EisSample sample)
        {


            if (SessionData == null || SessionData == new EisMeta())
            {
                return new CommandReturnValues()
                {
                    State = STATE.NACK,
                    Status = STATUS.COMPLETED
                };
            }



            FileOperations.AddNewEntry(SessionData, sample);

            return new CommandReturnValues()
            {
                State = STATE.ACK,
                Status = STATUS.COMPLETED
            };
        }

        public CommandReturnValues StartSession(EisMeta data)
        {
            SessionData = data;
            lastIndex = -1;
            //Console.WriteLine($"Starting new session for BatteryId: {data.BatteryId}, TestId: {data.TestId}, SoC: {data.SoC}, FileName: {data.FileName}, TotalRows: {data.TotalRows}");
            //throw new NotImplementedException();
            return new CommandReturnValues()
            {
                State = STATE.ACK,
                Status = STATUS.COMPLETED
            };
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


