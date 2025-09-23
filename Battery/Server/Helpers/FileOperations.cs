using Common;
using System;
using System.Collections.Generic;
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
    }
}
