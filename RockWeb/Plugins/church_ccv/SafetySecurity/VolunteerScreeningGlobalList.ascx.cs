
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

using church.ccv.SafetySecurity.Model;
using System.Web.UI.WebControls;

namespace RockWeb.Plugins.church_ccv.SafetySecurity
{
    [DisplayName( "Volunteer Screening Global List" )]
    [Category( "CCV > Safety and Security" )]
    [Description( "Lists all volunteer screening instances newer than 2 months." )]
    
    [LinkedPage( "Detail Page" )]
    [CampusesField( "Campuses", "List of which campuses to show volunteer screening instances for.", required: false, includeInactive: true )]
    public partial class VolunteerScreeningGlobalList : RockBlock
    {
        // the actual types of Volunteer Screening Applications aren't
        // defined anywhere. However, in order to filter on them, we need thm listed.
        // we put them here since this is the only area in all of Volunteer Screening that cares about them.
        const string sApplicationType_Standard = "Standard";
        const string sApplicationType_KidsStudents = "Kids & Students";
        const string sApplicationType_SafetySecurity = "Safety & Security";
        const string sApplicationType_STARS = "STARS";
        const string sApplicationType_Renewal = "Renewal";

        // 2019 types
        const string sApplicationType_Adult2019 = "Adult";
        const string sApplicationType_Student2019 = "Student";

        const int sCharacterReference_WorkflowId = 203;

        #region Control Methods
        
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            
            InitFilter( );
            InitGrid( );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }
        
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            
            if ( !Page.IsPostBack )
            {
                BindFilter( );
                BindGrid( );
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
            BindFilter();
            BindGrid();
        }

        #endregion

        #region Filter Methods

