using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface IBatteryCommands
    {
        [OperationContract]
        CommandReturnValues StartSession(EisMeta data);
        [OperationContract]
        CommandReturnValues EndSession();
        [OperationContract]
        CommandReturnValues PushSample(EisSample sample);
    }

    [DataContract]
    public enum STATE
    {
        [EnumMember] NACK,
        [EnumMember] ACK
    }

    [DataContract]
    public enum STATUS
    {
        [EnumMember] IN_PROGRESS,
        [EnumMember] COMPLETED
    }

    [DataContract]
    public class CommandReturnValues
    {
        [DataMember] public STATE State { get; set; }
        [DataMember] public STATUS Status { get; set; }
    }
}
