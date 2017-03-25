using System;
using System.Collections.Generic;

namespace Synapse.Services.Controller.Dal
{
    public class FileSystemDalSettings
    {
        public bool RequireSecurity { get; set; }
        public bool ValidateSecuritySignature { get; set; }
        public string SecuritySignaturePublicKeyFile { get; set; }
        public string GlobalExternalGroupsCsv { get; set; }
    }
}