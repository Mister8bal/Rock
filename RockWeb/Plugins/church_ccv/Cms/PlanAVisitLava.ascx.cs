using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using church.ccv.CCVCore.PlanAVisit.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Cms
{
    [DisplayName( "Plan A Visit List Lava" )]
    [Category( "CCV > Cms" )]
    [Description( "Lists all visit's from the Plan A Visit plugin using a Lava template" )]
    [CodeEditorField( "Contents", @"The Lava template to use for displaying the visits.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false, "", "", 3 )]


    public partial class PlanAVisitLava : RockBlock
    {

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {            
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindData();
            }
        }



        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindData();
        }

        #endregion

        #region Methods

        private void BindData()
        {
            RockContext rockContext = new RockContext();

            // services for query
            var planAVisitTable = new Service<PlanAVisit>( rockContext ).Queryable().AsNoTracking();
            var campusTable = new CampusService( rockContext ).Queryable().AsNoTracking();
            var personAliasTable = new PersonAliasService( rockContext ).Queryable().AsNoTracking();
            var personTable = new PersonService( rockContext ).Queryable().AsNoTracking();
            var scheduleTable = new ScheduleService( rockContext ).Queryable().AsNoTracking();

            var pavQuery =
                from planAVisit in planAVisitTable
                join campus in campusTable on planAVisit.CampusId equals campus.Id into campuses
                from campus in campuses.DefaultIfEmpty()
                join personAlias in personAliasTable on planAVisit.AdultOnePersonAliasId equals personAlias.Id into personAliases
                from personAlias in personAliases.DefaultIfEmpty()
                join person in personTable on personAlias.PersonId equals person.Id into people
                from person in people.DefaultIfEmpty()
                join scheduledSchedule in scheduleTable on planAVisit.ScheduledServiceScheduleId equals scheduledSchedule.Id into scheduleSchedules
                from scheduledSchedule in scheduleSchedules.DefaultIfEmpty()
                join attendedSchedule in scheduleTable on planAVisit.AttendedServiceScheduleId equals attendedSchedule.Id into attendedSchedules
                from attendedSchedule in attendedSchedules.DefaultIfEmpty()
                select new
                {
                    planAVisit.Id,
                    planAVisit.AdultOnePersonAliasId,                    
                    planAVisit.AdultTwoPersonAliasId,
                    FamilyLastName = person.LastName,
                    planAVisit.CampusId,
                    CampusName = campus.Name,
                    planAVisit.ScheduledDate,
                    planAVisit.ScheduledServiceScheduleId,
                    ScheduledServiceName = scheduledSchedule.Name,
                    planAVisit.BringingChildren,
                    planAVisit.AttendedDate,
                    planAVisit.AttendedServiceScheduleId,
                    AttendedServiceName = attendedSchedule.Name
                };

            // filter query from block settings
            var filteredQuery = pavQuery.ToList();

            string contents = GetAttributeValue( "Contents" );

            
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

            var mergeFieldsKV = mergeFields.ToList();


            mergeFields.Add( "Visits", filteredQuery.OrderByDescending( a => a.ScheduledDate ).ThenBy( b => b.FamilyLastName ) );

            ///
            /// Need block settings for configuring how many weeks back and weeks forward to show
            ///

            lContents.Text = contents.ResolveMergeFields( mergeFields );     
        }     
        
        #endregion

    }
}
