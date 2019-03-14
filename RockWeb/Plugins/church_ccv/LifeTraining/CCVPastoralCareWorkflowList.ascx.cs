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
using Rock.Web.UI;
using System.Data.Entity;

namespace RockWeb.Plugins.church_ccv.Workflow
{
    /// <summary>
    /// Block to display active workflow activities assigned to the current user that have a form entry action.  The display format is controlled by a lava template.
    /// </summary>
    [DisplayName( "CCV Pastoral Care Workflow List" )]
    [Category( "CCV > Workflow" )]
    [Description( "Block to display Pastoral Care Workflows submitted by a user." )]

    [CodeEditorField( "Contents", @"The Lava template to use for displaying activities assigned to current user.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false, @"{% include '~/Plugins/church_ccv/LifeTraining/CCVPastoralCareWorkflowList.lava' %}", "", 3 )]
    public partial class CCVPastoralCareWorkflowList : PersonBlock
    {
        #region Properties

        const int PastoralCareWorkflowTypeId = 255;

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
                string contents = GetAttributeValue( "Contents" );

                string appRoot = ResolveRockUrl( "~/" );
                string themeRoot = ResolveRockUrl( "~~/" );
                contents = contents.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );

                using ( var rockContext = new RockContext() )
                {
                    List<WorkflowAction> actions = null;
                    
                    actions = GetWorkflowActions( rockContext );

                    var mergeFields = new Dictionary<string, object>();
                    mergeFields.Add( "Actions", actions.OrderByDescending( a => a.CreatedDateTime ) );

                    lContents.Text = contents.ResolveMergeFields( mergeFields );
                }
            }
            catch ( Exception ex )
            {
                LogException( ex );
                lContents.Text = "Error Loading Pastoral Care Requests";
            }
        }

        private List<WorkflowAction> GetWorkflowActions( RockContext rockContext )
        {
            var actions = new List<WorkflowAction>();

            // get all pastoral care workflow instances
            var workflowQuery = new WorkflowService( rockContext ).Queryable( "WorkflowType" )
                .Where( w =>
                    w.ActivatedDateTime.HasValue &&
                    w.WorkflowTypeId == PastoralCareWorkflowTypeId ).AsNoTracking();

            // We want to get all Pastoral Care workflows where the Submitter is the Person in this PersonBlock.
            // Because Submitter is an attribute value, we need to join and query at the database level for performance reasons.

            // Now, since the Submitter Attribute Value depends on the Attribute table, we need to join those two tables, and then join that to the query we built above.
            var attribQuery = new AttributeService( rockContext ).Queryable().AsNoTracking();
            var avQuery = new AttributeValueService( rockContext ).Queryable().AsNoTracking();
            var attribWithValue = attribQuery.Join( avQuery, a => a.Id, av => av.AttributeId, ( a, av ) => new { Attribute = a, AttribValue = av } )
                                                .Where( a => a.Attribute.EntityTypeQualifierColumn.Equals( "WorkflowTypeId", StringComparison.OrdinalIgnoreCase ) );

            // join the attributeValues so we can find all workflows with a Submitter that matches the person this block is being used for.
            List<Rock.Model.Workflow> workflowList = workflowQuery.Join( attribWithValue, wf => wf.Id, av => av.AttribValue.EntityId, ( wf, av ) => new { WF = wf, AV = av } )
                                            .Where( a => a.AV.Attribute.Key == "Submitter" && a.AV.AttribValue.ValueAsPersonId == Person.Id )
                                            .Select( a => a.WF )
                                            .OrderBy( w => w.ActivatedDateTime )
                                            .ToList();

            foreach ( var workflow in workflowList )
            {
                var activity = new WorkflowActivity();
                activity.Workflow = workflow;

                var action = new WorkflowAction();
                action.Activity = activity;

                actions.Add( action );
            }

            return actions;
        }
        #endregion
    }
}