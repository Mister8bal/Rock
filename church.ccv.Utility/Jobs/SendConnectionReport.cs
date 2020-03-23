
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;

namespace church.ccv.Utility
{
    /// <summary>
    /// Creates workflows for first visit calls
    /// </summary>
    [SystemEmailField( "Email Template", "The system email to use.", true, "", Order = 1 )]
    [LinkedPage( "Connection Report Page", "The page of the connection report.", true, Order = 2 )]
    [TextField( "Development Email List", "Email addresses of people that receive the development connection report.", false, "", "Email", Order = 3 )]
    [TextField( "Anthem Email List", "Email addresses of people that receive the Anthem connection report.", false, "", "Email", Order = 4 )]
    [TextField( "Avondale Email List", "Email addresses of people that receive the Avondale connection report.", false, "", "Email", Order = 5 )]
    [TextField( "Chandler Email List", "Email addresses of people that receive the Chandler connection report.", false, "", "Email", Order = 6)]
    [TextField( "East Valley Email List", "Email addresses of people that receive the East Valley connection report.", false, "", "Email", Order = 7 )]
    [TextField( "Midtown Email List", "Email addresses of people that receive the Midtown connection report.", false, "", "Email", Order = 8 )]
    [TextField( "North Phoenix Email List", "Email addresses of people that receive the North Phoenix connection report.", false, "", "Email", Order = 9)]
    [TextField( "Peoria Email List", "Email addresses of people that receive the Peoria connection report.", false, "", "Email", Order = 10 )]
    [TextField( "Scottsdale Email List", "Email addresses of people that receive the Scottsdale connection report.", false, "", "Email", Order = 11 )]
    [TextField( "Surprise Email List", "Email addresses of people that receive the Surprise connection report.", false, "", "Email", Order = 12 )]
    [TextField( "Verrado Email List", "Email addresses of people that receive the Verrado connection report.", false, "", "Email", Order = 13)]

    [DisallowConcurrentExecution]
    public class SendConnectionReport : IJob
    {
        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public SendConnectionReport()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            var rockContext = new RockContext();

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            Guid? systemEmailGuid = dataMap.GetString( "EmailTemplate" ).AsGuidOrNull();
            Guid? pageGuid = dataMap.GetString( "ConnectionReportPage" ).AsGuidOrNull();
            List<string> devEmailList = dataMap.GetString( "DevelopmentEmailList" ).SplitDelimitedValues().ToList();
            List<string> anthemEmailList = dataMap.GetString( "AnthemEmailList" ).SplitDelimitedValues().ToList();
            List<string> avondaleEmailList = dataMap.GetString( "AvondaleEmailList" ).SplitDelimitedValues().ToList();
            List<string> chandlerEmailList = dataMap.GetString( "ChandlerEmailList" ).SplitDelimitedValues().ToList();
            List<string> eastvalleyEmailList = dataMap.GetString( "EastValleyEmailList" ).SplitDelimitedValues().ToList();
            List<string> midtownEmailList = dataMap.GetString( "MidtownEmailList" ).SplitDelimitedValues().ToList();
            List<string> northphoenixEmailList = dataMap.GetString( "NorthPhoenixEmailList" ).SplitDelimitedValues().ToList();
            List<string> peoriaEmailList = dataMap.GetString( "PeoriaEmailList" ).SplitDelimitedValues().ToList();
            List<string> scottsdaleEmailList = dataMap.GetString( "ScottsdaleEmailList" ).SplitDelimitedValues().ToList();
            List<string> surpriseEmailList = dataMap.GetString( "SurpriseEmailList" ).SplitDelimitedValues().ToList();
            List<string> verradoEmailList = dataMap.GetString( "VerradoEmailList" ).SplitDelimitedValues().ToList();

            // get connection report page
            var connectionPageId = new PageService( rockContext )
                .Queryable().Where( p => p.Guid == pageGuid )
                .Select( p => p.Id ).FirstOrDefault();
   
            // if campus email list is not empty, pass in campus id, campus name and campus email list to SendCampusEmailList function. Do this for every list.  
            if (devEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 100, "All Connections", devEmailList);
            }

