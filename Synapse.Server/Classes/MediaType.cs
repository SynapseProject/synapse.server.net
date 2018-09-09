using System;

namespace Synapse.Services
{
    public struct MediaType
    {
        public static readonly string ApplicationJson = "application/json";
        public static readonly string ApplicationXml = "application/xml";

        public static bool IsApplicationJson(string mediaType) { return mediaType.Equals( ApplicationJson, StringComparison.OrdinalIgnoreCase ); }
        public static bool IsApplicationXml(string mediaType) { return mediaType.Equals( ApplicationXml, StringComparison.OrdinalIgnoreCase ); }
    }
}