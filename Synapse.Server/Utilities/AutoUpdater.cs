using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Synapse.Services
{
    public class AutoUpdater
    {
        public static readonly string CurrentPath = $@"{Path.GetDirectoryName( typeof( AutoUpdater ).Assembly.Location )}\AutoUpdater";

        public static List<AutoUpdaterMessage> Update()
        {
            List<AutoUpdaterMessage> result = null;

            string logfile = $"{DateTime.Now.Ticks}_{Path.GetFileNameWithoutExtension( Path.GetTempFileName() )}.log";
            string logPath = Path.Combine( CurrentPath, logfile );
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = Path.Combine( CurrentPath, @"Synapse.Server.AutoUpdater.exe" ),
                WorkingDirectory = CurrentPath,
                Arguments = $"update {logPath}",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                result = AutoUpdaterMessage.LoadOneMessage( DateTime.Now, $"Starting update, logging to [{logfile}]." );
                Process p = Process.Start( psi );
            }
            catch( Exception ex )
            {
                result = AutoUpdaterMessage.LoadOneMessage( DateTime.Now, ex.Message );
            }

            return result;
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

        public static List<AutoUpdaterMessage> FetchLogList()
        {
            DirectoryInfo directory = new DirectoryInfo( CurrentPath );
            IEnumerable<FileInfo> logs = directory.GetFiles( "*.log", SearchOption.TopDirectoryOnly )
                .OrderByDescending( f => f.LastWriteTime );

            List<AutoUpdaterMessage> logFiles = new List<AutoUpdaterMessage>();
            foreach( FileInfo log in logs )
                logFiles.Add( new AutoUpdaterMessage() { TimeStamp = log.LastWriteTime, Message = log.Name } );

            return logFiles;
        }
    }

    public class AutoUpdaterMessage
    {
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }

        public static List<AutoUpdaterMessage> LoadOneMessage(DateTime timeStamp, string message)
        {
            return new List<AutoUpdaterMessage>
            {
                { new AutoUpdaterMessage(){ TimeStamp = timeStamp, Message = message } }
            };
        }

        public static List<AutoUpdaterMessage> LoadFile(string path)
        {
            string[] loglines = File.ReadAllLines( path );
            List<AutoUpdaterMessage> msgs = new List<AutoUpdaterMessage>();
            foreach( string l in loglines )
            {
                string[] parts = l.Split( '|' );
                if( parts.Length > 1 )
                {
                    DateTime dt = new DateTime( 1970, 1, 1 );
                    DateTime.TryParse( parts[0], out dt );
                    msgs.Add( new AutoUpdaterMessage()
                    {
                        TimeStamp = dt,
                        Message = parts[1]
                    } );
                }
            }

            return msgs;
        }
    }
}