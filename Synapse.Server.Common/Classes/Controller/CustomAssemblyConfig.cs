using System;
using System.Collections.Generic;

using YamlDotNet.Serialization;

namespace Synapse.Services
{
    public class CustomAssemblyConfig
    {
        public string Name { get; set; }
        public string RoutePrefix { get; set; }
        public object Config { get; set; }
        public List<string> JsonConverters { get; set; }
        [YamlIgnore]
        public bool HasJsonConverters { get { return JsonConverters?.Count > 0; } }
    }
}