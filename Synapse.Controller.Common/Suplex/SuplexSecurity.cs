using System;
using System.Collections.Generic;
using System.Data;
using io = System.IO;

using Suplex.Forms;
using Suplex.Forms.ObjectModel.Api;
using splxApi = Suplex.Forms.ObjectModel.Api;
using Suplex.Forms.SecureManager;
using Suplex.Security;
using sg = Suplex.General;
using ss = Suplex.Security.Standard;
using Suplex.Data;

namespace Synapse.Services.Controller.Dal
{
    public partial class SuplexDal
	{
        SuplexApiClient _splxApi = new SuplexApiClient();
        SuplexStore _splxStore = null;
        io.FileSystemWatcher filestoreWatcher;

        DataAccessor _da;


        public SuplexDal(string filestorePath)
        {
            string folder = io.Path.GetDirectoryName( filestorePath );
            string file = io.Path.GetFileName( filestorePath );
            filestoreWatcher = new io.FileSystemWatcher( folder, file );
            filestoreWatcher.Changed += FilestoreWatcher_Changed;
            filestoreWatcher.EnableRaisingEvents = true;

            _splxStore = _splxApi.LoadFile( filestorePath );
            IsFileStore = true;
        }

        private void FilestoreWatcher_Changed(object sender, io.FileSystemEventArgs e)
        {
            int attempts = 0;
            while( attempts++ < 5 )
            {
                try
                {
                    _splxStore = _splxApi.LoadFile( e.FullPath );
                }
                catch { System.Threading.Thread.Sleep( 100 ); }
            }
        }

        public bool IsFileStore { get; internal set; }


        public string ContainerRootUniqueName { get; set; } = "SynapseRoot";
        public string ContainerUniqueNamePrefix { get; set; }
        public string LdapRoot { get; set; }
        public string GlobalExternalGroupsCsv { get; set; }

        //stub method
        private string WhoAmI()
        {
            return "steve";
        }


        internal ss.User GetSuplexUser(bool resolve)
		{
			return this.GetSuplexUser( resolve, true );
		}

		internal ss.User GetSuplexUser(bool resolve, bool resolveRls)
		{
			string userName = WhoAmI();
			ss.User user = new ss.User()
			{
				Name = userName,
				CreateUnresolvedName = true
			};
			if( resolve )
			{
				user.DataAccessor = _da;

				//this is just for the option of avoiding the AD lookup
				if( resolveRls )
				{
					ExternalGroupInfo egi = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv );
					egi.BuildGroupsList( userName );

					sg.SqlResult result = user.ResolveByName( true, egi.GroupsList );
					//sometimes multithreaded requests to create a new user get too close together, causing a dup-username error
					//this is a cheap retry
					if( result.SqlException != null )
					{
						if( result.SqlException.Number == 2601 ) //2601 == duplicate value error
						{
							System.Threading.Thread.Sleep( 500 );
							result = user.ResolveByName( true, egi.GroupsList );
						}

						//if err not duplicate or it still didn't work in retry, throw the exeption
						if( result.SqlException != null )
						{
							throw result.SqlException;
						}
					}
				}
				else
				{
					user.ResolveByName();
				}
			}

			if( user.RlsMask == null )
			{
				user.RlsMask = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			}

			return user;
		}

        public SuplexUserRecord GetCurrentSuplexUser()
		{
			ss.User user = this.GetSuplexUser( true );
			return user.FromSuplexNative();
		}

		public SuplexUserRecord GetSuplexUserByName(string name)
		{
			ss.User user = new ss.User()
			{
				Name = name.FromBase64String(),
				CreateUnresolvedName = false,
				DataAccessor = _da
			};
			user.ResolveByName();

			return user.FromSuplexNative();
		}

		public SuplexUserRecord GetSuplexResolvedUserByName(string name)
		{
			ss.User user = new ss.User()
			{
				Name = name,
				CreateUnresolvedName = true,
				DataAccessor = _da
			};
			user.ResolveByName();

			return user.FromSuplexNative();
		}

