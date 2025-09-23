using System.Runtime.Serialization;

[DataContract]
public class ValidationFault
{
    public ValidationFault(string message) { Message = message; }
    [DataMember] public string Message { get; set; }
}