using System;
using System.Collections.Generic;

namespace Synapse.Services
{
    public interface IConfigurationProvider
    {
        object GetDefaultConfig();

        void Configure(IConfigurationProvider configProvider);
    }
}