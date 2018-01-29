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

        public static List<AutoUpdaterMessage> FetchLog(string logfile = null)
        {
            if( string.IsNullOrWhiteSpace( logfile ) )
            {
                DirectoryInfo directory = new DirectoryInfo( CurrentPath );
                logfile = directory.GetFiles( "*.log", SearchOption.TopDirectoryOnly )
                             .OrderByDescending( f => f.LastWriteTime )
                             .First().Name;
            }

            string logPath = Path.Combine( CurrentPath, logfile );
            if( File.Exists( logPath ) )
                return AutoUpdaterMessage.LoadFile( logPath );
            else
                return AutoUpdaterMessage.LoadOneMessage( DateTime.Now, $"Could not find logfile {logfile}" );
        }
    }
}