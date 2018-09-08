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
            if( !isRegexFilter )
            {
                foreach( char x in @"\+?|{[()^$.#" )
                    filter = filter.Replace( x.ToString(), @"\" + x.ToString() );
                filter = $@"{filter.Replace( "*", ".*" )}.*\.yaml$";
            }
            else if( !filter.EndsWith( ".yaml", StringComparison.OrdinalIgnoreCase ) )
            {
                if( filter.EndsWith( "$" ) )
                    filter = $@"{filter.Remove( filter.Length - 1 )}\.yaml$";
                else
                    filter = $@"{filter}.*\.yaml$";
            }

            Regex regex = new Regex( filter, RegexOptions.IgnoreCase );

            return Directory.GetFiles( _planPath ).Where( f => regex.IsMatch( Path.GetFileName( f ) ) )
                .Select( f => Path.GetFileNameWithoutExtension( f ) );
        }
    }

    public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
    {
        Regex regex = new Regex( $@"^{planUniqueName}(_\d+\.yaml)$" );
        IEnumerable<string> files = Directory.GetFiles( _histPath )
            .Where( f => regex.IsMatch( Path.GetFileName( f ) ) )
            .Select( f => Path.GetFileNameWithoutExtension( f ) );

        List<long> ids = new List<long>();
        foreach( string file in files )
        {
            Match m = Regex.Match( file, @"_(?<instanceId>\d+)" );
            string iid = m.Groups["instanceId"].Value;
            if( !string.IsNullOrWhiteSpace( iid ) )
                ids.Add( long.Parse( iid ) );
        }

        return ids;
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