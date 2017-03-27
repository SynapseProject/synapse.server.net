using System;
using System.Collections.Generic;

namespace Synapse.Services.Controller.Dal
{
    public class FileSystemDalSettings
    {
        public string PlanFolderPath { get; set; } = "\\Plans";
        public string HistoryFolderPath { get; set; } = "\\History";
        public bool ProcessPlansOnSingleton { get; set; } = false;
        public bool ProcessActionsOnSingleton { get; set; } = true;

        public SecuritySettings Security { get; set; } = new SecuritySettings();
    }

    public class SecuritySettings
    {
        public string FilePath { get; set; }= "\\Security";
        public bool IsRequired { get; set; } = true;
        public bool ValidateSignature { get; set; } = false;
        public string SignaturePublicKeyFile { get; set; }
        public string GlobalExternalGroupsCsv { get; set; } = "Everyone";
    }
}