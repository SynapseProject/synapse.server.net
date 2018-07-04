using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Synapse.Common;

namespace Synapse.Services
{
    public class Log4netUtil
    {
        public static List<AutoUpdaterMessage> FetchLogList()
        {
            List<AutoUpdaterMessage> logFiles = new List<AutoUpdaterMessage>();

            string currentPath = Log4netHelpers.GetLogFileFolder( "SynapseServer" );
            if( !string.IsNullOrWhiteSpace( currentPath ) )
            {
                DirectoryInfo directory = new DirectoryInfo( currentPath );
                IEnumerable<FileInfo> logs = directory.GetFiles( "*.log", SearchOption.TopDirectoryOnly )
                    .OrderByDescending( f => f.LastWriteTime );

                foreach( FileInfo log in logs )
                    logFiles.Add( new AutoUpdaterMessage() { TimeStamp = log.LastWriteTime, Message = log.Name } );
            }

            return logFiles;
        }

        public static string GetLogfilePath(string logfile)
        {
            string currentPath = Log4netHelpers.GetLogFileFolder( "SynapseServer" );
            if( !string.IsNullOrWhiteSpace( currentPath ) )
            {
                DirectoryInfo directory = new DirectoryInfo( currentPath );
                return Path.Combine( currentPath, logfile );
            }
            else
                return null;
        }
    }
}