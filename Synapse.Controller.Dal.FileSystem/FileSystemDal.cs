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


    public object GetDefaultConfig()
    {
        return new FileSystemDalConfig();
    }


    public Dictionary<string, string> Configure(ISynapseDalConfig conifg)
    {
        if( conifg != null )
        {
            string s = YamlHelpers.Serialize( conifg.Config );
            FileSystemDalConfig fsds = YamlHelpers.Deserialize<FileSystemDalConfig>( s );

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

        string name = nameof( FileSystemDal );
        Dictionary<string, string> props = new Dictionary<string, string>
        {
            { name, CurrentPath },
            { $"{name} Plan path", _planPath },
            { $"{name} History path", _histPath },
            { $"{name} Security path", _splxPath }
        };
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
}