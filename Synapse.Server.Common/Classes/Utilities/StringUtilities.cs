using System;
using System.Runtime.InteropServices;
using System.Security;


namespace Synapse.Common
{
    public static class StringUtilities
    {
        public static string ToUnsecureString(this SecureString secureValue)
        {
            if( secureValue == null )
                throw new ArgumentNullException( nameof( secureValue ) );

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode( secureValue );
                return Marshal.PtrToStringUni( unmanagedString );
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode( unmanagedString );
            }
        }

        public static SecureString ToSecureString(this string value)
        {
            if( value == null )
                throw new ArgumentNullException( nameof( value ) );

            unsafe
            {
                fixed ( char* valueChars = value )
                {
                    SecureString secureValue = new SecureString( valueChars, value.Length );
                    secureValue.MakeReadOnly();
                    return secureValue;
                }
            }
        }
    }
}