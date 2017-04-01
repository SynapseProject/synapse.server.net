using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Suplex.Security;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Services;
using Synapse.Services.Controller.Dal;


//namespace Synapse.Services.Controller.Dal { }
public partial class FileSystemDal : IControllerDal
{
    static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( FileSystemDal ).Assembly.Location )}";

    string _planPath = null;
    string _histPath = null;
    string _splxPath = null;

    SuplexDal _splxDal = null;

    //this is a stub feature
    static long PlanInstanceIdCounter = DateTime.Now.Ticks;

    public FileSystemDal()
    {
    }

    internal FileSystemDal(string basePath, bool processPlansOnSingleton = false, bool processActionsOnSingleton = true) : this()
    {
        if( string.IsNullOrWhiteSpace( basePath ) )
            basePath = CurrentPath;

        _planPath = $"{basePath}\\Plans\\";
        _histPath = $"{basePath}\\History\\";
        _splxPath = $"{basePath}\\Security\\";

        EnsurePaths();

        ProcessPlansOnSingleton = processPlansOnSingleton;
        ProcessActionsOnSingleton = processActionsOnSingleton;

        LoadSuplex();
    }


    public Dictionary<string, string> Configure(ISynapseDalConfig conifg)
    {
        if( conifg != null )
        {
            string s = YamlHelpers.Serialize( conifg.Config );
            FileSystemDalSettings fsds = YamlHelpers.Deserialize<FileSystemDalSettings>( s );

            _planPath = fsds.PlanFolderPath;
            _histPath = fsds.HistoryFolderPath;
            _splxPath = fsds.Security.FilePath;

            EnsurePaths();

            ProcessPlansOnSingleton = fsds.ProcessPlansOnSingleton;
            ProcessActionsOnSingleton = fsds.ProcessActionsOnSingleton;

            LoadSuplex();

            if( _splxDal == null && fsds.Security.IsRequired )
                throw new Exception( $"Security is required.  Could not load security file: {_splxPath}." );

            if( _splxDal != null )
            {
                _splxDal.LdapRoot = conifg.LdapRoot;
                _splxDal.GlobalExternalGroupsCsv = fsds.Security.GlobalExternalGroupsCsv;
            }
        }
        else
        {
            ConfigureDefaults();
        }

        Dictionary<string, string> props = new Dictionary<string, string>();
        string name = nameof( FileSystemDal );
        props.Add( name, CurrentPath );
        props.Add( $"{name} Plan path", _planPath );
        props.Add( $"{name} History path", _histPath );
        props.Add( $"{name} Security path", _splxPath );
        return props;
    }

    internal void ConfigureDefaults()
    {
        _planPath = $"{CurrentPath}\\Plans\\";
        _histPath = $"{CurrentPath}\\History\\";
        _splxPath = $"{CurrentPath}\\Security\\";

        EnsurePaths();

        ProcessPlansOnSingleton = false;
        ProcessActionsOnSingleton = true;

        LoadSuplex();
    }

    void EnsurePaths()
    {
        //GetFullPath tests below validate the paths are /complete/ paths.  IsPathRooted returns 'true'
        //in a few undesriable cases

        if( Path.GetFullPath( _planPath ) != _planPath )
            _planPath = Utilities.PathCombine( CurrentPath, _planPath, "\\" );

        if( Path.GetFullPath( _histPath ) != _histPath )
            _histPath = Utilities.PathCombine( CurrentPath, _histPath, "\\" );

        if( Path.GetFullPath( _splxPath ) != _splxPath )
            _splxPath = Utilities.PathCombine( CurrentPath, _splxPath, "\\" );

        Directory.CreateDirectory( _planPath );
        Directory.CreateDirectory( _histPath );
    }

    void LoadSuplex()
    {
        string splx = Utilities.PathCombine( _splxPath, "security.splx" );
        if( File.Exists( splx ) )
            _splxDal = new SuplexDal( splx );
    }


    public bool ProcessPlansOnSingleton { get; set; }
    public bool ProcessActionsOnSingleton { get; set; }


    public bool HasAccess(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute)
    {
        bool ok = false;
        try
        {
            _splxDal?.TrySecurityOrException( securityContext, planUniqueName, AceType.FileSystem, right, "Plan" );
            ok = true;
        }
        catch { }

        return ok;
    }

    public bool HasAccess(string securityContext, string planUniqueName, AceType aceType, object right)
    {
        bool ok = false;
        try
        {
            _splxDal?.TrySecurityOrException( securityContext, planUniqueName, aceType, right, "Plan" );
            ok = true;
        }
        catch { }

        return ok;
    }

    public void HasAccessOrException(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute)
    {
        _splxDal?.TrySecurityOrException( securityContext, planUniqueName, AceType.FileSystem, right, "Plan" );
    }

    public void HasAccessOrException(string securityContext, string planUniqueName, AceType aceType, object right)
    {
        _splxDal?.TrySecurityOrException( securityContext, planUniqueName, aceType, right, "Plan" );
    }


    public IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true)
    {
        if( string.IsNullOrEmpty( filter ) )
        {
            return Directory.GetFiles( _planPath, "*.yaml" ).Select( f => Path.GetFileNameWithoutExtension( f ) );
        }
        else
        {
            Regex regex = new Regex( filter, RegexOptions.IgnoreCase );

            if( !isRegexFilter )
            {
                foreach( char x in @"\+?|{[()^$.#" )
                    filter = filter.Replace( x.ToString(), @"\" + x.ToString() );
                regex = new Regex( string.Format( "^{0}$", filter.Replace( "*", ".*" ) ), RegexOptions.IgnoreCase );
            }

            if( !filter.EndsWith( ".yaml", StringComparison.OrdinalIgnoreCase ) )
            {
                filter = $@"{filter}.*\.yaml";
                regex = new Regex( filter, RegexOptions.IgnoreCase );
            }

            return Directory.GetFiles( _planPath ).Where( f => regex.IsMatch( f ) ).Select( f => Path.GetFileNameWithoutExtension( f ) );
        }
    }

    public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
    {
        return new long[] { 1, 2, 3 };
    }

    public Plan GetPlan(string planUniqueName)
    {
        //_splxDal?.TrySecurityOrException( securityContext, planUniqueName, AceType.FileSystem, FileSystemRight.Execute, "Plan" );

        string planFile = Utilities.PathCombine( _planPath, $"{planUniqueName}.yaml" );
        return YamlHelpers.DeserializeFile<Plan>( planFile );
    }

    public Plan CreatePlanInstance(string planUniqueName)
    {
        string planFile = Utilities.PathCombine( _planPath, $"{planUniqueName}.yaml" );
        Plan plan = YamlHelpers.DeserializeFile<Plan>( planFile );

        if( string.IsNullOrWhiteSpace( plan.UniqueName ) )
            plan.UniqueName = planUniqueName;
        plan.InstanceId = PlanInstanceIdCounter++;

        return plan;
    }

    public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
    {
        string planFile = Utilities.PathCombine( _histPath, $"{planUniqueName}_{planInstanceId}.yaml" );
        return YamlHelpers.DeserializeFile<Plan>( planFile );
    }

    public void UpdatePlanStatus(Plan plan)
    {
        PlanUpdateItem item = new PlanUpdateItem() { Plan = plan };

        if( ProcessPlansOnSingleton )
            PlanItemSingletonProcessor.Instance.Queue.Enqueue( item );
        else
            UpdatePlanStatus( item );
    }

    public void UpdatePlanStatus(PlanUpdateItem item)
    {
        try
        {
            YamlHelpers.SerializeFile( Utilities.PathCombine( _histPath, $"{item.Plan.UniqueName}_{item.Plan.InstanceId}.yaml" ),
                item.Plan, emitDefaultValues: true );
        }
        catch( Exception ex )
        {
            PlanItemSingletonProcessor.Instance.Exceptions.Enqueue( ex );

            if( item.RetryAttempts++ < 5 )
                PlanItemSingletonProcessor.Instance.Queue.Enqueue( item );
            else
                PlanItemSingletonProcessor.Instance.Fatal.Enqueue( ex );
        }
    }

    public void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem)
    {
        ActionUpdateItem item = new ActionUpdateItem()
        {
            PlanUniqueName = planUniqueName,
            PlanInstanceId = planInstanceId,
            ActionItem = actionItem
        };

        if( ProcessActionsOnSingleton )
            ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
        else
            UpdatePlanActionStatus( item );
    }

    public void UpdatePlanActionStatus(ActionUpdateItem item)
    {
        try
        {
            Plan plan = GetPlanStatus( item.PlanUniqueName, item.PlanInstanceId );
            bool ok = DalUtilities.FindActionAndReplace( plan.Actions, item.ActionItem );
            if( ok )
                YamlHelpers.SerializeFile( Utilities.PathCombine( _histPath, $"{plan.UniqueName}_{plan.InstanceId}.yaml" ), plan, emitDefaultValues: true );
            else
                throw new Exception( $"Could not find Plan.InstanceId = [{item.PlanInstanceId}], Action:{item.ActionItem.Name}.ParentInstanceId = [{item.ActionItem.ParentInstanceId}] in Plan outfile." );
        }
        catch( Exception ex )
        {
            ActionItemSingletonProcessor.Instance.Exceptions.Enqueue( ex );

            if( item.RetryAttempts++ < 5 )
                ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
            else
                ActionItemSingletonProcessor.Instance.Fatal.Enqueue( ex );
        }
    }
}