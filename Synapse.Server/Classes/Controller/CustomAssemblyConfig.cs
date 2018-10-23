using System.Collections.Generic;

namespace Synapse.Services
{
    public class CustomAssemblyConfig
    {
        public string Name { get; set; }
        public string RoutePrefix { get; set; }
        public object Config { get; set; }
        public List<string> JsonConverters { get; set; }
        public bool HasJsonConverters { get { return JsonConverters?.Count > 0; } }
    }
}