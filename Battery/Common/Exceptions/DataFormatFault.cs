using System.Runtime.Serialization;

[DataContract]
public class DataFormatFault
{
    public DataFormatFault(string message) { Message = message; }
    [DataMember] public string Message { get; set; }
}