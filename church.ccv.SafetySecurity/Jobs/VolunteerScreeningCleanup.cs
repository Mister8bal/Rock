using System;
using System.Collections.Generic;
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
            var rockContext = new RockContext();
            var vsService = new VolunteerScreeningService( rockContext );

            // get all volunteer screenings that have a workflow that is complete.
            var screenings = vsService.Queryable()
                .Where( vs => vs.Application_Workflow != null &&
                              !vs.Application_Workflow.CompletedDateTime.HasValue )
                .ToList();

            // loop through and remove any that are past the retention period and are still "Waiting for Applicant"...
            foreach ( var screening in screenings )
            {
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
            var rockContext = new RockContext();
            var workflowService = new WorkflowService( rockContext );

            // get all character reference workflows that are incomplete.
            var workflowsToDelete = workflowService.Queryable()
                .Where( w => w.CompletedDateTime == null &&
                             w.Status != "Completed" &&
                             w.WorkflowTypeId == 203 )
                .ToList();

            // loop through and remove any that are past the retention period.
            foreach ( var workflow in workflowsToDelete )
            {
                if ( workflow.CreatedDateTime < RockDateTime.Now.AddDays( -1 * ( int ) retentionPeriod ) )
                {
                    workflowService.Delete( workflow );
                    rockContext.SaveChanges();

                    count++;
                }
            }

            return count;
        }
    }
}
