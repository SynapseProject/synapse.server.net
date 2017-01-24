using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

//SecurityPrincipal/User/Group/Ace are copycats for Suplex objects, the intent being not to have a dependency on Suplex.Core.dll
//see extension methods in Api
namespace Synapse.ControllerService.Dal
{
	public class SuplexSecurityPrincipal : INotifyPropertyChanged
	{
		private string _id = string.Empty;
		private string _name = string.Empty;
		private string _description = string.Empty;
		private bool _isLocal = false;
		private BitArray _mask = ContainerSecurityRecord.GetEmptyRlsBitArray();

		public string Id
		{
			get { return _id; }
			set
			{
				if( _id != value )
				{
					_id = value;
					OnPropertyChanged( "Id" );
				}
			}
		}

		public string Name
		{
			get { return _name; }
			set
			{
				if( _name != value )
				{
					_name = value;
					OnPropertyChanged( "Name" );
				}
			}
		}

		public string Description
		{
			get { return _description; }
			set
			{
				if( _description != value )
				{
					_description = value;
					OnPropertyChanged( "Description" );
				}
			}
		}

		public bool IsLocal
		{
			get { return _isLocal; }
			set
			{
				if( _isLocal != value )
				{
					_isLocal = value;
					OnPropertyChanged( "IsLocal" );
				}
			}
		}

		public BitArray Mask
		{
			get { return _mask; }
			set
			{
				if( _mask != value )
				{
					_mask = value;
					this.OnPropertyChanged( "Mask" );
				}
			}
		}

		public string MaskValue
		{
			get
			{	//todo: 10082012, bug: _maskSize not working from svc (switched to _mask.Length)
				int[] mask = new int[_mask.Length / 32];	//32 bits per int
				_mask.CopyTo( mask, 0 );
				return string.Join<int>( ",", mask );
			}
			set
			{
				string[] values = value.Split( ',' );
				int[] masks = new int[values.Length];	//todo: 10082012, bug: (_maskSize / 32) not working from svc
				for( int i = 0; i < values.Length; i++ )
				{
					if( values[i] == int.MinValue.ToString().TrimStart( '-' ) )
					{
						masks[i] = int.MinValue;
					}
					else
					{
						masks[i] = Int32.Parse( values[i] );
					}
				}

				_mask = new BitArray( masks );
				this.OnPropertyChanged( "MaskValue" );
			}
		}

		#region
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if( PropertyChanged != null )
			{
				PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}
		#endregion
	}

	public class SuplexUserRecord : SuplexSecurityPrincipal
	{
		private bool _isAnonymous = false;
		public bool IsAnonymous
		{
			get { return _isAnonymous; }
			set
			{
				if( _isAnonymous != value )
				{
					_isAnonymous = value;
					OnPropertyChanged( "IsAnonymous" );
				}
			}
		}
	}

	public class SuplexGroupRecord : SuplexSecurityPrincipal
	{
		private List<SuplexGroupRecord> _groups = null;

		public List<SuplexGroupRecord> Groups
		{
			get
			{
				if( _groups == null ) { _groups = new List<SuplexGroupRecord>(); }
				return _groups;
			}
			set
			{
				_groups = value;
			}
		}
	}

	public class SuplexAce : INotifyPropertyChanged
	{
		public string SecurityPrincipal { get; set; }
		public string Right { get; set; }
		public bool Allowed { get; set; }

		#region
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if( PropertyChanged != null )
			{
				PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}
		#endregion
	}

	public class SuplexRlsSummaryRecord : INotifyPropertyChanged
	{
		string _rlsOwner = null;
		string _rlsOwnerName = null;
		bool _isDirty = false;

		public SuplexRlsSummaryRecord()
		{
			this.Dacl = new ObservableCollection<SuplexAce>();
			this.Members = new ObservableCollection<SuplexGroupRecord>();
			this.NonMembers = new ObservableCollection<SuplexGroupRecord>();
		}

		public string RlsOwner
		{
			get { return _rlsOwner; }
			set
			{
				if( _rlsOwner != value )
				{
					_rlsOwner = value;
					this.IsDirty = true;
					OnPropertyChanged( "RlsOwner" );
				}
			}
		}
		public Guid RlsOwnerToGuid() { return Guid.Parse( this.RlsOwner ); }

		public string RlsOwnerName
		{
			get { return _rlsOwnerName; }
			set
			{
				if( _rlsOwnerName != value )
				{
					_rlsOwnerName = value;
					this.IsDirty = true;
					OnPropertyChanged( "RlsOwnerName" );
				}
			}
		}

		public bool IsDirty
		{
			get { return _isDirty; }
			set
			{
				if( _isDirty != value )
				{
					_isDirty = value;
					OnPropertyChanged( "IsDirty" );
				}
			}
		}

		public ObservableCollection<SuplexAce> Dacl { get; set; }

		public ObservableCollection<SuplexGroupRecord> Members { get; set; }
		public ObservableCollection<SuplexGroupRecord> NonMembers { get; set; }

		#region
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if( PropertyChanged != null )
			{
				PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}
		#endregion
	}

	public class ContainerSecurityRecord
	{
        public const int RlsMaskSizeBits = 2048;         // Next Values : 2048 and 256
        public const int RlsMaskSizeBytes = 256;        //--> RlsMaskSizeBits / 8
        public static byte[] GetEmptyRlsMask() { return new byte[RlsMaskSizeBytes]; }
		public static BitArray GetEmptyRlsBitArray() { return new BitArray( RlsMaskSizeBits ); }

		public int BucketId { get; set; }
        public Guid SuplexUiElementId { get; set; }
		public string RlsOwner { get; set; }
		public Guid RlsOwnerToGuid() { return Guid.Parse( this.RlsOwner ); }
		public byte[] RlsMask { get; set; }
        public List<PermissionSet> Permissions { get; set; }

        public static byte[] CalculateMask(List<byte[]> masks)
        {
            BitArray arr = new BitArray(RlsMaskSizeBits);
            byte[] mask = new byte[RlsMaskSizeBytes];

            foreach (byte[] m in masks)
            {
                //sometimes groups have an invalid mask length, which is bug from somewhere else.
                //this is a hack/workaround to make sure they're RlsMaskSizeBits bits
                BitArray gMask = new BitArray(m);
                if (m.Length < RlsMaskSizeBits)
                {
                    byte[] groupMask = new byte[RlsMaskSizeBytes];
                    m.CopyTo(groupMask, 0);
                    gMask = new BitArray(groupMask);
                }
                arr.Or(gMask);
            }
            arr.CopyTo(mask, 0);

            return mask;
        }

/*
        public static byte[] CalculateMask(IList groups)
		{
			BitArray arr = new BitArray( RlsMaskSizeBits );
			byte[] mask = new byte[RlsMaskSizeBytes];

			foreach( SuplexGroupRecord g in groups )
			{
				//sometimes groups have an invalid mask length, which is bug from somewhere else.
				//this is a hack/workaround to make sure they're RlsMaskSizeBits bits
				if( g.Mask.Length < RlsMaskSizeBits )
				{
					byte[] groupMask = new byte[RlsMaskSizeBytes];
					g.Mask.CopyTo( groupMask, 0 );
					g.Mask = new BitArray( groupMask );
				}
				arr.Or( g.Mask );
			}
			arr.CopyTo( mask, 0 );

			return mask;
		}
 */
    }
}