using System.Collections.Generic;

public class ComponentizedDalConfig
{
    public string SecurityProviderKey { get; set; }
    public string ExecuteReaderKey { get; set; }
    public string HistoryWriterKey { get; set; }

    public List<ComponentizedDalItem> DalComponents { get; set; } = new List<ComponentizedDalItem>();
}