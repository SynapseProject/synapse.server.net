using System;
using System.Reflection;

namespace Synapse.Services
{
    public interface IAuthenticationProvider
    {
        void ConfigureBasicAuthentication(string ldapRoot, string domain, bool requireSsl = true);
    }

    public class BasicAuthenticationConfig
    {
        public string LdapRoot { get; set; }
        public string Domain { get; set; }
        public bool RequireSsl { get; set; } = true;
    }

    public class AuthenticationProviderUtil
    {
        public static IAuthenticationProvider GetInstance(string assemblyName, string className, params object[] args)
        {
            Assembly a = Assembly.Load( assemblyName );
            Type t = a.GetType( className, true );
            return Activator.CreateInstance( t, args ) as IAuthenticationProvider;
        }
    }
}