using System;
using System.Data;

using Suplex.Forms.SecureManager;
using Suplex.Security;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace Synapse.Services.Controller.Dal
{
	public static class Utilities
	{

		static public string ToBase64String(this string str)
		{
			byte[] bytes = System.Text.ASCIIEncoding.ASCII.GetBytes( str );
			return System.Convert.ToBase64String( bytes ); ;
		}

		static public string FromBase64String(this string str)
		{
			byte[] bytes = System.Convert.FromBase64String( str );
			return System.Text.ASCIIEncoding.ASCII.GetString( bytes ); ;
		}


		public static T ParseEnum<T>(object data)
		{
			return (T)Enum.Parse( typeof( T ), data.ToString(), true );
		}
		public static ParseResult<T> TryParseEnum<T>(object data) where T : struct
		{
			ParseResult<T> r = new ParseResult<T>();
			T result;
			if( Enum.TryParse<T>( data.ToString(), true, out result ) )
			{
				r.Success = true;
				r.Result = result;
			}
			return r;
		}

		//public static T IsDBNullOrValue<T>(this DataRow r, string field, T altValue)
		//{
		//    T value = default( T );
		//    value = r[field] == Convert.DBNull ? altValue : (T)r[field];
		//    return value;
		//}

		/// <summary>
		/// Gets the column value as string.
		/// </summary>
		/// <param name="r">The r.</param>
		/// <param name="field">The field.</param>
		/// <returns>System.String.</returns>
		public static string GetColumnValueAsString(this DataRow r, string field)
		{
			return r.IsDBNullOrValue<string>( field, default( string ) );
		}
		/// <summary>
		/// Gets the column value as int.
		/// </summary>
		/// <param name="r">The r.</param>
		/// <param name="field">The field.</param>
		/// <returns>System.Int32.</returns>
		public static int GetColumnValueAsInt(this DataRow r, string field)
		{
			return r.IsDBNullOrValue<int>( field, default( int ) );
		}
		/// <summary>
		/// Gets the column value as bool.
		/// </summary>
		/// <param name="r">The r.</param>
		/// <param name="field">The field.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		public static bool GetColumnValueAsBool(this DataRow r, string field)
		{
			return r.IsDBNullOrValue<bool>( field, default( bool ) );
		}
		/// <summary>
		/// Gets the column value as DateTime.
		/// </summary>
		/// <param name="r">The r.</param>
		/// <param name="field">The field.</param>
		/// <returns>System.DateTime.</returns>
		public static DateTime GetColumnValueAsDateTime(this DataRow r, string field)
		{
			return r.IsDBNullOrValue<DateTime>( field, default( DateTime ) );
		}
		/// <summary>
		/// Gets the column value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="r">The r.</param>
		/// <param name="field">The field.</param>
		/// <returns>T.</returns>
		public static T GetColumnValue<T>(this DataRow r, string field)
		{
			return r.IsDBNullOrValue<T>( field, default( T ) );
		}
		/// <summary>
		/// Gets the column value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="r">The r.</param>
		/// <param name="field">The field.</param>
		/// <param name="altValue">The alt value.</param>
		/// <returns>T.</returns>
		public static T GetColumnValue<T>(this DataRow r, string field, T altValue)
		{
			return r.IsDBNullOrValue<T>( field, default( T ) );
		}
		public static T IsDBNullOrValue<T>(this DataRow r, string field)
		{
			return r.IsDBNullOrValue<T>( field, default( T ) );
		}
		public static T IsDBNullOrValue<T>(this DataRow r, string field, T altValue)
		{
			T value = default( T );

			if( typeof( T ).IsEnum )
			{
				value = r[field] == Convert.DBNull ? altValue : Suplex.General.MiscUtils.ParseEnum<T>( r[field].ToString() );
			}
			else
			{
				value = r[field] == Convert.DBNull ? altValue : (T)r[field];
			}

			return value;
		}


		public static DateTimeOffset ToDateTimeOffset(this DateTime datetime, TimeSpan offset)
		{
			return new DateTimeOffset( datetime, offset );
		}


        /// <summary>
        /// A wrapper on Path.Combine to correct for fronting/trailing backslashes that otherwise fail in Path.Combine.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>The combined path</returns>
        public static string PathCombine(params string[] paths)
        {
            if( paths.Length > 0 )
            {
                int last = paths.Length - 1;
                for( int c = 0; c <= last; c++ )
                {
                    if( c != 0 )
                        paths[c] = paths[c].Trim( Path.DirectorySeparatorChar );

                    if( c != last )
                        paths[c] = string.Format( "{0}\\", paths[c] );
                }
            }
            else
            {
                return string.Empty;
            }

            return Path.Combine( paths );
        }


        //public static string UnwindException(string method, Exception ex)
        //{
        //    StringBuilder msg = new StringBuilder();
        //    msg.Append( $"An error occurred in: {method}|{ex.Message}" );

        //    if( ex.InnerException != null )
        //    {
        //        Stack<Exception> exceptions = new Stack<Exception>();
        //        exceptions.Push( ex.InnerException );

        //        while( exceptions.Count > 0 )
        //        {
        //            Exception e = exceptions.Pop();
        //            msg.Append( $"|{e.Message}" );

        //            if( e.InnerException != null )
        //                exceptions.Push( e.InnerException );
        //        }
        //    }

        //    return msg.ToString();
        //}


        //public static void DigestSecurityDescriptor(this Container container, SplxFileSystemManager security)
        //{
        //          container.Rights |= security.HasTakeOwnershipRight ? FileSystemRight.TakeOwnership : 0;
        //          container.Rights |= security.HasReadPermissionsRight ? FileSystemRight.ReadPermissions : 0;
        //          container.Rights |= security.HasChangePermissionsRight ? FileSystemRight.ChangePermissions : 0;
        //          container.Rights |= security.HasListRight ? FileSystemRight.List : 0;
        //          container.Rights |= security.HasReadRight ? FileSystemRight.Read : 0;
        //          container.Rights |= security.HasCreateRight ? FileSystemRight.Create : 0;
        //          container.Rights |= security.HasWritedRight ? FileSystemRight.Write : 0;
        //          container.Rights |= security.HasDeleteRight ? FileSystemRight.Delete : 0;
        //          container.Rights |= security.HasExecuteRight ? FileSystemRight.Execute : 0;
        //}
    }

    public class ParseResult<T>
	{
		public ParseResult()
		{
			this.Success = false;
		}
		public bool Success { get; internal set; }
		public T Result { get; internal set; }
	}
}