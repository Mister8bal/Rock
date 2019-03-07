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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Core
{
    /// <summary>
    /// Block that can be used to set the default campus context for the site
    /// </summary>
    [DisplayName( "CCV Campus Context Setter" )]
    [Category( "CCV > Core" )]
    [Description( "Block that can be used to force campus context on a page.  If campus attribute is not set, block will clear current campus context" )]
    [CodeEditorField( "Display Lava", "The Lava template to use when displaying the current campus.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, @"
{% if Campus %}
    Campus: {{Campus.Name}}
{% else %}
    Missing Campus.
{% endif %}" )]
    [CustomRadioListField( "Context Scope", "The scope of context to set", "Site,Page", true, "Site" )]
    [CampusField( "Campus","Campus to set context from", false, "", "" )]
    public partial class CCVCampusContextSetter : RockBlock
    {
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

            SetCampus();
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            SetCampus();
        }

        #endregion

        #region Methods

        private void SetCampus()
        {
            // setup context and services
            RockContext rockContext = new RockContext();
            CampusService campusService = new CampusService( rockContext );

            // get campus from current context
            var campusEntityType = EntityTypeCache.Read( "Rock.Model.Campus" );
            var currentCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

            // get block settings
            bool pageScope = GetAttributeValue( "ContextScope" ) == "Page";
            Campus campus = campusService.Get( GetAttributeValue( "Campus" ).AsGuid() ) as Campus;

            // check if campus block attribute value exists
            if ( campus != null )
            {
                // campus attribute value exists
                // ensure no current campus or current campus is different from campus - prevents infinite loop
                if ( currentCampus == null || currentCampus.Id != campus.Id )
                {
                    // set campus context
                    RockPage.SetContextCookie( campus, pageScope, true );
                }
            } else
            {
                // campus attribute value does not exist
                // ensure current campus context exists - prevents infinite loop
                if ( currentCampus != null )
                {
                    // campus attribute value does not exist
                    // clear campus context
                    RockPage.ClearContextCookie( campusEntityType.GetEntityType(), pageScope, true );
                }
            }

            // display output
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Campus", campus );

            lOutput.Text = GetAttributeValue( "DisplayLava" ).ResolveMergeFields( mergeFields );
        }

        #endregion
    }
}