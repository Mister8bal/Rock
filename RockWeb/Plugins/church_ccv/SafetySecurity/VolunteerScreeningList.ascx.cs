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
using church.ccv.SafetySecurity.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;

namespace RockWeb.Plugins.church_ccv.SafetySecurity
{
    [DisplayName( "Volunteer Screening List" )]
    [Category( "CCV > Safety and Security" )]
    [Description( "Lists volunteer screening instances for the given person." )]

    [ContextAware( typeof( Person ) )]
    [LinkedPage( "Detail Page" )]
    public partial class VolunteerScreeningList : RockBlock
    {
        protected Person TargetPerson { get; set; }
        const string sApplicationType_Standard = "Standard";
        const string sApplicationType_KidsStudents = "Kids & Students";
        const string sApplicationType_SafetySecurity = "Safety & Security";
        const string sApplicationType_STARS = "STARS";
        const string sApplicationType_Renewal = "Renewal";

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // in case this is used as a Person Block, set the TargetPerson 
            TargetPerson = ContextEntity<Person>();
            InitInstancesGrid( );
        }

        void InitInstancesGrid( )
        {
            gGrid.DataKeyNames = new string[] { "Id" };

            // turn on only the 'add' button
            gGrid.Actions.Visible = true;
            gGrid.Actions.Enabled = true;
            gGrid.Actions.ShowBulkUpdate = false;
            gGrid.Actions.ShowCommunicate = false;
            gGrid.Actions.ShowExcelExport = false;
            gGrid.Actions.ShowMergePerson = false;
            gGrid.Actions.ShowMergeTemplate = false;

            //gGrid.Actions.ShowAdd = IsUserAuthorized( Authorization.EDIT );
            //gGrid.Actions.AddClick += gGrid_AddClick;

            gGrid.GridRebind += gGrid_Rebind;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            
            if ( !Page.IsPostBack && TargetPerson != null )
            {   
                using ( RockContext rockContext = new RockContext( ) )
                {
                    BindGrid( rockContext );
                }
                string url = string.Format("~/WorkflowEntry/230?PersonId={0}",TargetPerson.Id);
                hlSendApplication.NavigateUrl = ResolveRockUrl(url);
            }
        }
        
        #endregion
        
        #region Grid Events (main grid)

