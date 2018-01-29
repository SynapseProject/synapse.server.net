using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Synapse.Common;

namespace Synapse.Services
{
    public class Log4netUtil
    {
        public static List<AutoUpdaterMessage> FetchLogList()
        {
            string currentPath = Log4netHelpers.GetLogFileFolder( "SynapseServer" );
            DirectoryInfo directory = new DirectoryInfo( currentPath );
            IEnumerable<FileInfo> logs = directory.GetFiles( "*.log", SearchOption.TopDirectoryOnly )
                .OrderByDescending( f => f.LastWriteTime );

            List<AutoUpdaterMessage> logFiles = new List<AutoUpdaterMessage>();
            foreach( FileInfo log in logs )
                logFiles.Add( new AutoUpdaterMessage() { TimeStamp = log.LastWriteTime, Message = log.Name } );

            return logFiles;
        }

        public static string GetLogfilePath(string logfile)
        {
            string currentPath = Log4netHelpers.GetLogFileFolder( "SynapseServer" );
            DirectoryInfo directory = new DirectoryInfo( currentPath );
            return Path.Combine( currentPath, logfile );
        }
    }
}