using System;
using System.Collections.Generic;
using System.Linq;

using Suplex.Security;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Services;
using Synapse.Services.Controller.Dal;


public partial class ComponentizedDal : IControllerDal
{
    IPlanSecurityProvider _planSecurityProvider = null;
    IPlanExecuteReader _planExecuteReader = null;
    IPlanHistoryWriter _planHistoryWriter = null;


    public object GetDefaultConfig()
    {
        const string fileSystem = "FileSystem";

        ComponentizedDalItem dalItem = new ComponentizedDalItem
        {
            Key = fileSystem,
            Type = "Synapse.Controller.Dal.FileSystem:FileSystemDal"
        };

        IControllerDal fsd = AssemblyLoader.Load<IControllerDal>( dalItem.Type, string.Empty );
        dalItem.Config = fsd.GetDefaultConfig();

        return new ComponentizedDalConfig
        {
            SecurityProviderKey = fileSystem,
            ExecuteReaderKey = fileSystem,
            HistoryWriterKey = fileSystem,

            DalComponents = new List<ComponentizedDalItem> { dalItem }
        };
    }

    public Dictionary<string, string> Configure(ISynapseDalConfig conifg)
    {
        ComponentizedDalConfig cdc = null;

        if( conifg != null )
        {
            string s = YamlHelpers.Serialize( conifg.Config );
            cdc = YamlHelpers.Deserialize<ComponentizedDalConfig>( s );
        }
        else
        {
            cdc = (ComponentizedDalConfig)GetDefaultConfig();
        }

        ComponentizedDalItem cdi = cdc.DalComponents.SingleOrDefault( r => r.Key.Equals( cdc.SecurityProviderKey, StringComparison.OrdinalIgnoreCase ) );
        if( cdi != null )
        {
            _planSecurityProvider = AssemblyLoader.Load<IPlanSecurityProvider>( cdi.Type, string.Empty );
            _planSecurityProvider.Configure( new ConfigWrapper { Config = cdi.Config, Type = cdi.Type } );
        }
        else
            throw new TypeLoadException( $"Could not load {cdi.Key}/{cdi.Type} for {nameof( IPlanSecurityProvider )}" );

        cdi = cdc.DalComponents.SingleOrDefault( r => r.Key.Equals( cdc.ExecuteReaderKey, StringComparison.OrdinalIgnoreCase ) );
        if( cdi != null )
        {
            _planExecuteReader = AssemblyLoader.Load<IPlanExecuteReader>( cdi.Type, string.Empty );
            _planExecuteReader.Configure( new ConfigWrapper { Config = cdi.Config, Type = cdi.Type } );
        }
        else
            throw new TypeLoadException( $"Could not load {cdi.Key}/{cdi.Type} for {nameof( IPlanExecuteReader )}" );

        cdi = cdc.DalComponents.SingleOrDefault( r => r.Key.Equals( cdc.HistoryWriterKey, StringComparison.OrdinalIgnoreCase ) );
        if( cdi != null )
        {
            _planHistoryWriter = AssemblyLoader.Load<IPlanHistoryWriter>( cdi.Type, string.Empty );
            _planHistoryWriter.Configure( new ConfigWrapper { Config = cdi.Config, Type = cdi.Type } );
        }
        else
            throw new TypeLoadException( $"Could not load {cdi.Key}/{cdi.Type} for {nameof( IPlanHistoryWriter )}" );


        string name = nameof( ComponentizedDal );
        Dictionary<string, string> props = new Dictionary<string, string>
        {
            { $"{name} SecurityProviderKey", cdc.SecurityProviderKey },
            { $"{name} ExecuteReaderKey", cdc.ExecuteReaderKey },
            { $"{name} HistoryWriterKey", cdc.HistoryWriterKey }
        };
        return props;
    }

    public bool HasAccess(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute)
    {
        return _planSecurityProvider.HasAccess( securityContext, planUniqueName, right );
    }

    public void HasAccessOrException(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute)
    {
        _planSecurityProvider.HasAccessOrException( securityContext, planUniqueName, right );
    }

    public IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true)
    {
        return _planExecuteReader.GetPlanList( filter, isRegexFilter );
    }

    public Plan GetPlan(string planUniqueName)
    {
        return _planExecuteReader.GetPlan( planUniqueName );
    }

    public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
    {
        return _planHistoryWriter.GetPlanInstanceIdList( planUniqueName );
    }

    public Plan CreatePlanInstance(string planUniqueName)
    {
        return CreatePlanInstance( planUniqueName );
    }

    public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
    {
        return _planHistoryWriter.GetPlanStatus( planUniqueName, planInstanceId );
    }

    public void UpdatePlanStatus(Plan plan)
    {
        _planHistoryWriter.UpdatePlanStatus( plan );
    }

    public void UpdatePlanStatus(PlanUpdateItem item)
    {
        _planHistoryWriter.UpdatePlanStatus( item );
    }

    public void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem)
    {
        _planHistoryWriter.UpdatePlanActionStatus( planUniqueName, planInstanceId, actionItem );
    }

    public void UpdatePlanActionStatus(ActionUpdateItem item)
    {
        _planHistoryWriter.UpdatePlanActionStatus( item );
    }
}