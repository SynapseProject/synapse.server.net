using System;
using System.Collections.Generic;
using System.IO;

using Suplex.Security.AclModel;
using Suplex.Security.DataAccess;
using Suplex.Security.Utilities.ActiveDirectory;
using Suplex.Security.WebApi;

namespace Synapse.Services.Controller.Dal
{
    public partial class SuplexDal
    {
        ISuplexDal _dal = null;
        FileSystemWatcher filestoreWatcher;

        public SuplexDal() { }

        public SuplexDal(string connectionString, SuplexDalConnectionType connectionType)
        {
            Connect( connectionString, connectionType );
        }

        public void Connect(string connectionString, SuplexDalConnectionType connectionType)
        {
            switch( connectionType )
            {
                case SuplexDalConnectionType.File:
                {
                    InitFileConnection( connectionString );
                    break;
                }
                case SuplexDalConnectionType.Http:
                {
                    _dal = new SuplexSecurityHttpApiClient( connectionString );
                    break;
                }
            }
        }

        void InitFileConnection(string filestorePath)
        {
            string folder = Path.GetDirectoryName( filestorePath );
            string file = Path.GetFileName( filestorePath );
            filestoreWatcher = new FileSystemWatcher( folder, file );
            filestoreWatcher.Changed += FilestoreWatcher_Changed;
            filestoreWatcher.EnableRaisingEvents = true;

            _dal = FileSystemDal.LoadFromYamlFile( filestorePath );

            IsMemoryStore = true;
        }

        void FilestoreWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            int attempts = 0;
            while( attempts++ < 5 )
                try { _dal = FileSystemDal.LoadFromYamlFile( e.FullPath ); }
                catch { System.Threading.Thread.Sleep( 100 ); }
        }

        public void LoadStoreData(string storeData)
        {
            _dal = FileSystemDal.LoadFromYaml( storeData );

            IsMemoryStore = true;
        }

        /// <summary>
        /// The Suplex store is loaded in memory (vs remote service)
        /// </summary>
        public bool IsMemoryStore { get; internal set; }
        public string LdapRoot { get; set; }
        public string GlobalExternalGroupsCsv { get; set; }
        bool HasGlobalExternalGroupsCsv { get { return !string.IsNullOrWhiteSpace( GlobalExternalGroupsCsv ); } }


        /// <summary>
        /// Tests security /and/ (RlsOwner /or/ RlsMask) for the given UniqueName and validates SecurityResults[AceType.Record, right].AccessAllowed
        /// </summary>
        /// <param name="uniqueName">The UniqueName for which to select security.</param>
        /// <param name="right">The RecordRight to test (used in error message).</param>
        /// <param name="assetType">The associated AssetType (used in error message).</param>
        /// <param name="rowOwnerId">The rlsOwner from the row.</param>
        /// <param name="rowRlsMask">The rlsMask from the row.</param>
        public ISecureObject TrySecurityOrException(string userName, string uniqueName, FileSystemRight right = FileSystemRight.Execute, string assetType = "Plan",
            byte[] rowRlsMask = null, Guid? rowOwnerId = null, bool allowOwnerOverride = false)
        {
            List<string> groupMembership = new List<string>();

            if( IsMemoryStore )
                groupMembership = ActiveDirectoryUtility.GetGroupMembershipSimple( userName, LdapRoot );

            if( HasGlobalExternalGroupsCsv )
            {
                string[] extGroups = GlobalExternalGroupsCsv.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                groupMembership.AddRange( extGroups );
            }

            ISecureObject secureObject = _dal.EvalSecureObjectSecurity( uniqueName, userName, groupMembership );

            if( !secureObject.Security.Results.GetByTypeRight( right ).AccessAllowed )
                throw new Exception( $"You do not have {right.ToString()} rights to {assetType} record {uniqueName}." );

            return secureObject;
        }
    }
}