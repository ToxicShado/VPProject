using System;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class EisSample
    {
        [DataMember] public int RowIndex { get; set; }
        [DataMember] public double FrequencyHz { get; set; }
        [DataMember] public double R_ohm { get; set; }
        [DataMember] public double X_ohm { get; set; }
        [DataMember] public double Voltage_V { get; set; }
        [DataMember] public double T_degC { get; set; }
        [DataMember] public double Range_ohm { get; set; }
        [DataMember] public DateTime TimestampLocal { get; set; }

        public override string ToString()
        {
            return $"RowIndex: {RowIndex}, FrequencyHz: {FrequencyHz}, R_ohm: {R_ohm}, X_ohm: {X_ohm}, Voltage_V: {Voltage_V}, T_degC: {T_degC}, Range_ohm: {Range_ohm}, TimestampLocal: {TimestampLocal}";
        }
    }
}