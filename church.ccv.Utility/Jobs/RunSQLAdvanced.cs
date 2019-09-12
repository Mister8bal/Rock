
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace church.ccv.Utility
{
    /// <summary>
    /// Job to run quick SQL queries on a schedule.  This advanced version will also return messages and print statements when the job is complete.
    /// </summary>
    [ValueListField( "Stored Procedures", "The stored procedures to run.  Each entry needs to be formatted as EXEC [storedProcName]. Note Regular SQL can be used too.", true, "", "Stored Proc", "", "", "General", 1 )]
    [IntegerField( "Command Timeout", "Maximum amount of time (in seconds) to wait for the SQL Query to complete. Leave blank to use the SQL default (30 seconds).", false, 180, "General", 1, "CommandTimeout" )]
    [DisallowConcurrentExecution]
    public class RunSQLAdvanced : IJob
    {
        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public RunSQLAdvanced()
        {
        }

        /// <summary>
        /// Job that will run quick SQL queries on a schedule.
        /// 
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            List<string> storedProcs = dataMap.GetString( "StoredProcedures" ).Split( '|').ToList();
            StringBuilder sb = new StringBuilder();

            foreach ( string query in storedProcs )
            {
                if ( query.IsNotNullOrWhiteSpace() )
                {
                    // run a SQL query to do something
                    int? commandTimeout = dataMap.GetString( "CommandTimeout" ).AsIntegerOrNull();
                    try
                    {
                        // start timer
                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();
                        sb.AppendLine( "Started '" + query + "' at " + RockDateTime.Now.ToString( "MM/dd/yyyy hh:mm:ss tt" ) );
                        context.UpdateLastStatusMessage( sb.ToString() );

                        // run command
                        int rows = DbService.ExecuteCommand( query, System.Data.CommandType.Text, null, commandTimeout );

                        // stop timer
                        stopWatch.Stop();
                        TimeSpan ts = stopWatch.Elapsed;
                        string elapsedTime = String.Format( "{0:00}:{1:00}:{2:00}.{3:00}",
                                            ts.Hours, ts.Minutes, ts.Seconds,
                                            ts.Milliseconds / 10 );

                        sb.AppendLine( "Finished in " + elapsedTime );
                        sb.AppendLine();
                        context.UpdateLastStatusMessage( sb.ToString() );
                    }
                    catch ( System.Exception ex )
                    {
                        HttpContext context2 = HttpContext.Current;
                        ExceptionLogService.LogException( ex, context2 );
                        throw;
                    }
                }
            }
        }
    }
}
