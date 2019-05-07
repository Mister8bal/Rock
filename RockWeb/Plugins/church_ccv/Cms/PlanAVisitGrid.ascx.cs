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
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Cms
{
    [DisplayName( "Plan A Visit Grid" )]
    [Category( "CCV > Cms" )]
    [Description( "Grid used to display / manage Plan A Visit" )]

    public partial class PlanAVisitGrid : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            //InitFilter();
            InitGrid();

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
                //BindFilter();
                BindGrid();
            }

        }

        #region Events
        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

        protected void gGrid_RowSelected( object sender, RowEventArgs e )
        {
            RockContext rockContext = new RockContext();

            PlanAVisit visit = new Service<PlanAVisit>( rockContext ).Get( e.RowKeyId );

            Person person = new PersonAliasService( rockContext ).Get( visit.PersonAliasId ).Person;

            lblModalPersonName.Text = person.FullName;

            cpCampus.SelectedValue = visit.CampusId.ToString();
            tglBringingSpouse.Checked = visit.BringingSpouse;
            tglBringingChildren.Checked = visit.BringingChildren;

            hfModalVisitId.Value = e.RowKeyId.ToString();

            mdManageVisit.Show();
        }

        /// <summary>
        /// Handles the gGrid_Delete click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gGrid_Delete( object sender, RowEventArgs e )
        {
            RockContext rockContext = new RockContext();

            Service<PlanAVisit> pavService = new Service<PlanAVisit>( rockContext );

            PlanAVisit visit = pavService.Get( e.RowKeyId );

            pavService.Delete( visit );

            rockContext.SaveChanges();

            BindGrid();
        }

        protected void mdManageVisit_SaveClick( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();

            PlanAVisit visit = new Service<PlanAVisit>( rockContext ).Get( int.Parse( hfModalVisitId.Value ) );

            if ( cpCampus.SelectedValue.IsNotNullOrWhitespace())
            {
                visit.CampusId = int.Parse( cpCampus.SelectedValue );
            }

            visit.BringingSpouse = tglBringingSpouse.Checked;
            visit.BringingChildren = tglBringingChildren.Checked;

            rockContext.SaveChanges();

            mdManageVisit.Hide();

            BindGrid();
        }

        #endregion

        #region Grid Methods

        void InitGrid()
        {
            gGrid.DataKeyNames = new string[] { "Id" };

            gGrid.Actions.Visible = true;
            gGrid.Actions.Enabled = true;
            gGrid.Actions.ShowBulkUpdate = false;
            gGrid.Actions.ShowCommunicate = false;
            gGrid.Actions.ShowExcelExport = true;
            gGrid.Actions.ShowMergePerson = false;
            gGrid.Actions.ShowMergeTemplate = false;

            gGrid.GridRebind += gGrid_Rebind;
        }

        private void gGrid_Rebind( object sender, EventArgs e )
        {
            //BindFilter();
            BindGrid();
        }

        protected void gGrid_Edit( object sender, RowEventArgs e )
        {
            var qryParams = new Dictionary<string, string>
            {
                { "PlanAVisitInstanceId", e.RowKeyId.ToString() }
            };

            NavigateToLinkedPage( "DetailPage", qryParams );
        }

        private void BindGrid()
        {
            RockContext rockContext = new RockContext();

            Service<PlanAVisit> pavService = new Service<PlanAVisit>( rockContext );

            var pavQuery = pavService.Queryable().AsNoTracking();


            var filteredQuery = pavQuery.AsEnumerable().Select( pav => 
                                    new {
                                        Id = pav.Id,
                                        ScheduledDate = pav.ScheduledDate,
                                        ServiceTime = GetServiceTime(pav.ServiceTimeScheduleId),
                                        Campus = CampusCache.All().SingleOrDefault( a => a.Id == pav.CampusId ).Name,
                                        Person = GetPersonLink(pav.PersonAliasId),
                                        BringingSpouse = pav.BringingSpouse ? "Yes" : "No",
                                        BringingChildren = pav.BringingChildren ? "Yes" : "No",
                                        AttendedDate = pav.AttendedDate
                                    } ).ToList();

            gGrid.DataSource = filteredQuery;

            gGrid.DataBind();

        }

        /// <summary>
        /// Returns a clickable person link using a person alias Id
        /// </summary>
        /// <param name="personAliasId"></param>
        /// <returns></returns>
        private object GetPersonLink( int personAliasId )
        {
            Person person = new PersonAliasService( new RockContext() ).Get( personAliasId ).Person;

            return String.Format( "<a href=/person/{0}>{1}</a>", person.Id, person.FullName);
        }

        /// <summary>
        /// Return the time from a schedule using the schedule Id
        /// </summary>
        /// <param name="serviceTimeScheduleId"></param>
        /// <returns></returns>
        private object GetServiceTime( int serviceTimeScheduleId )
        {
            Schedule schedule = new ScheduleService( new RockContext() ).Get( serviceTimeScheduleId);

            // only return the time. IE Saturday 4:00pm returns just 4:00pm
            return schedule.Name.Substring( schedule.Name.IndexOf( ' ' ) + 1 );
        }

        #endregion


 
    }
}
