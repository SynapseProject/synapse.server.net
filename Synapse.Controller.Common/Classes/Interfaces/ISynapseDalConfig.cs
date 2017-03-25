namespace Synapse.Services
{
    public interface ISynapseDalConfig
    {
        object Config { get; set; }
        string LdapRoot { get; set; }
        string Type { get; set; }
    }
}