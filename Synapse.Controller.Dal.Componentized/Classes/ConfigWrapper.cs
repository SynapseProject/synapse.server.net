using Synapse.Services;

public class ConfigWrapper : ISynapseDalConfig
{
    public object Config { get; set; }
    public string LdapRoot { get; set; }
    public string Type { get; set; }
}