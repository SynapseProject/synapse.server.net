using System;
using System.Collections.Generic;

using Synapse.Services;

public class DirectoryRoleProvider : IAuthorizationProvider
{
    public List<string> Allowed { get; set; }
    public List<string> Denied { get; set; }

    public string ListSourcePath { get; set; }

    public Dictionary<string, string> Configure(IAuthorizationProviderConfig conifg)
    {
        throw new NotImplementedException();
    }

    public object GetDefaultConfig()
    {
        throw new NotImplementedException();
    }

    public bool? IsAuthorized(string id)
    {
        return true;
    }
}
