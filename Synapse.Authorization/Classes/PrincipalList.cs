using System;
using System.Collections.Generic;

using YamlDotNet.Serialization;

public class PrincipalList
{
    public List<string> Allowed { get; set; }
    [YamlIgnore]
    public bool HasAllowed { get { return Allowed != null && Allowed.Count > 0; } }

    public List<string> Denied { get; set; }
    [YamlIgnore]
    public bool HasDenied { get { return Denied != null && Denied.Count > 0; } }

    [YamlIgnore]
    public bool HasContent { get { return HasAllowed || HasDenied; } }
}