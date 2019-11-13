// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

using church.ccv.Datamart.Model;

namespace church.ccv.Utility
{
    /// <summary>
    /// Sends eRA potential loss email
    /// </summary>
    [SystemEmailField( "Email Template", required: true )]
    [DisallowConcurrentExecution]
    public class SendPotentialEraLossEmails : IJob
    {
        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public SendPotentialEraLossEmails()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            int emailsSent = 0;

            var rockContext = new RockContext();

            // get system email
            Guid? systemEmailGuid = dataMap.GetString( "EmailTemplate" ).AsGuidOrNull();

            SystemEmailService emailService = new SystemEmailService( rockContext );

            SystemEmail systemEmail = null;
            if ( systemEmailGuid.HasValue )
            {
                systemEmail = emailService.Get( systemEmailGuid.Value );
            }

            if ( systemEmail == null )
            {
                // no email specified, so nothing to do
                return;
            }

            DatamartEraLossService eraLossService = new DatamartEraLossService( rockContext );

            // result string for job, add blank line at start to insert the final job result
            StringBuilder result = new StringBuilder();
            result.AppendLine();

            var lossRecipients = eraLossService.Queryable()
                                    .Where( e => e.Processed == true && e.SendEmail == true && e.Sent == false ).ToList();

            foreach (var lossRecipient in lossRecipients)
            {
                // Whether the email sent or not mark as sent
                lossRecipient.Sent = true;
                rockContext.SaveChanges();

                var family = new GroupService( rockContext ).Get( lossRecipient.FamilyId );
                if ( family == null )
                {
                    // no family, skip to next item
                    result.AppendLine( string.Format( "Family {0} does not exist", lossRecipient.FamilyId ) );

                    continue;
                }

                var headOfHouse = family.Members.AsQueryable().HeadOfHousehold();
                if ( headOfHouse == null )
                {
                    // no head of house hold, skip to next item
                    result.AppendLine( string.Format( "{0} has no head of household", family.Id ) );

                    continue;
                }

                var campus = family.Campus;
                if ( campus == null )
                {
                    // no campus, skip to next item
                    result.AppendLine( string.Format( "{0} ({1}) has no campus", family.Name, family.Id ) );

                    continue;
                }

                var campusPastor = campus.LeaderPersonAlias.Person;
                if ( campusPastor == null )
                {
                    // no campus pastor, skip to next item
                    result.AppendLine( string.Format( "{0} ({1}) has no campus pastor", family.Name, family.Id ) );

                    continue;
                }

                var familyNeighborhoodId = new DatamartFamilyService( rockContext ).Queryable().Where( a => a.FamilyId == family.Id ).Select( a => a.NeighborhoodId ).FirstOrDefault() ?? 0;
                var neighborhoodPastorId = new DatamartNeighborhoodService( rockContext ).Queryable().Where( a => a.NeighborhoodId == familyNeighborhoodId ).Select( a => a.NeighborhoodPastorId ).FirstOrDefault() ?? 0;
                var neighborhoodPastor = new PersonService( rockContext ).Get( neighborhoodPastorId );
                if ( neighborhoodPastor == null )
                {
                    // no neighborhood pastor, skip to next item
                    result.AppendLine( string.Format( "{0} ({1}) has no neighborhood pastor", family.Name, family.Id ) );

                    continue;
                }

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                mergeFields.Add( "Person", headOfHouse );
                mergeFields.Add( "Pastor", neighborhoodPastor );
                mergeFields.Add( "Campus", campus );
                mergeFields.Add( "CampusPastor", campusPastor );

                // Create email to send
                var emailMessage = new RockEmailMessage();
                emailMessage.AddRecipient( new RecipientData( headOfHouse.Email, mergeFields ) );
                emailMessage.AppRoot = Rock.Web.Cache.GlobalAttributesCache.Read( rockContext ).GetValue( "ExternalApplicationRoot" );
                emailMessage.FromEmail = neighborhoodPastor.Email;
                emailMessage.FromName = neighborhoodPastor.FullName;
                emailMessage.Subject = systemEmail.Subject;
                emailMessage.Message = systemEmail.Body.ResolveMergeFields( mergeFields );
                emailMessage.CreateCommunicationRecord = true;
                emailMessage.Send();
                emailsSent++;

                rockContext.SaveChanges();
            }

            result.Insert( 0, string.Format( "{0} {1} sent", emailsSent, "email".PluralizeIf( emailsSent != 1 ) ) );

            context.Result = result.ToString();
        }
    }
}
