using System;
using Synapse.Common.Utilities;

namespace Synapse.Services
{
    public class AboutData : AboutBase
    {
        public SynapseServerConfig Config { get; set; }

        public override void GetFiles(bool asCsv = false)
        {
            if( asCsv )
                FilesCsv = FileEnumerator.EnumerateFilesToCsv( SynapseServerConfig.CurrentPath );
            else
                Files = FileEnumerator.EnumerateFiles( SynapseServerConfig.CurrentPath );
        }
    }
}