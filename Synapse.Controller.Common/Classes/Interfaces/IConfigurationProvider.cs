using System;
using System.Collections.Generic;

namespace Synapse.Services
{
    public interface IConfigurationProvider
    {
        Dictionary<string, string> GetDefaultValues();

        void Configure(Dictionary<string, string> values);

        void Configure(IConfigurationProvider configProvider);
    }
}