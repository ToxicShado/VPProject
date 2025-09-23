using Common;
using Server.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class BatteryServer : IBatteryCommands
    {
        
        public static EisMeta SessionData = new EisMeta();

        public CommandReturnValues EndSession()
        {
            SessionData = new EisMeta();
            throw new NotImplementedException();
        }

        public CommandReturnValues PushSample(EisSample sample)
        {
            // throw an exception if it does not work?

            if(SessionData == null || SessionData == new EisMeta())
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
            Console.WriteLine($"Starting new session for BatteryId: {data.BatteryId}, TestId: {data.TestId}, SoC: {data.SoC}, FileName: {data.FileName}, TotalRows: {data.TotalRows}"); 
            //throw new NotImplementedException();
            return new CommandReturnValues()
            {
                State = STATE.ACK,
                Status = STATUS.COMPLETED
            };
        }
    }
}
