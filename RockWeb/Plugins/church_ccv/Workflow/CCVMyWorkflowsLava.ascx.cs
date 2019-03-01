// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Security;
using Rock.Workflow;

namespace RockWeb.Plugins.church_ccv.Workflow
{
    /// <summary>
    /// Block to display active workflow activities assigned to the current user that have a form entry action.  The display format is controlled by a lava template.
    /// </summary>
    [DisplayName( "CCV My Workflows Lava" )]
    [Category( "CCV > Workflow" )]
    [Description( "Block to display active workflow activities assigned to the current user that have a form entry action.  The display format is controlled by a lava template." )]

    [WorkflowTypeField( "Workflow Type", "Type of workflow to start." )]

    [CustomRadioListField( "Role", "Display the active workflows that the current user Initiated, or is currently Assigned To.", "0^Assigned To,1^Initiated", true, "0", "", 0 )]
    [CategoryField( "Categories", "Optional categories to limit display to.", true, "Rock.Model.WorkflowType", "", "", false, "", "", 1 )]
    [BooleanField( "Include Child Categories", "Should descendent categories of the selected Categories be included?", true, "", 2 )]
    [CodeEditorField( "Contents", @"The Lava template to use for displaying activities assigned to current user.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false, @"{% include '/Assets/Lava/MyWorkflowsSortable.lava' %}", "", 3 )]
    [TextField( "Set Panel Title", "The title to display in the panel header. Leave empty to have the block name.", required: false, order: 4 )]
    [TextField( "Set Panel Icon", "The icon to display in the panel header.", required: false, order: 5 )]
    public partial class CCVMyWorkflowsLava : Rock.Web.UI.RockBlock
    {
        #region Fields

        private RockContext _rockContext = null;
        private WorkflowService _workflowService = null;

        private WorkflowTypeCache _workflowType = null;
        private WorkflowActionTypeCache _actionType = null;
      //  Workflow _workflow = null;
        private WorkflowActivity _activity = null;
        private WorkflowAction _action = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the workflow type identifier.
        /// </summary>
        /// <value>
        /// The workflow type identifier.
        /// </value>
        public int? WorkflowTypeId
        {
            get { return ViewState["WorkflowTypeId"] as int?; }
            set { ViewState["WorkflowTypeId"] = value; }
        }


        /// <summary>
        /// Gets or sets a value indicating whether the workflow type was set by attribute.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [configured type]; otherwise, <c>false</c>.
        /// </value>
        public bool ConfiguredType
        {
            get { return ViewState["ConfiguredType"] as bool? ?? false; }
            set { ViewState["ConfiguredType"] = value; }
        }

        /// <summary>
        /// Gets or sets the workflow identifier.
        /// </summary>
        /// <value>
        /// The workflow identifier.
        /// </value>
        public int? WorkflowId
        {
            get { return ViewState["WorkflowId"] as int?; }
            set { ViewState["WorkflowId"] = value; }
        }

        /// <summary>
        /// Gets or sets the action type identifier.
        /// </summary>
        /// <value>
        /// The action type identifier.
        /// </value>
        public int? ActionTypeId
        {
            get { return ViewState["ActionTypeId"] as int?; }
            set { ViewState["ActionTypeId"] = value; }
        }
        #endregion

        #region Base Control Methods

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

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            if ( _workflowType != null && !ConfiguredType )
            {
                RockPage.PageTitle = _workflowType.Name;

                // we can only override if the page does not have a icon
                if ( string.IsNullOrWhiteSpace( RockPage.PageIcon ) && !string.IsNullOrWhiteSpace( _workflowType.IconCssClass ) )
                {
                    RockPage.PageIcon = _workflowType.IconCssClass;
                }
            }
            if ( _workflowType != null )
            {
                lTitle.Text = string.Format( "{0} Entry", _workflowType.WorkTerm );

                if ( !string.IsNullOrWhiteSpace( _workflowType.IconCssClass ) )
                {
                    lIconHtml.Text = string.Format( "<i class='{0}' ></i>", _workflowType.IconCssClass );
                }
            }
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
            try
            {
                string role = GetAttributeValue( "Role" );
                if ( string.IsNullOrWhiteSpace( role ) )
                {
                    role = "0";
                }

                string contents = GetAttributeValue( "Contents" );
                string panelTitle = GetAttributeValue( "SetPanelTitle" );
                string panelIcon = GetAttributeValue( "SetPanelIcon" );

                string appRoot = ResolveRockUrl( "~/" );
                string themeRoot = ResolveRockUrl( "~~/" );
                contents = contents.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );

                using ( var rockContext = new RockContext() )
                {
                    List<WorkflowAction> actions = null;
                    if ( role == "1" )
                    {
                        actions = GetWorkflows( rockContext );
                    }
                    else
                    {
                        actions = GetActions( rockContext );
                    }

                    var mergeFields = new Dictionary<string, object>();
                    mergeFields.Add( "Role", role );
                    mergeFields.Add( "Actions", actions.OrderByDescending( a => a.CreatedDateTime ) );
                    mergeFields.Add( "PanelTitle", panelTitle );
                    mergeFields.Add( "PanelIcon", panelIcon );

                    lContents.Text = contents.ResolveMergeFields( mergeFields );
                }

            }
            catch ( Exception ex )
            {
                LogException( ex );
                lContents.Text = "error getting workflows";
            }
        }

        private List<WorkflowAction> GetWorkflows( RockContext rockContext )
        {
            var actions = new List<WorkflowAction>();

            if ( CurrentPerson != null )
            {

                var categoryIds = GetCategories( rockContext );

                var qry = new WorkflowService( rockContext ).Queryable( "WorkflowType" )
                    .Where( w =>
                        w.ActivatedDateTime.HasValue &&
                        !w.CompletedDateTime.HasValue &&
                        w.InitiatorPersonAlias.PersonId == CurrentPerson.Id );

                if ( categoryIds.Any() )
                {
                    qry = qry
                        .Where( w =>
                            w.WorkflowType.CategoryId.HasValue &&
                            categoryIds.Contains( w.WorkflowType.CategoryId.Value ) );
                }

                foreach ( var workflow in qry.OrderBy( w => w.ActivatedDateTime ) )
                {
                    var activity = new WorkflowActivity();
                    activity.Workflow = workflow;

                    var action = new WorkflowAction();
                    action.Activity = activity;

                    actions.Add( action );
                }
            }

            return actions;
        }

        private List<WorkflowAction> GetActions( RockContext rockContext )
        {
            var formActions = new List<WorkflowAction>();

            if ( CurrentPerson != null )
            {

                // Get all of the active form actions that user is assigned to and authorized to view
                formActions = GetActiveForms( rockContext );

                // If a category filter was specified, filter list by selected categories
                var categoryIds = GetCategories( rockContext );
                if ( categoryIds.Any() )
                {
                    formActions = formActions
                        .Where( a =>
                            a.ActionType.ActivityType.WorkflowType.CategoryId.HasValue &&
                            categoryIds.Contains( a.ActionType.ActivityType.WorkflowType.CategoryId.Value ) )
                        .ToList();
                }
            }

            return formActions;
        }

        private List<WorkflowAction> GetActiveForms( RockContext rockContext )
        {
            var formActions = RockPage.GetSharedItem( "ActiveForms" ) as List<WorkflowAction>;
            if ( formActions == null )
            {
                formActions = new WorkflowActionService( rockContext ).GetActiveForms( CurrentPerson );
                RockPage.SaveSharedItem( "ActiveForms", formActions );
            }

            // find first form for each activity
            var firstForms = new List<WorkflowAction>();
            foreach ( var activityId in formActions.Select( a => a.ActivityId ).Distinct().ToList() )
            {
                firstForms.Add( formActions.First( a => a.ActivityId == activityId ) );
            }

            return firstForms;
        }

        private List<int> GetCategories( RockContext rockContext )
        {
            int entityTypeId = EntityTypeCache.Read( typeof( Rock.Model.WorkflowType ) ).Id;

            var selectedCategories = new List<Guid>();
            GetAttributeValue( "Categories" ).SplitDelimitedValues().ToList().ForEach( c => selectedCategories.Add( c.AsGuid() ) );

            bool includeChildCategories = GetAttributeValue( "IncludeChildCategories" ).AsBoolean();

            return GetCategoryIds( new List<int>(), new CategoryService( rockContext ).GetNavigationItems( entityTypeId, selectedCategories, includeChildCategories, CurrentPerson ) );
        }

        private List<int> GetCategoryIds( List<int> ids, List<CategoryNavigationItem> categories )
        {
            foreach ( var categoryNavItem in categories )
            {
                ids.Add( categoryNavItem.Category.Id );
                GetCategoryIds( ids, categoryNavItem.ChildCategories );
            }

            return ids;
        }

        //private bool HydrateObjects()
        //{
        //    LoadWorkflowType();

        //    if ( _workflowType == null )
        //    {
        //    //    ShowNotes( false );
        //    //    ShowMessage( NotificationBoxType.Danger, "Configuration Error", "Workflow type was not configured or specified correctly." );
        //        return false;
        //    }

        //    if ( !_workflowType.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
        //    {
        //    //    ShowNotes( false );
        //    //    ShowMessage( NotificationBoxType.Warning, "Sorry", "You are not authorized to view this type of workflow." );
        //        return false;
        //    }

        //    if ( !( _workflowType.IsActive ?? true ) )
        //    {
        //     //   ShowNotes( false );
        //     //   ShowMessage( NotificationBoxType.Warning, "Sorry", "This type of workflow is not active." );
        //        return false;
        //    }

        //    // If operating against an existing workflow, get the workflow and load attributes
        //    if ( !WorkflowId.HasValue )
        //    {
        //        WorkflowId = PageParameter( "WorkflowId" ).AsIntegerOrNull();
        //        if ( !WorkflowId.HasValue )
        //        {
        //            Guid guid = PageParameter( "WorkflowGuid" ).AsGuid();
        //            if ( !guid.IsEmpty() )
        //            {
        //                _workflow = _workflowService.Queryable()
        //                    .Where( w => w.Guid.Equals( guid ) && w.WorkflowTypeId == _workflowType.Id )
        //                    .FirstOrDefault();
        //                if ( _workflow != null )
        //                {
        //                    WorkflowId = _workflow.Id;
        //                }
        //            }
        //        }
        //    }

        //    if ( WorkflowId.HasValue )
        //    {
        //        if ( _workflow == null )
        //        {
        //            _workflow = _workflowService.Queryable()
        //                .Where( w => w.Id == WorkflowId.Value && w.WorkflowTypeId == _workflowType.Id )
        //                .FirstOrDefault();
        //        }
        //        if ( _workflow != null )
        //        {
        //            hlblWorkflowId.Text = _workflow.WorkflowId;

        //            _workflow.LoadAttributes();
        //            foreach ( var activity in _workflow.Activities )
        //            {
        //                activity.LoadAttributes();
        //            }
        //        }

        //    }

        //    // If an existing workflow was not specified, activate a new instance of workflow and start processing
        //    if ( _workflow == null )
        //    {
        //        string workflowName = PageParameter( "WorkflowName" );
        //        if ( string.IsNullOrWhiteSpace( workflowName ) )
        //        {
        //            workflowName = "New " + _workflowType.WorkTerm;
        //        }

        //        _workflow = Rock.Model.Workflow.Activate( _workflowType, workflowName );
        //        if ( _workflow != null )
        //        {
        //            // If a PersonId or GroupId parameter was included, load the corresponding
        //            // object and pass that to the actions for processing
        //            object entity = null;
        //            int? personId = PageParameter( "PersonId" ).AsIntegerOrNull();
        //            if ( personId.HasValue )
        //            {
        //                entity = new PersonService( _rockContext ).Get( personId.Value );
        //            }
        //            else
        //            {
        //                int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();
        //                if ( groupId.HasValue )
        //                {
        //                    entity = new GroupService( _rockContext ).Get( groupId.Value );
        //                }
        //            }

        //            // Loop through all the query string parameters and try to set any workflow
        //            // attributes that might have the same key
        //            foreach ( var param in RockPage.PageParameters() )
        //            {
        //                if ( param.Value != null && param.Value.ToString().IsNotNullOrWhitespace() )
        //                {
        //                    _workflow.SetAttributeValue( param.Key, param.Value.ToString() );
        //                }
        //            }

        //            List<string> errorMessages;
        //            if ( !_workflowService.Process( _workflow, entity, out errorMessages ) )
        //            {
        //               // ShowNotes( false );
        //              //  ShowMessage( NotificationBoxType.Danger, "Workflow Processing Error(s):",
        //             //       "<ul><li>" + errorMessages.AsDelimited( "</li><li>" ) + "</li></ul>" );
        //                return false;
        //            }
        //            if ( _workflow.Id != 0 )
        //            {
        //                WorkflowId = _workflow.Id;
        //            }
        //        }
        //    }

        //    if ( _workflow == null )
        //    {
        //      //  ShowNotes( false );
        //     //   ShowMessage( NotificationBoxType.Danger, "Workflow Activation Error", "Workflow could not be activated." );
        //        return false;
        //    }

        //    var canEdit = UserCanEdit || _workflow.IsAuthorized( Authorization.EDIT, CurrentPerson );

        //    if ( _workflow.IsActive )
        //    {
        //        if ( ActionTypeId.HasValue )
        //        {
        //            foreach ( var activity in _workflow.ActiveActivities )
        //            {
        //                _action = activity.ActiveActions.Where( a => a.ActionTypeId == ActionTypeId.Value ).FirstOrDefault();
        //                if ( _action != null )
        //                {
        //                    _activity = activity;
        //                    _activity.LoadAttributes();

        //                    _actionType = _action.ActionTypeCache;
        //                    ActionTypeId = _actionType.Id;
        //                    return true;
        //                }
        //            }
        //        }

        //        // Find first active action form
        //        int personId = CurrentPerson != null ? CurrentPerson.Id : 0;
        //        int? actionId = PageParameter( "ActionId" ).AsIntegerOrNull();
        //        foreach ( var activity in _workflow.Activities
        //            .Where( a =>
        //                a.IsActive &&
        //                ( !actionId.HasValue || a.Actions.Any( ac => ac.Id == actionId.Value ) ) &&
        //                (
        //                    ( canEdit ) ||
        //                    ( !a.AssignedGroupId.HasValue && !a.AssignedPersonAliasId.HasValue ) ||
        //                    ( a.AssignedPersonAlias != null && a.AssignedPersonAlias.PersonId == personId ) ||
        //                    ( a.AssignedGroup != null && a.AssignedGroup.Members.Any( m => m.PersonId == personId ) )
        //                )
        //            )
        //            .ToList()
        //            .OrderBy( a => a.ActivityTypeCache.Order ) )
        //        {
        //            if ( canEdit || ( activity.ActivityTypeCache.IsAuthorized( Authorization.VIEW, CurrentPerson ) ) )
        //            {
        //                foreach ( var action in activity.ActiveActions
        //                    .Where( a => ( !actionId.HasValue || a.Id == actionId.Value ) ) )
        //                {
        //                    if ( action.ActionTypeCache.WorkflowForm != null && action.IsCriteriaValid )
        //                    {
        //                        _activity = activity;
        //                        _activity.LoadAttributes();

        //                        _action = action;
        //                        _actionType = _action.ActionTypeCache;
        //                        ActionTypeId = _actionType.Id;
        //                        return true;
        //                    }
        //                }
        //            }
        //        }

        //   //     lSummary.Text = string.Empty;

        //    }
        //    else
        //    {
        //        if ( GetAttributeValue( "ShowSummaryView" ).AsBoolean() && !string.IsNullOrWhiteSpace( _workflowType.SummaryViewText ) )
        //        {
        //            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
        //            mergeFields.Add( "Action", _action );
        //            mergeFields.Add( "Activity", _activity );
        //            mergeFields.Add( "Workflow", _workflow );

        //       //     lSummary.Text = _workflowType.SummaryViewText.ResolveMergeFields( mergeFields, CurrentPerson );
        //         //   lSummary.Visible = true;
        //        }
        //    }

        //    //if ( lSummary.Text.IsNullOrWhiteSpace() )
        //    //{
        //    //    if ( _workflowType.NoActionMessage.IsNullOrWhiteSpace() )
        //    //    {
        //    //        ShowMessage( NotificationBoxType.Warning, string.Empty, "The selected workflow is not in a state that requires you to enter information." );
        //    //    }
        //    //    else
        //    //    {
        //    //        var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
        //    //        mergeFields.Add( "Action", _action );
        //    //        mergeFields.Add( "Activity", _activity );
        //    //        mergeFields.Add( "Workflow", _workflow );
        //    //        ShowMessage( NotificationBoxType.Warning, string.Empty, _workflowType.NoActionMessage.ResolveMergeFields( mergeFields, CurrentPerson ) );
        //    //    }
        //    //}

        //    //ShowNotes( false );
        //    return false;
        //}

        //private void LoadWorkflowType()
        //{
        //    if ( _rockContext == null )
        //    {
        //        _rockContext = new RockContext();
        //    }

        //    if ( _workflowService == null )
        //    {
        //        _workflowService = new WorkflowService( _rockContext );
        //    }

        //    // Get the workflow type id (initial page request)
        //    if ( !WorkflowTypeId.HasValue )
        //    {
        //        // Get workflow type set by attribute value
        //        Guid workflowTypeguid = GetAttributeValue( "WorkflowType" ).AsGuid();
        //        if ( !workflowTypeguid.IsEmpty() )
        //        {
        //            _workflowType = WorkflowTypeCache.Read( workflowTypeguid );
        //        }

        //        // If an attribute value was not provided, check for query/route value
        //        if ( _workflowType != null )
        //        {
        //            WorkflowTypeId = _workflowType.Id;
        //            ConfiguredType = true;
        //        }
        //        else
        //        {
        //            WorkflowTypeId = PageParameter( "WorkflowTypeId" ).AsIntegerOrNull();
        //            ConfiguredType = false;
        //        }
        //    }

        //    // Get the workflow type 
        //    if ( _workflowType == null && WorkflowTypeId.HasValue )
        //    {
        //        _workflowType = WorkflowTypeCache.Read( WorkflowTypeId.Value );
        //    }
        //}


        #endregion
    }
}