        protected void gGrid_AddClick( object sender, EventArgs e )
        {
            using ( RockContext rockContext = new RockContext( ) )
            {
                VolunteerScreening instance = new VolunteerScreening( );
                instance.PersonAliasId = TargetPerson.PrimaryAliasId.Value;
                instance.CreatedDateTime = RockDateTime.Now;
                instance.ModifiedDateTime = RockDateTime.Now;
                instance.Type = (int)VolunteerScreening.Types.Legacy;

                Service<VolunteerScreening> rockService = new Service<VolunteerScreening>( rockContext );
                rockService.Add( instance );

                rockContext.SaveChanges( );
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gPromotions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void gGrid_Rebind( object sender, EventArgs e )
        {
            using ( RockContext rockContext = new RockContext( ) )
            {
                BindGrid( rockContext );
            }
        }

        protected void gGrid_Edit( object sender, RowEventArgs e )
        {
            var qryParams = new Dictionary<string, string>();
            qryParams.Add( "VolunteerScreeningInstanceId", e.RowKeyId.ToString() );

            NavigateToLinkedPage( "DetailPage", qryParams );
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid( RockContext rockContext )
        {
            // get all the volunteer screening instances tied to this person
            var vsQuery = new Service<VolunteerScreening>( rockContext ).Queryable( ).AsNoTracking( );
            var paQuery = new Service<PersonAlias>( rockContext ).Queryable( ).AsNoTracking( );
            var wfQuery = new Service<Workflow>( rockContext ).Queryable( ).AsNoTracking( );

            var attribQuery = new AttributeService( rockContext ).Queryable( ).AsNoTracking( );
            var avQuery = new AttributeValueService( rockContext ).Queryable( ).AsNoTracking( );
            var attribWithValue = attribQuery.Join( avQuery, a => a.Id, av => av.AttributeId, ( a, av ) => new { Attribute = a, AttribValue = av } );
            
            var vsForPersonQuery = vsQuery.Join( paQuery, vs => vs.PersonAliasId, pa => pa.Id, ( vs, pa ) => new { VolunteerScreening = vs, PersonAlias = pa } )
                                       .Where( c => c.PersonAlias.PersonId == TargetPerson.Id )
                                       .Select( a => a.VolunteerScreening )
                                       .AsQueryable( );

            var instanceQuery = vsForPersonQuery.Join( wfQuery, vs => vs.Application_WorkflowId, wf => wf.Id, ( vs, wf ) => new { VolunteerScreening = vs, Workflow = wf, WorkflowStatus = wf.Status, InitiatedBy = wf.InitiatorPersonAliasId  } ).AsQueryable( );

            // JHM 7-10-17
                // HORRIBLE HACK - If the application was sent before we ended testing, we need to support old states and attributes.
                // We need to do this because we have 100+ applications that were sent out (and not yet completed) during our testing phase. I was hoping for like, 10.
                // We can get rid of this when all workflows of type 202 are marked as 'completed'
            var starsQueryResult = attribWithValue.Where( a => a.Attribute.Key == "ApplyingForStars" )
                .Select( a => new ApplyingForStars{  EntityId = a.AttribValue.EntityId, Applying = a.AttribValue.Value } )
                .ToList( );

            if ( instanceQuery.Count( ) > 0 )
            {
                var instances = instanceQuery.Select( vs => new { Id = vs.VolunteerScreening.Id,
                                                                  SentDate = vs.VolunteerScreening.CreatedDateTime.Value,
                                                                  InitiatedBy = vs.InitiatedBy,
                                                                  CompletedDate = vs.VolunteerScreening.ModifiedDateTime.Value,
                                                                  vs.WorkflowStatus,
                                                                  vs.Workflow } ).ToList( );

                gGrid.DataSource = instances.OrderByDescending( vs => vs.SentDate ).OrderByDescending( vs => vs.CompletedDate ).Select( vs => 
                    new {
                            Id = vs.Id,
                            SentDate = vs.SentDate.ToShortDateString( ),
                            InitiatedBy = GetInitiator(vs.InitiatedBy),
                            CompletedDate = ParseCompletedDate( vs.SentDate, vs.CompletedDate ),
                            State = VolunteerScreening.GetState( vs.SentDate, vs.CompletedDate, vs.WorkflowStatus ),
                            ApplicationType = ParseApplicationType( vs.Workflow, starsQueryResult )
                        } ).ToList( );
            }
            
            gGrid.DataBind();
        }

        string GetInitiator( int? initiator )
        {
            if ( initiator.HasValue )
            {
                using ( RockContext rockContext = new RockContext() )
                {
                    PersonAlias personAlias = new PersonAliasService( rockContext ).Get( initiator.Value );

                    if ( personAlias != null && personAlias.Person != null )
                    {
                        return personAlias.Person.FullName;
                    }
                }
            }
            return "";
        }

        string ParseCompletedDate( DateTime sentDate, DateTime completedDate )
        {
            if( sentDate == completedDate )
            {
                return "Not Completed";
            }
            else
            {
                return completedDate.ToShortDateString( );
            }
        }

         // JHM 7-10-17
        // HORRIBLE HACK - If the application was sent before we ended testing, we need to support old states and attributes.
        // We need to do this because we have 100+ applications that were sent out (and not yet completed) during our testing phase. I was hoping for like, 10.
        // We can get rid of this when all workflows of type 202 are marked as 'completed'
        public class ApplyingForStars
        {
            public int? EntityId { get; set; }
            public string Applying { get; set; }
        }

        string ParseApplicationType( Workflow workflow, List<ApplyingForStars> starsQueryResult )
        {
            // JHM 7-10-17
            // HORRIBLE HACK - If the application was sent before we ended testing, we need to support old states and attributes.
            // We need to do this because we have 100+ applications that were sent out (and not yet completed) during our testing phase. I was hoping for like, 10.
            // We can get rid of this when all workflows of type 202 are marked as 'completed'
            ApplyingForStars applyingForStars = starsQueryResult.Where( av => av.EntityId == workflow.Id ).SingleOrDefault( );
            if ( applyingForStars != null )
            {
                return applyingForStars.Applying == "True" ? sApplicationType_STARS : sApplicationType_Standard;
            }
            else
            {
                // given the name of the workflow (which is always in the format 'FirstName LastName Application (Specific Type)' we'll return
                // either what's in parenthesis, or if nothing's there, "Standard" to convey it wasn't for a specific area.
                int appTypeStartIndex = workflow.Name.LastIndexOf( '(' );
                if ( appTypeStartIndex > -1 )
                {
                    // there was an ending "()", so take just that part of the workflow name
                    string applicationType = workflow.Name.Substring( appTypeStartIndex );

                    // take the character after the first, up to just before the closing ')', which removes the ()s
                    return applicationType.Substring( 1, applicationType.Length - 2 );
                }
                else
                {
                    return sApplicationType_Standard;
                }
            }
        }
        #endregion
    }
}
