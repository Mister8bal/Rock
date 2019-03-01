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
//using Rock.Workflow;

namespace RockWeb.Plugins.church_ccv.Workflow
{
    /// <summary>
    /// Block to display active workflow activities assigned to the current user that have a form entry action.  The display format is controlled by a lava template.
    /// </summary>
    [DisplayName( "CCV My Workflows Lava" )]
    [Category( "CCV > Workflow" )]
    [Description( "Block to display active workflow activities assigned to the current user that have a form entry action.  The display format is controlled by a lava template." )]

    [WorkflowTypeField( "Workflow Type", "Type of workflow to start." )]

    [CustomRadioListField( "Role", "Display the active workflows that the current user Initiated, or is currently Assigned To.", "0^Assigned To,1^Initiated", false, "0", "", 0 )]
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
        private Rock.Model.Workflow _workflow = null;
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

             //   var workflowTypes = LoadWorkflowType( rockContext );

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

                    // Get the workflow type id (initial page request)
                    if ( !WorkflowTypeId.HasValue )
                    {
                        // Get workflow type set by attribute value
                        Guid workflowTypeguid = GetAttributeValue( "WorkflowType" ).AsGuid();
                        if ( !workflowTypeguid.IsEmpty() )
                        {
                            _workflowType = WorkflowTypeCache.Read( workflowTypeguid );
                        }

                        // If an attribute value was not provided, check for query/route value
                        if ( _workflowType != null )
                        {
                            WorkflowTypeId = _workflowType.Id;
                            ConfiguredType = true;
                        }
                        else
                        {
                            WorkflowTypeId = PageParameter( "WorkflowTypeId" ).AsIntegerOrNull();
                            ConfiguredType = false;
                        }
                    }

                    // Get the workflow type 
                    if ( _workflowType == null && WorkflowTypeId.HasValue )
                    {
                        _workflowType = WorkflowTypeCache.Read( WorkflowTypeId.Value );
                    }

                    if ( workflow.WorkflowTypeId == WorkflowTypeId )
                    {

                        var activity = new WorkflowActivity();
                        activity.Workflow = workflow;

                        var action = new WorkflowAction();
                        action.Activity = activity;

                        actions.Add( action );
                    }
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

            var test = new WorkflowService( rockContext );

            var workflow = test.Get(guid)




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

        #endregion
    }
}