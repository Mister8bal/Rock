using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using church.ccv.SafetySecurity.Model;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.SafetySecurity.Jobs
{
    [DisallowConcurrentExecution]
    [IntegerField( "Days to Keep Incomplete Screenings and Character References.", "The number of days to keep incomplete volunteer screenings.", true, 60, "", 0, "RetentionPeriod" )]
    class VolunteerScreeningCleanup : IJob
    {
        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public VolunteerScreeningCleanup()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            // get the job map
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var retentionPeriod = dataMap.GetInt( "RetentionPeriod" );  

            int screeningsDeleted = DeleteExpiredVolunteerScreenings( retentionPeriod );
            int referencesDeleted = DeleteExpiredCharacterReferences( retentionPeriod );

            context.UpdateLastStatusMessage( string.Format( "{0} Volunteer Screenings deleted. {1} Character References deleted.", screeningsDeleted.ToString(), referencesDeleted.ToString() ) );
        }

        /// <summary>
        /// Deletes the expired volunteer screenings.
        /// </summary>
        /// <param name="retentionPeriod">The retention period.</param>
        /// <returns></returns>
        public int DeleteExpiredVolunteerScreenings( int retentionPeriod )
        {
            int count = 0;
            List<int> screeningIds = new List<int>();
            
            // get all volunteer screenings that have a workflow that is complete.
            using ( var rockContext = new RockContext() )
            {
                screeningIds = new VolunteerScreeningService( rockContext ).Queryable()
                        .AsNoTracking()
                        .Where( vs => vs.Application_Workflow != null &&
                                      !vs.Application_Workflow.CompletedDateTime.HasValue )
                        .Select( vs => vs.Id )
                        .ToList(); 
            }

            // loop through and remove any that are past the retention period and are still "Waiting for Applicant"...
            foreach ( var screeningId in screeningIds )
            {
                using ( var rockContext = new RockContext() )
                {
                    var vsService = new VolunteerScreeningService( rockContext );
                    var screening = vsService.Get( screeningId );
                    if ( screening.CreatedDateTime < RockDateTime.Now.AddDays( -1 * ( int ) retentionPeriod ) )
                    {
                        var workflow = screening.Application_Workflow;
                        if ( workflow != null )
                        {
                            string state = VolunteerScreening.GetState( workflow.CreatedDateTime, workflow.ModifiedDateTime, workflow.Status );

                            if ( state == "Waiting for Applicant to Complete" )
                            {
                                vsService.Delete( screening );
                                rockContext.SaveChanges();

                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }


        /// <summary>
        /// Deletes the expired character references.
        /// </summary>
        /// <param name="retentionPeriod">The retention period.</param>
        /// <returns></returns>
        public int DeleteExpiredCharacterReferences( int retentionPeriod )
        {
            int count = 0;
            List<int> workflowIds = new List<int>();

            // get all character reference workflows that are incomplete.
            using ( var rockContext = new RockContext() )
            {
                workflowIds = new WorkflowService( rockContext ).Queryable()
                        .AsNoTracking()
                        .Where( w => w.CompletedDateTime == null &&
                                     w.Status != "Completed" &&
                                     w.WorkflowTypeId == 203 )
                        .Select( w => w.Id )
                        .ToList(); 
            }

            // loop through and remove any that are past the retention period.
            foreach ( var workflowId in workflowIds )
            {
                using ( var rockContext = new RockContext() )
                {
                    var workflowService = new WorkflowService( rockContext );
                    var workflow = workflowService.Get( workflowId );
                    if ( workflow.CreatedDateTime < RockDateTime.Now.AddDays( -1 * ( int ) retentionPeriod ) )
                    {
                        workflowService.Delete( workflow );
                        rockContext.SaveChanges();

                        count++;
                    } 
                }
            }

            return count;
        }
    }
}
