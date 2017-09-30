using System;
using System.Collections.Generic;
using Synapse.Common.Utilities;

namespace Synapse.Services
{
    public class AboutData
    {
        public SynapseServerConfig Config { get; set; }

        public List<FileData> Files { get; set; } = null;
        public string FilesCsv { get; set; } = null;

        public void GetFiles(bool asCsv = false)
        {
            if( asCsv )
                FilesCsv = FileEnumerator.EnumerateFilesToCsv( SynapseServerConfig.CurrentPath );
            else
                Files = FileEnumerator.EnumerateFiles( SynapseServerConfig.CurrentPath );
        }
    }
}