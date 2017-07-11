﻿using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Text;
using System.Net.Http.Headers;


namespace Synapse.Services
{
    public class Impersonator
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
            int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public extern static bool CloseHandle(IntPtr handle);

        public WindowsIdentity Identity { get; set; }
        public WindowsImpersonationContext Context { get; set; }

        public string Domain { get; set; } = System.Environment.UserDomainName;
        public string UserName { get; set; }
        public string Password { get; set; }

        private SafeTokenHandle safeTokenHandle;
        private bool isStarted = false;

        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        public Impersonator()
        {
            Identity = WindowsIdentity.GetCurrent();
        }

        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        public Impersonator(String username, String password)
        {
            UserName = username;
            Password = password;
            Logon();
        }

        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        public Impersonator(String domain, String username, String password)
        {
            Domain = domain;
            UserName = username;
            Password = password;
            Logon();
        }

        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        public Impersonator(WindowsIdentity winId)
        {
            Identity = winId;
        }

        public Impersonator(AuthenticationHeaderValue basicAuthHeader)
        {
            SynapseServer.Logger.Debug( "Impersonating With Basic Authentication Header" );
            String userpass = basicAuthHeader.Parameter.Replace( "Basic ", "" );
            byte[] bytes = Convert.FromBase64String( userpass );
            String decodedStr = Encoding.UTF8.GetString( bytes );
            SynapseServer.Logger.Debug( $"AuthHeader >> {decodedStr}" );
            String[] parts = decodedStr.Split( ':' );
            UserName = parts[0];
            Password = parts[1];
            Logon();
        }

        public void Logon()
        {
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            if (Context != null)
                Stop();

            // Call LogonUser to obtain a handle to an access token. 
            bool returnValue = LogonUser(UserName, Domain, Password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out safeTokenHandle);

            if (false == returnValue)
            {
                int ret = Marshal.GetLastWin32Error();
                Console.WriteLine("LogonUser failed with error code : {0}", ret);
                throw new System.ComponentModel.Win32Exception(ret);
            }

            Identity = new WindowsIdentity(safeTokenHandle.DangerousGetHandle());
        }

        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        public void Start()
        {
            if ( !isStarted )
            {
                isStarted = true;
                Context = Identity.Impersonate();
            }
        }

        public void Stop()
        {
            if ( isStarted )
            {
                isStarted = false;
                Context.Undo();
//                Context.Dispose();
//                Context = null;
            }
        }

        public static WindowsIdentity WhoAmI()
        {
            return WindowsIdentity.GetCurrent();
        }
    }

    public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeTokenHandle()
            : base(true)
        {
        }

        [DllImport("kernel32.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }
    }

}
