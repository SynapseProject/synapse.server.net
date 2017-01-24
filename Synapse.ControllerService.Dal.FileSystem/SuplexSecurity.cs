using System;
using System.Data;
using System.Data.SqlClient;

using Suplex.Forms;
using Suplex.Forms.ObjectModel.Api;
using Suplex.Forms.SecureManager;
using Suplex.Security;
using sg = Suplex.General;
using ss = Suplex.Security.Standard;

namespace Synapse.ControllerService.Dal
{
    public partial class SuplexDal
    {
        SuplexApiClient _splxApi = new SuplexApiClient();
        public string LdapRoot { get; set; }
        public string GlobalExternalGroupsCsv { get; set; }

        public SuplexStore _splxStore;

        public void LoadSuplexSecurity(string path)
        {
            _splxStore = _splxApi.LoadFile( path );
        }


        //public string GetSuplexSecurity(string uniqueName)
        //{
        //    SecurityLoadParameters slp = new SecurityLoadParameters()
        //    {
        //        ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
        //        User = this.GetSuplexUser( false )
        //    };

        //    DataSet ds = _splxApi.GetSecurity( uniqueName, slp.User, slp.ExternalGroupInfo );

        //    SerializationUtility ser = new SerializationUtility();
        //    return ser.SerializeSecurityToStringFromDataSet( ds );
        //}

        private ss.User GetSuplexUser(bool v)
        {
            return new ss.User( "steve", "steve" );
        }

        //public SuplexStore GetSuplexStore(string uniqueName)
        //{
        //    SecurityLoadParameters slp = new SecurityLoadParameters()
        //    {
        //        ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
        //        User = this.GetSuplexUser( false )
        //    };

        //    return _splxApi.GetSecurityStore( uniqueName, slp.User, slp.ExternalGroupInfo );
        //}

        /// <summary>
        /// Selects and loads security for the given UniqueName into a SplxRecordManager
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns>A loaded and resolved SplxRecordManager</returns>
        public SplxSecureManagerBase GetSecureManagerManagerSecurity(AceType aceType, string uniqueName, SecurityLoadParameters slp)
        {
            SplxSecureManagerBase sm = new SplxRecordManager() { UniqueName = uniqueName };
            if( aceType == AceType.FileSystem )
            {
                sm = new SplxFileSystemManager() { UniqueName = uniqueName };
            }

            if( slp == null )
            {
                slp = new SecurityLoadParameters()
                {
                    ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
                    User = this.GetSuplexUser( false )
                };
            }

            DataSet securityCache = sm.Security.Load( _splxStore, slp );

            return sm;
        }

        /// <summary>
        /// Tests security /and/ (RlsOwner /or/ RlsMask) for the given UniqueName and validates SecurityResults[AceType.Record, right].AccessAllowed
        /// </summary>
        /// <param name="uniqueName">The UniqueName for which to select security.</param>
        /// <param name="right">The RecordRight to test (used in error message).</param>
        /// <param name="assetType">The associated AssetType (used in error message).</param>
        /// <param name="rowOwnerId">The rlsOwner from the row.</param>
        /// <param name="rowRlsMask">The rlsMask from the row.</param>
        public ss.User TrySecurityOrException(string uniqueName, AceType aceType, object right,
            Guid? rowOwnerId = null, byte[] rowRlsMask = null, bool? allowOwnerOverride = null,
            ss.User user = null)
        {
            if( rowOwnerId == null )
                rowOwnerId = Guid.Empty;
            if( rowOwnerId != Guid.Empty && allowOwnerOverride == null )
                allowOwnerOverride = true;
            if( allowOwnerOverride == null )
                allowOwnerOverride = false;

            string exceptionMsg = $"You do not have {right} rights to this record.";
            SecurityLoadParameters slp = new SecurityLoadParameters()
            {
                ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
                User = user == null ? this.GetSuplexUser( true ) : user
            };

            SplxSecureManagerBase perms = this.GetSecureManagerManagerSecurity( aceType, uniqueName, slp );

            #region eval rls
            RowLevelSecurityHelper.EvalOption option = RowLevelSecurityHelper.EvalOption.None;
            if( rowOwnerId != Guid.Empty ) { option |= RowLevelSecurityHelper.EvalOption.Owner; }
            if( rowRlsMask != null ) { option |= RowLevelSecurityHelper.EvalOption.Mask; }

            RowLevelSecurityHelper rlsHelper = new RowLevelSecurityHelper()
            {
                RowOwnerId = rowOwnerId.Value,
                RowRlsMask = rowRlsMask,
                SecurityPrincipalId = slp.User.IdToGuid(),
                SecurityPrincipalRlsMask = slp.User.RlsMask,
                Option = option
            };

            perms.Security.EvalRowLevelSecurity( rlsHelper, aceType, new object[] { right }, allowOwnerOverride.Value );

            if( option != RowLevelSecurityHelper.EvalOption.None &&
                !perms.Security.Descriptor.SecurityResults[aceType, right].AccessAllowed )
            {
                exceptionMsg = "You do not have rights to this record.";
            }
            #endregion


            if( !perms.Security.Descriptor.SecurityResults[aceType, right].AccessAllowed )
            {
                throw new SecurityException( exceptionMsg );
            }

            return slp.User;
        }
    }

    public enum UpdateType
    {
        Record,
        Rls
    }
}