        void InitFilter( )
        {
            rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;
        }

        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SaveUserPreference( "Campus", "Campus", cblCampus.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( "Status", ddlStatus.SelectedValue );
            rFilter.SaveUserPreference( "Application Type", ddlApplicationType.SelectedValue );
            rFilter.SaveUserPreference( "Applicant Name", tbApplicantName.Text );
            rFilter.SaveUserPreference( "Ministry Serving With", dvpMinistryServingWith.SelectedValueAsGuid().ToString() );
            rFilter.SaveUserPreference( "Requester", ppRequester.PersonAliasId.ToString() );
            rFilter.SaveUserPreference( "Application Completed", cblApplicationCompleted.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( "Application Sent Date", drpApplicationSentDate.DelimitedValues );

            BindFilter( );
            BindGrid( );
        }

        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Campus":
                {
                    var values = new List<string>();
                    foreach ( string value in e.Value.Split( ';' ) )
                    {
                        var item = cblCampus.Items.FindByValue( value );
                        if ( item != null )
                        {
                            values.Add( item.Text );
                        }
                    }
                    e.Value = values.AsDelimited( ", " );
                    break;
                }

                case "Status":
                {
                    e.Value = rFilter.GetUserPreference( "Status" );
                    break;
                }

                case "Application Type":
                {
                    e.Value = rFilter.GetUserPreference( "Application Type" );
                    break;
                }

                case "Applicant Name":
                {
                    e.Value = rFilter.GetUserPreference( "Applicant Name" );
                    break;
                }

                case "Ministry Serving With":
                {
                    e.Value = rFilter.GetUserPreference( "Ministry Serving With" ).AsNumeric();
                    break;
                }

                case "Requester":
                {
                    e.Value = rFilter.GetUserPreference( "Requster" ).AsNumeric();
                    break;
                }

                case "Application Completed":
                {
                    var values = new List<string>();
                    foreach ( string value in e.Value.Split( ';' ) )
                    {
                        var item = cblApplicationCompleted.Items.FindByValue( value );
                        if ( item != null )
                        {
                            values.Add( item.Text );
                        }
                    }
                    e.Value = values.AsDelimited( ", " );
                    break;
                }

                case "Application Sent Date":
                {
                    e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                    break;
                }

                default:
                {
                    e.Value = string.Empty;
                    break;
                }
            }
        }

        private void BindFilter()
        {
            // setup the campus
            // if Block Campus filter is applied, update User campus filter to only show respective campuses
            // if not applied, show all campuses
            if ( string.IsNullOrWhiteSpace( GetAttributeValue( "Campuses" ) ) == false )
            {
                List<Guid> selectedCampusesAttribute = Array.ConvertAll( GetAttributeValue( "Campuses" ).Split( ',' ), s => new Guid( s ) ).ToList();

                var selectedCampuses = CampusCache.All();

                selectedCampuses = selectedCampuses.Where( vs => selectedCampusesAttribute.Contains( vs.Guid ) ).ToList();

                cblCampus.DataSource = selectedCampuses;
                cblCampus.DataBind();

            }
            else
            {
                cblCampus.DataSource = CampusCache.All( false );
                cblCampus.DataBind();
            }


            string campusValue = rFilter.GetUserPreference( "Campus" );
            if ( !string.IsNullOrWhiteSpace( campusValue ) )
            {
                cblCampus.SetValues( campusValue.Split( ';' ).ToList() );
            }

            // setup the status / state
            ddlStatus.Items.Clear( );
            ddlStatus.Items.Add( string.Empty );
            ddlStatus.Items.Add( VolunteerScreening.sState_Waiting );
            ddlStatus.Items.Add( VolunteerScreening.sState_InReviewWithCampus );
            ddlStatus.Items.Add( VolunteerScreening.sState_InReviewWithSecurity );
            ddlStatus.Items.Add( VolunteerScreening.sState_Accepted );

            ddlStatus.SetValue( rFilter.GetUserPreference( "Status" ) );

            // setup the application types
            ddlApplicationType.Items.Clear( );
            ddlApplicationType.Items.Add( string.Empty );
            ddlApplicationType.Items.Add( sApplicationType_Adult2019 );
            ddlApplicationType.Items.Add( sApplicationType_Student2019 );
            ddlApplicationType.Items.Add( sApplicationType_Standard );
            ddlApplicationType.Items.Add( sApplicationType_KidsStudents );
            ddlApplicationType.Items.Add( sApplicationType_SafetySecurity );
            ddlApplicationType.Items.Add( sApplicationType_STARS );
            ddlApplicationType.Items.Add( sApplicationType_Renewal );
            ddlApplicationType.SetValue( rFilter.GetUserPreference( "Application Type" ) );

            // setup the Applicant Name
            tbApplicantName.Text = rFilter.GetUserPreference( "Applicant Name" );

            // setup the Ministry Serving With
            var definedTypeCache = DefinedTypeCache.Read( 558 );

            //dvpMinistryServingWith.BindToDefinedType( definedTypeCache, true );

            var ds = definedTypeCache.DefinedValues
                .Select( v => new
                {
                    Name = v.Value,
                    v.Description,
                    v.Guid
                } );

            dvpMinistryServingWith.SelectedIndex = -1;
            dvpMinistryServingWith.DataSource = ds;
            dvpMinistryServingWith.DataTextField = "Name";
            dvpMinistryServingWith.DataValueField = "Guid";
            dvpMinistryServingWith.DataBind();
            dvpMinistryServingWith.Items.Insert( 0, new ListItem() );

            Guid? definedValueGuid = rFilter.GetUserPreference( "Ministry Serving With" ).AsGuidOrNull();
            if ( definedValueGuid.HasValue )
            {
                dvpMinistryServingWith.SetValue( definedValueGuid.Value );
            }

            // setup the Requester
            int? personAliasId = rFilter.GetUserPreference( "Requester" ).AsIntegerOrNull();
            if ( personAliasId.HasValue )
            {
                var requester = new PersonAliasService( new RockContext() ).Get( personAliasId.Value ).Person;
                ppRequester.SetValue( requester );
            }

            // setup Application Completed
            string applicationCompletedValue = rFilter.GetUserPreference( "Application Completed" );
            if ( !string.IsNullOrWhiteSpace( applicationCompletedValue ) )
            {
                cblApplicationCompleted.SetValues( applicationCompletedValue.Split( ';' ).ToList() );
            }

            // setup Application Date
            string applicationSentDate = rFilter.GetUserPreference( "Application Sent Date" );
            if ( applicationSentDate.IsNotNullOrWhiteSpace() )
            {
                drpApplicationSentDate.DelimitedValues = applicationSentDate;
            }

        }
        #endregion

        #region Grid Methods

        void InitGrid( )
        {
            gGrid.DataKeyNames = new string[] { "Id" };
            gGrid.CommunicationRecipientPersonIdFields.Add( "PersonId" );

            gGrid.Actions.Visible = true;
            gGrid.Actions.Enabled = true;
            gGrid.Actions.ShowBulkUpdate = false;
            gGrid.Actions.ShowCommunicate = true;
            gGrid.Actions.ShowExcelExport = false;
            gGrid.Actions.ShowMergePerson = false;
            gGrid.Actions.ShowMergeTemplate = false;

            gGrid.GridRebind += gGrid_Rebind;
        }

        private void gGrid_Rebind( object sender, EventArgs e )
        {
            BindFilter( );
            BindGrid( );
        }

        protected void gGrid_Edit( object sender, RowEventArgs e )
        {
            if ( e.RowKeyId > 0 )
            {
                var rockContext = new RockContext();
                var volunteerScreening = new VolunteerScreeningService( rockContext ).Get( e.RowKeyId );
                if ( volunteerScreening != null && volunteerScreening.PersonAliasId > 0 )
                {
                    var person = new PersonAliasService( rockContext ).Get( volunteerScreening.PersonAliasId ).Person;

                    var qryParams = new Dictionary<string, string>();
                    qryParams.Add( "VolunteerScreeningInstanceId", e.RowKeyId.ToString() );
                    qryParams.Add( "PersonId", person.Id.ToString() );

                    NavigateToLinkedPage( "DetailPage", qryParams );  
                }
            }
        }

        /// <summary>
        /// Handles the Delete event of the gGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGrid_Delete( object sender, RowEventArgs e )
        {
            if ( e.RowKeyId.ToString().AsInteger() > 0 )
            {
                using ( RockContext rockContext = new RockContext() )
                {
                    var vsService = new VolunteerScreeningService( rockContext );
                    VolunteerScreening screening = vsService.Get( e.RowKeyId.ToString().AsInteger() );

                    // remove volunteer screening
                    vsService.Delete( screening );
                    rockContext.SaveChanges();

                    BindGrid( );
                }
            }
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid( )
        {
            using ( RockContext rockContext = new RockContext( ) )
            {
                List<CampusCache> campusCache = CampusCache.All( );
                PersonAliasService paService = new PersonAliasService( rockContext );

                // get all volunteer screening instances. This is complicated, so I'll explain:

                // Each instance is stored in the VolunteerScreening table, with Ids (pointers) to a person and workflow.
                // Additionally, the workflows store the Campus Attribute, which is important for organizing these per-campus.
                // So, we have to join FIVE tables to get everything.

                // First, we simply join the 3 "core" tables--volunteer screening, personAlias, and workflow.
                var vsQuery = new VolunteerScreeningService( rockContext ).Queryable( ).AsNoTracking( );
                var paQuery = new Service<PersonAlias>( rockContext ).Queryable( ).AsNoTracking( );
                var wfQuery = new Service<Workflow>( rockContext ).Queryable( ).AsNoTracking( );
                var coreQuery = vsQuery.Join( paQuery, vs => vs.PersonAliasId, pa => pa.Id, ( vs, pa ) => new { VolunteerScreening = vs, PersonName = pa.Person.FirstName + " " + pa.Person.LastName, PersonId = pa.PersonId } )
                                       .Join( wfQuery, vs => vs.VolunteerScreening.Application_WorkflowId, wf => wf.Id, ( vs, wf ) => new { VolunteerScreeningWithPerson = vs, Workflow = wf } );


                // Now, since the Campus Attribute Value depends on the Attribute table, we need to join those two tables, and then join that to the query we built above.
                var attribQuery = new AttributeService( rockContext ).Queryable( ).AsNoTracking( );
                var avQuery = new AttributeValueService( rockContext ).Queryable( ).AsNoTracking( );
                var attribWithValue = attribQuery.Join( avQuery, a => a.Id, av => av.AttributeId, ( a, av ) => new { Attribute = a, AttribValue = av } )
                                                 .Where( a => a.Attribute.EntityTypeQualifierColumn.Equals( "WorkflowTypeId", StringComparison.OrdinalIgnoreCase ) );

                // join the attributeValues so we can add in the campus
                var instanceQuery = coreQuery.Join( attribWithValue, vs => vs.Workflow.Id, av => av.AttribValue.EntityId, ( vs, av ) => new { VS = vs, AV = av } )
                                             .Where( a => a.AV.Attribute.Key == "Campus" )
                                             .Select( a => new { Id = a.VS.VolunteerScreeningWithPerson.VolunteerScreening.Id,
                                                                 SentDate = a.VS.VolunteerScreeningWithPerson.VolunteerScreening.CreatedDateTime.Value,
                                                                 CompletedDate = a.VS.VolunteerScreeningWithPerson.VolunteerScreening.ModifiedDateTime.Value,
                                                                 PersonId = a.VS.VolunteerScreeningWithPerson.PersonId,
                                                                 PersonName = a.VS.VolunteerScreeningWithPerson.PersonName,
                                                                 Workflow = a.VS.Workflow,
                                                                 CampusGuid = a.AV.AttribValue.Value } )
                                             .ToList( );

                // In the end, we've joined the following tables:
                // VolunteerScreening, Workflow, PersonAlias, Attribute, AttributeValue
                // and we're selecting the properties needed to filter and display each Application.
                // We now have an object with: 
                // The Volunteer Screening Id (Taken from the VS table)
                // Its SentDate, CompletedDate (Taken from the WF table)
                // Its Person (Taken from the PersonAlias table)
                // Its Campus (Taken from the AttributeValue table)

                // get a list of all "Requester" attribute values, which we'll match up with each Application when binding the rows
                var requesterResult = attribWithValue.Where( a => a.Attribute.Key == "Requester" )
                                                        .Select( a => new RequesterResult {  EntityId = a.AttribValue.EntityId,
                                                                                                RequesterPersonAliasGuidString = a.AttribValue.Value } )
                                                        .ToList( );

                // get a list of all "Ministry Serving With" attribute values, which we'll match up with each Application when binding the rows
                var ministryServingWithResult = attribWithValue.Where( a => a.Attribute.Key == "MinistryServingWith" )
                                                        .Select( a => new MinistryServingWithResult
                                                        {
                                                            EntityId = a.AttribValue.EntityId,
                                                            DefinedValueGuidString = a.AttribValue.Value
                                                        } )
                                                        .ToList();

                // ---- Apply Filters ----
                var filteredQuery = instanceQuery;

                // First apply Campus Block Setting Filter
                if ( string.IsNullOrWhiteSpace(GetAttributeValue( "Campuses" )) == false )
                {
                    List<Guid> selectedCampuses = Array.ConvertAll( GetAttributeValue( "Campuses" ).Split( ',' ), s => new Guid( s ) ).ToList();
                    if ( selectedCampuses.Count > 0 )
                    {
                        filteredQuery = filteredQuery.Where( vs => selectedCampuses.Contains( vs.CampusGuid.AsGuid() ) ).ToList();
                    }
                }

                // Now apply user filters
                // Campus
                List<int> campusIds = cblCampus.SelectedValuesAsInt;
                if( campusIds.Count > 0 )
                {
                    // the workflows store the campus by guid, so convert the selected Ids to guids
                    List<Guid> selectedCampusNames = campusCache.Where( cc => campusIds.Contains( cc.Id ) ).Select( cc => cc.Guid ).ToList( );

                    filteredQuery = filteredQuery.Where( vs => selectedCampusNames.Contains( vs.CampusGuid.AsGuid( ) ) ).ToList( );
                }

                // Status
                string statusValue = rFilter.GetUserPreference( "Status" );
                if ( string.IsNullOrWhiteSpace( statusValue ) == false )
                {
                    filteredQuery = filteredQuery.Where( vs => VolunteerScreening.GetState( vs.SentDate, vs.CompletedDate, vs.Workflow.Status ) == statusValue ).ToList( );
                }

                // Application Type
                string appType = rFilter.GetUserPreference( "Application Type" );
                if ( string.IsNullOrWhiteSpace( appType ) == false )
                {
                    filteredQuery = filteredQuery.Where( vs => ParseApplicationType( vs.Workflow ) == appType ).ToList( );
                }

                // Name
                string personName = rFilter.GetUserPreference( "Applicant Name" );
                if ( string.IsNullOrWhiteSpace( personName ) == false )
                {
                    filteredQuery = filteredQuery.Where( vs => vs.PersonName.ToLower( ).Contains( personName.ToLower( ).Trim( ) ) ).ToList( );
                }


                // Build Query so that requester and Completed Date are populated for filtering / sorting
                // If application is not completed, null emptyDate is used to display correct empty text
                DateTime? emptyDate = null;

                var filteredQueryWithRequester = filteredQuery.OrderByDescending( vs => vs.SentDate ).OrderByDescending( vs => vs.CompletedDate ).Select( vs =>
                                                new {
                                                    Name = vs.PersonName,
                                                    Id = vs.Id,
                                                    PersonId = vs.PersonId,
                                                    SentDate = vs.SentDate,
                                                    CompletedDate = (vs.SentDate == vs.CompletedDate) ? emptyDate : vs.CompletedDate,
                                                    State = VolunteerScreening.GetState( vs.SentDate, vs.CompletedDate, vs.Workflow.Status ),
                                                    Campus = TryGetCampus( campusCache, vs.CampusGuid ),
                                                    MinistryServingWith = TryGetCampusMinistryServingWith( vs.Workflow, ministryServingWithResult ),
                                                    Requester = TryGetRequester( vs.Workflow, requesterResult, paService ),
                                                    ApplicationType = ParseApplicationType( vs.Workflow )
                                                } ).ToList();

                // Ministry Serving With
                string ministryServingWith = "";
                Guid? ministryServingWithGuid = rFilter.GetUserPreference( "Ministry Serving With" ).AsGuidOrNull();
                if ( ministryServingWithGuid.HasValue )
                {
                    ministryServingWith = new DefinedValueService( new RockContext() ).Get( ministryServingWithGuid.Value ).Value;
                }

                if ( ministryServingWith.IsNotNullOrWhiteSpace() )
                {
                    filteredQueryWithRequester = filteredQueryWithRequester.Where( vs => vs.MinistryServingWith.ToLower().Contains( ministryServingWith.ToLower().Trim() ) ).ToList();
                }

                // Requester
                string requesterName = "";
                int? personAliasId = rFilter.GetUserPreference( "Requester" ).AsIntegerOrNull();
                if ( personAliasId.HasValue )
                {
                    requesterName = new PersonAliasService( new RockContext() ).Get( personAliasId.Value ).Person.FullName;
                }

                if( string.IsNullOrWhiteSpace( requesterName ) == false )
                {
                    filteredQueryWithRequester = filteredQueryWithRequester.Where( vs => vs.Requester.ToLower( ).Contains( requesterName.ToLower( ).Trim( ) ) ).ToList();
                }

                // Application Completed
                string applicationCompleted = cblApplicationCompleted.SelectedValues.AsDelimited( ";" );
                if ( string.IsNullOrWhiteSpace( applicationCompleted ) == false )
                {
                    if ( applicationCompleted == "Completed" )
                    {
                        filteredQueryWithRequester = filteredQueryWithRequester.Where( vs => vs.CompletedDate != null ).ToList();
                    }
                    else if ( applicationCompleted == "Not Completed" )
                    {
                        filteredQueryWithRequester = filteredQueryWithRequester.Where( vs => vs.CompletedDate == emptyDate ).ToList();
                    }
                }

                // Application Sent Date
                if ( drpApplicationSentDate.LowerValue.HasValue )
                {
                    filteredQueryWithRequester = filteredQueryWithRequester.Where( vs => vs.SentDate >= drpApplicationSentDate.LowerValue.Value ).ToList();
                }

                if ( drpApplicationSentDate.UpperValue.HasValue )
                {
                    filteredQueryWithRequester = filteredQueryWithRequester.Where( vs => vs.SentDate <= drpApplicationSentDate.UpperValue.Value ).ToList();
                }

                // ---- End Filters ----

                // Sort grid
                SortProperty sortProperty = gGrid.SortProperty;

                if ( sortProperty != null )
                {     
                    if ( sortProperty.Direction == System.Web.UI.WebControls.SortDirection.Ascending )
                    {
                        switch ( sortProperty.Property )
                        {
                            case "Name":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.Name ).ToList();
                                break;
                            case "State":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.State ).ToList();
                                break;
                            case "SentDate":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.SentDate ).ToList();
                                break;
                            case "CompletedDate":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.CompletedDate ).ToList();
                                break;
                            case "Campus":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.Campus ).ToList();
                                break;
                            case "ApplicationType":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.ApplicationType ).ToList();
                                break;
                            case "Requester":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.Requester ).ToList();
                                break;
                            case "MinistyServingWith":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderBy( o => o.MinistryServingWith ).ToList();
                                break;
                        }
                    }
                    else
                    {
                        switch ( sortProperty.Property )
                        {
                            case "Name":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.Name ).ToList();
                                break;
                            case "State":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.State ).ToList();
                                break;
                            case "SentDate":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.SentDate ).ToList();
                                break;
                            case "CompletedDate":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.CompletedDate ).ToList();
                                break;
                            case "Campus":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.Campus ).ToList();
                                break;
                            case "ApplicationType":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.ApplicationType ).ToList();
                                break;
                            case "Requester":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.Requester ).ToList();
                                break;
                            case "MinistyServingWith":
                                filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.MinistryServingWith ).ToList();
                                break;
                        }
                    }                  
                }
                else
                {
                    filteredQueryWithRequester = filteredQueryWithRequester.OrderByDescending( o => o.SentDate ).ToList();
                }

                // Bind filter to grid
                if ( filteredQueryWithRequester.Count( ) > 0 )
                {
                    gGrid.DataSource = filteredQueryWithRequester;
                }

                gGrid.DataBind( );
            }
        }

        #endregion

        #region Helper Methods

        string ParseApplicationType( Workflow workflow )
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

        string TryGetCampus( List<CampusCache> campusCache, string campusGuid )
        {
            // this should never be null, but if something happens to the underlying workflow data, it can be.
            // Doing this prevents the entire global list from breaking due to that.
            CampusCache campus = campusCache.Where( c => c.Guid == campusGuid.AsGuid() ).SingleOrDefault();
            if ( campus != null )
            {
                return campus.Name;
            }

            return "No Campus";
        }

        /// <summary>
        /// The Ministry Serving With result.
        /// </summary>
        public class MinistryServingWithResult
        {
            public int? EntityId { get; set; }
            public string DefinedValueGuidString { get; set; }
        }

        /// <summary>
        /// Tries the get campus ministry serving with.
        /// </summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="ministryServingWithResult">The ministry serving with result.</param>
        /// <param name="dvService">The dv service.</param>
        /// <returns></returns>
        string TryGetCampusMinistryServingWith( Workflow workflow, List<MinistryServingWithResult> ministryServingWithResult )
        {
            // it's possible that no ministry lead has been assigned yet. In that case, we'll return an empty string
            MinistryServingWithResult ministryServingWith = ministryServingWithResult.Where( ml => ml.EntityId == workflow.Id ).SingleOrDefault();
            if ( ministryServingWith != null && ministryServingWith.DefinedValueGuidString.IsNotNullOrWhiteSpace() )
            {
                // make sure the person exists
                Guid? definedValueGuid = ministryServingWith.DefinedValueGuidString.AsGuidOrNull();
                if ( !definedValueGuid.HasValue )
                {
                    return string.Empty;
                }

                DefinedValueCache definedValue = DefinedValueCache.Read( definedValueGuid.Value );
                if ( definedValue != null )
                {
                    if ( definedValue.Value.IsNotNullOrWhiteSpace() )
                    {
                        return definedValue.Value;
                    }
                }
            }

            return string.Empty;
        }

        public class RequesterResult
        {
            public int? EntityId { get; set; }
            public string RequesterPersonAliasGuidString { get; set; }
        }

        string TryGetRequester( Workflow workflow, List<RequesterResult> requesterQueryResult, PersonAliasService paService )
        {
            // it's possible that no ministry lead has been assigned yet. In that case, we'll return an empty string
            RequesterResult requester = requesterQueryResult.Where( ml => ml.EntityId == workflow.Id ).SingleOrDefault( );
            if ( requester != null && requester.RequesterPersonAliasGuidString.IsNotNullOrWhiteSpace() )
            {
                // make sure the person exists
                Guid? requesterAliasGuid = requester.RequesterPersonAliasGuidString.AsGuidOrNull();
                if ( !requesterAliasGuid.HasValue )
                {
                    return string.Empty;
                }

                Person person = paService.Get( requesterAliasGuid.Value ).Person;
                if ( person != null )
                {
                    return person.FullName;
                }
            }

            return string.Empty;
        }

        #endregion
    }
}
