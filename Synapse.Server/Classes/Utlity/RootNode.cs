using System.Collections.Generic;

namespace Synapse.Services
{
    //this is just an xml serialization helper class.
    //  when converting a dict from json -> xml, there may be many "root nodes" (peer kvps in json), so this provides a wrapper el
    public class RootNode
    {
        public IDictionary<object, object> KeyValuePairs { get; set; }
    }
}