		public string GetSuplexSecurity(string uniqueName)
		{
			SecurityLoadParameters slp = new SecurityLoadParameters()
			{
				ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
				User = this.GetSuplexUser( false )
			};

			DataSet ds = _splxApi.GetSecurity( uniqueName, slp.User, slp.ExternalGroupInfo );

			SerializationUtility ser = new SerializationUtility();
			return ser.SerializeSecurityToStringFromDataSet( ds );
		}

		public SuplexStore GetSuplexStore(string uniqueName)
		{
			SecurityLoadParameters slp = new SecurityLoadParameters()
			{
				ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
				User = this.GetSuplexUser( false )
			};

			return _splxApi.GetSecurityStore( uniqueName, slp.User, slp.ExternalGroupInfo );
		}

		/// <summary>
		/// Selects and loads security for the given UniqueName into a SplxRecordManager
		/// </summary>
		/// <param name="uniqueName"></param>
		/// <returns>A loaded and resolved SplxRecordManager</returns>
		SplxSecureManagerBase GetSecureManagerSecurity(AceType aceType, string uniqueName, SecurityLoadParameters slp)
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

            DataSet securityCache = IsFileStore ?
                sm.Security.Load( _splxStore, slp ) :
                sm.Security.Load( _splxApi, slp );

            return sm;
		}

        /// <summary>
        /// Selects and loads security for the given UniqueName into a SplxRecordManager
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns>A loaded and resolved SplxRecordManager</returns>
        SplxSecureManagerBase GetSecureManagerSecurityRecurseUp(AceType aceType, string uniqueName, SecurityLoadParameters slp)
		{
			string rootUniqueName = ContainerRootUniqueName;
			SecureContainer root = new SecureContainer() { UniqueName = rootUniqueName };

            #region setup SecurityLoadParameters, load ExternalGroupInfo
            if( slp == null )
                slp = new SecurityLoadParameters()
                {
                    ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
                    User = this.GetSuplexUser( false )
                };

            ExternalGroupInfo egi =
                new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv );
            egi.BuildGroupsList( slp.User.Name );
            #endregion

            SecureContainer ctrl = root;
            SplxSecureManagerBase context = null;

            #region IsFileStore = true
            if( IsFileStore )
            {
                ISecureControl c = new SplxRecordManager() { UniqueName = uniqueName };
                if( aceType == AceType.FileSystem )
                    c = new SplxFileSystemManager() { UniqueName = uniqueName };

                splxApi.UIElement uie = _splxStore.UIElements.GetByUniqueNameRecursive( uniqueName );


                SecureContainer parent = null;
                IObjectModel parentObj = uie.ParentObject;
                while( parentObj != null )
                {
                    uniqueName = ((splxApi.UIElement)parentObj).UniqueName;
                    parent = new SecureContainer() { UniqueName = uniqueName };
                    parent.Children.Add( c );
                    c = parent;

                    parentObj = parentObj.ParentObject;
                }

                parent.Security.Load( _splxStore, slp );
            }
            #endregion
            #region IsFileStore = false
            else
            {
                DataSet ds = _da.GetDataSet( "splx.splx_dal_sel_security_byuserbyuie_up",
                    new System.Collections.sSortedList(
                    "@UIE_UNIQUE_NAME", uniqueName,
                    "@SPLX_USER_ID", slp.User.Id,
                    "@EXTERNAL_GROUP_LIST", egi.GroupsList ) );

                _da.NameTablesFromCompositeSelect( ref ds );

                //todo, when suplex is ready
                //DataSet ds = _splxApi.GetSecurity( rootUniqueName, slp.User, slp.ExternalGroupInfo, future:recurseUp );

                DataTable acl = ds.Tables["AclInfo"];
                DataRow[] rows = acl.Select( string.Format( "UIE_UNIQUE_NAME = '{0}'", rootUniqueName ) );
                if( rows.Length > 0 )
                {
                    rows = acl.Select( string.Format( "UIE_PARENT_ID = '{0}'", rows[0]["SPLX_UI_ELEMENT_ID"] ) );
                }

                while( rows.Length > 0 )
                {
                    string un = rows[0]["UIE_UNIQUE_NAME"].ToString();
                    if( un.StartsWith( ContainerUniqueNamePrefix ) )
                    {
                        context = new SplxRecordManager() { UniqueName = un };
                        if( aceType == AceType.FileSystem )
                            context = new SplxFileSystemManager() { UniqueName = un };

                        ctrl.Children.Add( context );
                    }
                    else
                    {
                        SecureContainer child = new SecureContainer() { UniqueName = un };
                        ctrl.Children.Add( child );
                        ctrl = child;
                    }

                    rows = acl.Select( string.Format( "UIE_PARENT_ID = '{0}'", rows[0]["SPLX_UI_ELEMENT_ID"] ) );
                }

                root.Security.Load( ds, slp );
            }
            #endregion


            return context;
		}

		/// <summary>
		/// Tests security for the given UniqueName and validates SecurityResults[AceType.Record, right].AccessAllowed
		/// </summary>
		/// <param name="uniqueName">The UniqueName for which to select security.</param>
		/// <param name="right">The RecordRight to test (used in error message).</param>
		/// <param name="assetType">The associated AssetType (used in error message).</param>
		public SuplexSecurityInfo TrySecurityOrException(string uniqueName, AceType aceType, object right, string assetType, bool recurseUp = true)
		{
			return this.TrySecurityOrException( uniqueName, aceType, right, assetType, Guid.Empty, null, false, recurseUp );
		}
        /// <summary>
        /// Tests security /and/ RlsOwner for the given UniqueName and validates SecurityResults[AceType.Record, right].AccessAllowed
        /// </summary>
        /// <param name="uniqueName">The UniqueName for which to select security.</param>
        /// <param name="right">The RecordRight to test (used in error message).</param>
        /// <param name="assetType">The associated AssetType (used in error message).</param>
        /// <param name="rowOwnerId">The rlsOwner from the row.</param>
        public SuplexSecurityInfo TrySecurityOrException(string uniqueName, AceType aceType, object right, string assetType, Guid rowOwnerId, bool recurseUp = true)
		{
			return this.TrySecurityOrException( uniqueName, aceType, right, assetType, rowOwnerId, null, true, recurseUp );
		}
        /// <summary>
        /// Tests security /and/ RlsMask for the given UniqueName and validates SecurityResults[AceType.Record, right].AccessAllowed
        /// </summary>
        /// <param name="uniqueName">The UniqueName for which to select security.</param>
        /// <param name="right">The RecordRight to test (used in error message).</param>
        /// <param name="assetType">The associated AssetType (used in error message).</param>
        /// <param name="rowRlsMask">The rlsMask from the row.</param>
        public SuplexSecurityInfo TrySecurityOrException(string uniqueName, AceType aceType, object right, string assetType, byte[] rowRlsMask, bool recurseUp = true)
		{
			return this.TrySecurityOrException( uniqueName, aceType, right, assetType, Guid.Empty, rowRlsMask, false, recurseUp );
		}
        /// <summary>
        /// Tests security /and/ (RlsOwner /or/ RlsMask) for the given UniqueName and validates SecurityResults[AceType.Record, right].AccessAllowed
        /// </summary>
        /// <param name="uniqueName">The UniqueName for which to select security.</param>
        /// <param name="right">The RecordRight to test (used in error message).</param>
        /// <param name="assetType">The associated AssetType (used in error message).</param>
        /// <param name="rowOwnerId">The rlsOwner from the row.</param>
        /// <param name="rowRlsMask">The rlsMask from the row.</param>
        public SuplexSecurityInfo TrySecurityOrException(string uniqueName, AceType aceType, object right, string assetType, Guid rowOwnerId, byte[] rowRlsMask, bool allowOwnerOverride, bool recurseUp = true)
		{
			string exceptionMsg = this.GetNoRightsErrorMessage( right, assetType );
			SecurityLoadParameters slp = new SecurityLoadParameters()
			{
				ExternalGroupInfo = new ExternalGroupInfo( LdapRoot, true, GlobalExternalGroupsCsv ),
				User = this.GetSuplexUser( true )
			};

			SplxSecureManagerBase perms = recurseUp ?
				GetSecureManagerSecurityRecurseUp( aceType, uniqueName, slp ) :
				GetSecureManagerSecurity( aceType, uniqueName, slp );

			#region eval rls
			RowLevelSecurityHelper.EvalOption option = RowLevelSecurityHelper.EvalOption.None;
			if( rowOwnerId != Guid.Empty ) { option |= RowLevelSecurityHelper.EvalOption.Owner; }
			if( rowRlsMask != null ) { option |= RowLevelSecurityHelper.EvalOption.Mask; }

			RowLevelSecurityHelper rlsHelper = new RowLevelSecurityHelper()
			{
				RowOwnerId = rowOwnerId,
				RowRlsMask = rowRlsMask,
				SecurityPrincipalId = slp.User.IdToGuid(),
				SecurityPrincipalRlsMask = slp.User.RlsMask,
				Option = option
			};

			perms.Security.EvalRowLevelSecurity( rlsHelper, aceType, new object[] { right }, allowOwnerOverride );

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

			return new SuplexSecurityInfo( slp.User, perms );
		}

		/// <summary>
		/// Tests security for (RlsOwner /or/ RlsMask).
		/// </summary>
		/// <param name="rlsOwner">The rlsOwner from the row.</param>
		/// <param name="rlsMask">The rlsMask from the row.</param>
		/// <param name="user">The current security principal.</param>
		void TryRowLevelSecurityOrException(Guid rlsOwner, byte[] rlsMask, ss.User user)
		{
			RowLevelSecurityHelper.EvalOption option = RowLevelSecurityHelper.EvalOption.None;
			if( rlsOwner != Guid.Empty ) { option |= RowLevelSecurityHelper.EvalOption.Owner; }
			if( rlsMask != null ) { option |= RowLevelSecurityHelper.EvalOption.Mask; }

			RowLevelSecurityHelper rlsHelper = new RowLevelSecurityHelper()
			{
				RowOwnerId = rlsOwner,
				RowRlsMask = rlsMask,
				SecurityPrincipalId = user.IdToGuid(),
				SecurityPrincipalRlsMask = user.RlsMask,
				Option = option
			};
			rlsHelper.Eval();

			bool ok = rlsHelper.IsRowOwner || rlsHelper.HasMaskMatch;
			if( !ok )
			{
				throw new SecurityException( "You do not have rights to this record." );
			}
		}

		string GetNoRightsErrorMessage(object right, string assetType)
		{
			return string.Format( "You do not have {0} rights to {1} records.", right.ToString(), assetType );
		}

		public SuplexRlsSummaryRecord GetSuplexRls(int id, string source, string uieUniqueName)
		{
			string sp = string.Empty;
			string parm = string.Empty;

			switch( source.ToLower() )
			{
				case "packagegroup":
				{
					sp = "TPTR.api_package_group_sel_rls";
					parm = "@PACKAGE_GROUP_ID";
					break;
				}
				case "request":
				{
					sp = "TPTR.api_release_sel_rls";
					parm = "@RELEASE_ID";
					break;
				}
			}

			DataSet ds = _da.GetDataSet( sp, new System.Collections.sSortedList( parm, id, "@uie_unique_name", uieUniqueName ) );
			_da.NameTablesFromCompositeSelect( ref ds );

			SuplexRlsSummaryRecord rlsSummary = new SuplexRlsSummaryRecord();

			GroupFactory groupFactory = new GroupFactory();
			foreach( DataRow r in ds.Tables["GroupMembers"].Rows )
			{
				rlsSummary.Members.Add( groupFactory.CreateObject( r ).FromSuplexNative() );
			}
			foreach( DataRow r in ds.Tables["GroupNonMembers"].Rows )
			{
				rlsSummary.NonMembers.Add( groupFactory.CreateObject( r ).FromSuplexNative() );
			}

			AceFactory aceFactory = new AceFactory();
			foreach( DataRow r in ds.Tables["Aces"].Rows )
			{
				rlsSummary.Dacl.Add( aceFactory.CreateObject( r ).FromSuplexNative( r ) );
			}

			rlsSummary.RlsOwner = Guid.Empty.ToString();
			rlsSummary.RlsOwnerName = "Unknown";
			DataTable owner = ds.Tables["Owner"];
			if( owner.Rows.Count > 0 )
			{
				rlsSummary.RlsOwner = owner.Rows[0]["RLS_OWNER"].ToString();
				rlsSummary.RlsOwnerName = owner.Rows[0]["RLS_OWNER_NAME"].ToString();
			}

			return rlsSummary;
		}

		//for setting container Rls
		public void UpdateSuplexRls(ContainerSecurityRecord rls, string assetType)
		{
		}
	}

	public enum UpdateType
	{
		Record,
		RecordOverride,
		Rls
	}

	public static class SuplexExtensions
	{
		public static SuplexGroupRecord FromSuplexNative(this Group g)
		{
			return new SuplexGroupRecord()
			{
				Id = g.Id,
				Name = g.Name,
				Description = g.Description,
				IsLocal = g.IsLocal,
				MaskValue = g.MaskValue
			};
		}

		public static SuplexAce FromSuplexNative(this Suplex.Forms.ObjectModel.Api.AccessControlEntryBase ace, DataRow r)
		{
			return new SuplexAce()
			{
				SecurityPrincipal = r["ACE_TRUSTEE_USER_GROUP_NAME"].ToString(),
				Right = ace.Right.ToString(),
				Allowed = ace.Allowed
			};
		}

		public static SuplexUserRecord FromSuplexNative(this ss.User user)
		{
			return new SuplexUserRecord()
			{
				Id = user.Id,
				Name = user.Name,
				Description = user.Description,
				IsLocal = user.IsLocal,
				IsAnonymous = user.IsAnonymous
			};
		}
	}

	class SecureContainer : ISecureContainer
	{
		private Suplex.Data.DataAccessLayer _dal = new Suplex.Data.DataAccessLayer();
		private SecurityAccessor _sa = null;
		private SecurityResultCollection _sr = null;
		//private ValidationContainerAccessor _va = null;
		private List<ISecureControl> _children = new List<ISecureControl>();

		public SecureContainer(AceType aceType = AceType.FileSystem)
		{
			_sa = new SecurityAccessor( this, aceType );
			_sr = _sa.Descriptor.SecurityResults;
		}

		public string UniqueName { get; set; }

		public Suplex.Data.DataAccessLayer DataAccessLayer { get { return _dal; } set { _dal = value; } }

		#region Security Implementation
		public List<ISecureControl> Children { get { return _children; } }

		public SecurityAccessor Security
		{
			get { return _sa; }
		}

		public string GetSecurityState()
		{
			return null;
		}

		public virtual System.Collections.IEnumerable GetChildren()
		{
			return _children;
		}

		public void ApplySecurity()
		{
			//no-op
		}
		#endregion
	}

	public class SuplexSecurityInfo
	{
		public SuplexSecurityInfo(ss.User user, SplxSecureManagerBase security)
		{
			User = user;
			Security = security;
		}
		public ss.User User { get; private set; }
		public SplxSecureManagerBase Security { get; private set; }
	}
}