            if (anthemEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 8, "Anthem Campus", anthemEmailList);
            }

            if (avondaleEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 9, "Avondale Campus", avondaleEmailList);
            }

            if (chandlerEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 11, "Chandler Campus", chandlerEmailList);
            }

            if (eastvalleyEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 7, "East Valley Campus", eastvalleyEmailList);
            }

            if (midtownEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 10, "Midtown Campus", midtownEmailList);
            }

            if (northphoenixEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 13, "North Phoenix Campus", northphoenixEmailList);
            }

            if (peoriaEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 1, "Peoria Campus", peoriaEmailList);
            }

            if (scottsdaleEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 6, "Scottsdale Campus", scottsdaleEmailList);
            }

            if (surpriseEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 5, "Surprise Campus", surpriseEmailList);
            }

            if (verradoEmailList.Any())
            {
                SendCampusEmailList(rockContext, systemEmailGuid, connectionPageId, 14, "Verrado Campus", verradoEmailList);
            }

            context.Result = "Emails sent.";
        }

        void SendCampusEmailList(RockContext rockContext, Guid? systemEmailGuid, int connectionPageId, int campusId, string campusName, List<string> campusEmailList)
        {
            // timeframes
            var sevenDaysAgo = RockDateTime.Now.AddDays( -7 );
            var thirtyDaysAgo = RockDateTime.Now.AddDays( -30 );

            // get system email
            SystemEmail systemEmail = null;
            if ( systemEmailGuid.HasValue )
            {
                SystemEmailService emailService = new SystemEmailService( rockContext );
                systemEmail = emailService.Get( systemEmailGuid.Value );
            }

            var mergeFields = new Dictionary<string, object>();
            mergeFields.Add("Title", campusName);
            mergeFields.Add("CampusId", campusId);

            var connectionStatuses = new ConnectionRequestService(rockContext)
                .Queryable()
                .Where(r => r.CampusId == campusId)
                .Where(r => r.ConnectionOpportunity.ConnectionTypeId == 1)
                .Where(r => r.ConnectionState == ConnectionState.Active)
                .Where(r =>
                        (r.ConnectionStatus.Id == 1 && r.ConnectionRequestActivities.Max(a => a.CreatedDateTime).Value.CompareTo(sevenDaysAgo) <= 0) ||
                        (r.ConnectionStatus.Id == 2 && r.ConnectionRequestActivities.Max(a => a.CreatedDateTime).Value.CompareTo(thirtyDaysAgo) <= 0) ||
                        ((r.ConnectionStatus.Id == 9 || r.ConnectionStatus.Id == 10 || r.ConnectionStatus.Id == 13)
                            && r.ConnectionRequestActivities.Max(a => a.CreatedDateTime).Value.CompareTo(thirtyDaysAgo) <= 0)
                    )
                .GroupBy(r => new
                {
                    r.ConnectionStatus
                })
                .Select(s => new
                {
                    ConnectionStatus = s.Key.ConnectionStatus,
                    Total = s.Count()
                }).ToList();

            mergeFields.Add("NoContact", connectionStatuses.Where(c => c.ConnectionStatus.Id == 1).Any()
                ? connectionStatuses.Where(c => c.ConnectionStatus.Id == 1).FirstOrDefault().Total : 0);
            mergeFields.Add("InProgressPlacement", connectionStatuses.Where(c => c.ConnectionStatus.Id == 2).Any()
                ? connectionStatuses.Where(c => c.ConnectionStatus.Id == 2).FirstOrDefault().Total : 0);
            mergeFields.Add("InProgressOther", connectionStatuses
                .Where(c => c.ConnectionStatus.Id == 9 || c.ConnectionStatus.Id == 10 || c.ConnectionStatus.Id == 13).Any()
                    ? connectionStatuses.Where(c => c.ConnectionStatus.Id == 9 || c.ConnectionStatus.Id == 10 || c.ConnectionStatus.Id == 13).Sum(c => c.Total) : 0);

            var requests = new ConnectionRequestService(rockContext)
                .Queryable()
                .Where(r => r.CampusId == campusId)
                .Where(r => r.ConnectionOpportunity.ConnectionTypeId == 1)
                .Where(r => r.ConnectionState == ConnectionState.Active)
                .Where(r =>
                        (r.ConnectionStatus.Id == 1 && r.ConnectionRequestActivities.Max(a => a.CreatedDateTime).Value.CompareTo(sevenDaysAgo) <= 0) ||
                        (r.ConnectionStatus.Id == 2 && r.ConnectionRequestActivities.Max(a => a.CreatedDateTime).Value.CompareTo(thirtyDaysAgo) <= 0) ||
                        ((r.ConnectionStatus.Id == 9 || r.ConnectionStatus.Id == 10 || r.ConnectionStatus.Id == 13)
                            && r.ConnectionRequestActivities.Max(a => a.CreatedDateTime).Value.CompareTo(thirtyDaysAgo) <= 0)
                    )
                .Select(s => new
                {
                    Person = s.PersonAlias.Person,
                    CurrentStatus = s.ConnectionStatus.Name,
                    LastUpdated = s.ModifiedDateTime.ToString(),
                    Connector = s.ConnectorPersonAlias.Person
                }).OrderBy(r => r.Person.LastName).ToList();

            mergeFields.Add("rows", requests);
            mergeFields.Add("PageId", connectionPageId);

            if (requests.Any())
            {
                var emailMessage = new RockEmailMessage();
                foreach (var email in campusEmailList)
                {
                    emailMessage.AddRecipient(new RecipientData(email, mergeFields));
                }
                emailMessage.AppRoot = Rock.Web.Cache.GlobalAttributesCache.Read(rockContext).GetValue("ExternalApplicationRoot");
                emailMessage.FromEmail = systemEmail.From;
                emailMessage.FromName = systemEmail.FromName;
                emailMessage.Subject = systemEmail.Subject;
                emailMessage.Message = systemEmail.Body.ResolveMergeFields(mergeFields);
                emailMessage.CreateCommunicationRecord = true;
                emailMessage.Send();
            }
        }

    }
}
