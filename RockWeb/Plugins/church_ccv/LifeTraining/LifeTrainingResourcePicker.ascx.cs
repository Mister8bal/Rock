using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.LifeTraining
{
    /// <summary>
    /// Loads the Referred Counselors and Referred Resources Content CHannels
    /// </summary>
    [DisplayName( "Life Training Resource Picker" )]
    [Category( "CCV > Life Training" )]
    [Description( "Loads two content channels (Counselors & Resources)." )]

    [CodeEditorField( "Contents", @"The Lava template to use for displaying the workflows.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false, "", "", 3 )]

    public partial class LifeTrainingResourcePicker : RockBlock
    {
        #region Properties

        private static int _contentChannelId_Counselors = 320;
        private static int _contentChannelId_Resources = 323;

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
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
                ShowDetail();
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        public void ShowDetail()
        {
            string contents = GetAttributeValue( "Contents" );

            using ( var rockContext = new RockContext() )
            {
                ContentChannelItemService contentChannelItemService = new ContentChannelItemService( rockContext );
                var counselorContentChannelItems = contentChannelItemService
                                    .Queryable()
                                    .AsNoTracking()
                                    .Where( c => c.ContentChannelId == _contentChannelId_Counselors );

                var resourceContentChannelItems = contentChannelItemService
                                   .Queryable()
                                   .AsNoTracking()
                                   .Where( c => c.ContentChannelId == _contentChannelId_Resources );

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                mergeFields.Add( "Counselors", counselorContentChannelItems );
                mergeFields.Add( "Resources", resourceContentChannelItems );
                lContents.Text = contents.ResolveMergeFields( mergeFields );
            }
        }
        #endregion
    }
}