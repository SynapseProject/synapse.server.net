#if NET461

using System;
using System.Net.Http.Headers;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;

using Microsoft.Win32.SafeHandles;


namespace Synapse.Common
{
    public class Impersonator : IDisposable
    {
        [DllImport( "advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode )]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
            int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

        [DllImport( "kernel32.dll", CharSet = CharSet.Auto )]
        public extern static bool CloseHandle(IntPtr handle);

        public WindowsIdentity Identity { get; set; }

        public SecureString Domain { get; set; } = Environment.UserDomainName.ToSecureString();
        public SecureString UserName { get; set; }
        public SecureString Password { get; set; }

        private SafeTokenHandle _safeTokenHandle;
        public bool IsStarted = false;

        [PermissionSet( SecurityAction.Demand, Name = "FullTrust" )]
        public Impersonator()
        {
            Identity = WindowsIdentity.GetCurrent();
        }

        [PermissionSet( SecurityAction.Demand, Name = "FullTrust" )]
        public Impersonator(SecureString username, SecureString password)
        {
            UserName = username;
            Password = password;

            Logon();
        }

        [PermissionSet( SecurityAction.Demand, Name = "FullTrust" )]
        public Impersonator(SecureString domain, SecureString username, SecureString password)
        {
            Domain = domain;
            UserName = username;
            Password = password;

            Logon();
        }

        [PermissionSet( SecurityAction.Demand, Name = "FullTrust" )]
        public Impersonator(WindowsIdentity winId)
        {
            Identity = winId;
        }

        public Impersonator(AuthenticationHeaderValue basicAuthHeader)
        {
            string userpass = basicAuthHeader.Parameter.Replace( "Basic ", "" ).Trim();
            byte[] bytes = Convert.FromBase64String( userpass );
            string decodedStr = Encoding.UTF8.GetString( bytes );
            string[] parts = decodedStr.Split( ':' );
            UserName = parts[0].ToSecureString();
            Password = parts[1].ToSecureString();

            Logon();
        }

        public void Logon()
        {
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            // Call LogonUser to obtain a handle to an access token. 
            bool returnValue = LogonUser( UserName.ToUnsecureString(), Domain.ToUnsecureString(), Password.ToUnsecureString(),
                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out _safeTokenHandle );

            if( false == returnValue )
            {
                int ret = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception( ret, $"LogonUser failed for use [{Domain}\\{Password}] with error code: {ret}" );
            }

            Identity = new WindowsIdentity( _safeTokenHandle.DangerousGetHandle() );
        }

        [PermissionSet( SecurityAction.Demand, Name = "FullTrust" )]
        public void Logoff()
        {
            if( _safeTokenHandle != null )
            {
                _safeTokenHandle.Close();
                _safeTokenHandle = null;
            }

            Identity?.Dispose();
            Identity = null;
        }

        public void Dispose()
        {
            Logoff();
        }

        public static WindowsIdentity WhoAmI()
        {
            return WindowsIdentity.GetCurrent();
        }
    }

    public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeTokenHandle()
            : base( true )
        {
        }

        [DllImport( "kernel32.dll" )]
        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs( UnmanagedType.Bool )]
        private static extern bool CloseHandle(IntPtr handle);

        protected override bool ReleaseHandle()
        {
            return CloseHandle( handle );
        }
    }
}

#endif