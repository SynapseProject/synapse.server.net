using System;
using System.Web;

namespace Synapse.Services
{
    internal static class Utilities
    {
        public static string ToUrlEncodedOrNull(this string value, string urlParameterName)
        {
            return !string.IsNullOrWhiteSpace( value ) ? $"{urlParameterName}={HttpUtility.UrlEncode( value )}" : null;
        }
    }
}