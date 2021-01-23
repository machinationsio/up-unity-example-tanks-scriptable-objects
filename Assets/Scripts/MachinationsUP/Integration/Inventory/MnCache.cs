using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MachinationsUP.Integration.Inventory
{

    [DataContract(Name = "MachinationsCache", Namespace = "http://www.machinations.io")]
    public class MnCache
    {

        [DataMember]
        public List<DiagramMapping> DiagramMappings = new List<DiagramMapping>();

    }
}
