
using System;
using System.ComponentModel;
using Rock.Model;
using Rock.Web.UI;
using Rock.Data;
using church.ccv.PersonalizationEngine.Model;
using Rock.Web.UI.Controls;
using Rock;
using Rock.Security;
using System.Linq;
using System.Data.Entity;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Rock.Attribute;
using church.ccv.PersonalizationEngine.Data;

namespace RockWeb.Plugins.church_ccv.PersonalizationEngine
{
    [DisplayName( "Campaign List" )]
    [Category( "CCV > Personalization Engine" )]
    [Description( "Displays existing campaigns in a list." )]

    [LinkedPage("Detail Page")]
    public partial class CampaignList : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            CampaignFilter_Init( );
            CampaignGrid_Init( );
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
                CampaignFilter_Bind( );
                CampaignGrid_Bind( );
            }
        }

        #region Campaign Filter
        protected void CampaignFilter_Init( )
        {
            rCampaignFilter.ApplyFilterClick += CampaignFilter_ApplyFilterClick;
            rCampaignFilter.DisplayFilterValue += CampaignFilter_DisplayFilterValue;

            // populate the types checkbox list filter
            filterCblType.Items.Clear();

            using ( RockContext rockContext = new RockContext( ) )
            {
                var campaignTypes = new Service<CampaignType>( rockContext ).Queryable( ).AsNoTracking( ).Select( ct => new { Id = ct.Id, Type = ct.Name } );
                filterCblType.DataSource = campaignTypes.ToList( );
                filterCblType.DataBind( );
            }
        }   

        protected void CampaignFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rCampaignFilter.SaveUserPreference( "Title", filterTbTitle.Text );
            rCampaignFilter.SaveUserPreference( "Dates", filterDrpDates.DelimitedValues );
            rCampaignFilter.SaveUserPreference( "Types", filterCblType.SelectedValues.AsDelimited( ";" ) );

            CampaignGrid_Bind( );
        }

        protected void CampaignFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch( e.Key )
            {
                case "Title":
                {
                    e.Value = filterTbTitle.Text;
                    break;
                }

                case "Dates":
                {
                    // first, are BOTH values set?
                    if ( filterDrpDates.LowerValue != null && filterDrpDates.UpperValue != null )
                    {
                        // if both values are before today....
                        if( filterDrpDates.LowerValue.Value.Date < DateTime.Now && filterDrpDates.UpperValue.Value.Date < DateTime.Now )
                        {
                            e.Value = "Campaigns that ran during ";
                        }
                        // if both values are in the future...
                        else if( filterDrpDates.LowerValue.Value.Date > DateTime.Now && filterDrpDates.UpperValue.Value.Date > DateTime.Now )
                        {
                            e.Value = "Campaigns that will run ";
                        }
                        else
                        {
                            e.Value = "Campaigns running ";
                        }

                        e.Value += string.Format( "{0:M/dd/yy} thru {1:M/dd/yy}", filterDrpDates.LowerValue, filterDrpDates.UpperValue );
                    }
                    // otherwise only look for a left-hand (starting) date, and filter on that
                    else if ( filterDrpDates.LowerValue != null )
                    {
                        e.Value = "Campaigns running on " + string.Format( "{0:M/dd/yy}", filterDrpDates.LowerValue );
                    }
                    // otherwise look for a right-hand (ending) date
                    else if ( filterDrpDates.UpperValue != null )
                    {
                       e.Value = "Picking only an end date is not supported.";
                    }
                    else
                    {
                        e.Value = string.Empty;
                    }
                    break;
                }

                case "Types":
                {
                    e.Value = ResolveValues( e.Value, filterCblType );
                    break;
                }

                default:
                {
                    e.Value = string.Empty;
                    break;
                }
            }
        }

        private string ResolveValues( string values, CheckBoxList checkBoxList )
        {
            var resolvedValues = new List<string>();

            foreach ( string value in values.Split( ';' ) )
            {
                var item = checkBoxList.Items.FindByValue( value );
                if ( item != null )
                {
                    resolvedValues.Add( item.Text );
                }
            }

            return resolvedValues.AsDelimited( ", " );
        }

        protected void CampaignFilter_Bind( )
        {
            filterTbTitle.Text = rCampaignFilter.GetUserPreference( "Title" );
            filterDrpDates.DelimitedValues = rCampaignFilter.GetUserPreference( "Dates" );
            filterCblType.SetValues( rCampaignFilter.GetUserPreference( "Types" ).SplitDelimitedValues() );
        }
        #endregion

        #region Campaign Grid
        protected void CampaignGrid_Init( )
        {
            gCampaignGrid.DataKeyNames = new string[] { "Id" };

            // turn on only the 'add' button
            gCampaignGrid.Actions.Visible = true;
            gCampaignGrid.Actions.Enabled = true;
            gCampaignGrid.Actions.ShowBulkUpdate = false;
            gCampaignGrid.Actions.ShowCommunicate = false;
            gCampaignGrid.Actions.ShowExcelExport = false;
            gCampaignGrid.Actions.ShowMergePerson = false;
            gCampaignGrid.Actions.ShowMergeTemplate = false;
            
            gCampaignGrid.Actions.ShowAdd = IsUserAuthorized( Authorization.EDIT );
            gCampaignGrid.Actions.AddClick += CampaignGrid_AddClick;

            gCampaignGrid.GridRebind += CampaignGrid_GridRebind;
        }

        protected void CampaignGrid_AddClick( object sender, EventArgs e )
        {
            NavigateToDetailPage( null );
        }

        protected void CampaignGrid_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToDetailPage( e.RowKeyId );
        }

        protected void CampaignGrid_Remove( object sender, RowEventArgs e )
        {
            PersonalizationEngineUtil.DeleteCampaign( e.RowKeyId );

            CampaignGrid_Bind( );
        }

        protected void CampaignGrid_GridRebind( object sender, GridRebindEventArgs e )
        {
            CampaignGrid_Bind( );
        }

        protected void CampaignGrid_Bind( )
        {
            // Grab all the campaigns
            using ( RockContext rockContext = new RockContext( ) )
            {
                var campaignQuery = new Service<Campaign>( rockContext ).Queryable( );

                // ---- Apply Filters ----

                // --Title
                if( string.IsNullOrWhiteSpace( filterTbTitle.Text ) == false )
                {
                    campaignQuery = campaignQuery.Where( c => c.Name.ToLower( ).Contains( filterTbTitle.Text.ToLower( ) ) );
                }

                // --Date
                // our date filtering logic changes based on what they selected.

                // they picked  a start & end date.
                // What they likely want are campaigns that were running within that time frame.
                if(  filterDrpDates.LowerValue != null && filterDrpDates.UpperValue != null )
                {
                    DateTime? lowerValue = filterDrpDates.LowerValue.Value.Date;
                    DateTime? upperValue = filterDrpDates.UpperValue.Value.Date;
                                        
                    // campaigns that were running DURING this range of time -
                    // a start date that falls before the selected END, and an end date that falls after the selected START
                    campaignQuery = campaignQuery.Where( c => DbFunctions.TruncateTime(c.StartDate) <= upperValue && 
                                                        ( c.EndDate.HasValue == false || DbFunctions.TruncateTime(c.EndDate.Value) >= lowerValue ) );
                }
                // they picked ONLY a starting date.
                // What they likely want are campaigns that were running ON THIS DATE.
                else if ( filterDrpDates.LowerValue != null )
                {
                    DateTime lowerValue = filterDrpDates.LowerValue.Value.Date;

                    // make sure that the campaign was running DURING the date selected - 
                    // a start date on or before the selected date, and an end date on or AFTER the selected date.
                    campaignQuery = campaignQuery.Where( c => DbFunctions.TruncateTime(c.StartDate) <= lowerValue && 
                                                        ( c.EndDate.HasValue == false || DbFunctions.TruncateTime(c.EndDate) >= lowerValue ) );
                }
                // we don't support JUST picking an end date, because it doesn't really add any value. It's just confusing.

                // --Type
                if ( filterCblType.SelectedValues.Count( ) > 0 )
                {
                    // take the values in the filter, and see if any of them are found in the Campaign Type's comma delimited list.
                    // Example: Filter has one item checked: "MobileAppNewsFeed"
                    // The campaign has "MobileAppNewsFeed,WebsiteCard"
                    // Calling .Any on SelectedValues will take "MobileAppNewsFeed" and see if that string exists within c.Type, which it does.
                    campaignQuery = campaignQuery.Where( c => filterCblType.SelectedValues.Any( s => c.Type.Contains( s ) ) );
                }

                // ---- Load Data ----
                var dataSource = campaignQuery.Select( c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Type = c.Type,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Priority = c.Priority
                    });

                // --- Sorting ---
                if( gCampaignGrid.SortProperty != null )
                {
                    dataSource = dataSource.Sort( gCampaignGrid.SortProperty );
                }

                gCampaignGrid.DataSource = dataSource.ToList( );
                gCampaignGrid.DataBind( );
            }
        }
        #endregion

        #region Utility
        protected void NavigateToDetailPage( int? campaignId )
        {
            var qryParams = new Dictionary<string, string>();
            int campaignQueryId = campaignId.HasValue ? campaignId.Value : 0;
            qryParams.Add( "CampaignId", campaignQueryId.ToString( ) );
            
            NavigateToLinkedPage( "DetailPage", qryParams );
        }
        #endregion
    }
}