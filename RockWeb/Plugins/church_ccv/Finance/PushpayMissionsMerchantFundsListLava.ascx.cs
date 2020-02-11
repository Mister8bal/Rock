using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Finance
{
    /// <summary>
    /// Block that merges Rock financial accounts that are connected to Pushpay funds and merges into a lava template.
    /// </summary>
    [DisplayName( "Pushpay Missions Merchant Funds List Lava" )]
    [Category( "CCV > Finance" )]
    [Description( "Selects active Pushpay Missions merchant funds that are linked to Rock Financial Accounts and merges into a lava template." )]
    [CustomEnhancedListField( 
        "Accounts",
        "Available Pushpay Missions Accounts.",
        @"SELECT PPMF.Name as Text,
	            PPMF.Guid as Value 
            FROM [_com_pushPay_RockRMS_MerchantFund] PPMF
            INNER JOIN _com_pushPay_RockRMS_Merchant PPM on PPM.Id = PPMF.MerchantId
            WHERE PPMF.FinancialAccountId IS NOT NULL AND PPM.MerchantKey = 'MzU0MjIxNTc5ODpETHY1dDd1Qjg0d3lUcEZDYnNmaDd2OFdtN1E'",
        true,
        "",
        "",
        0,
        AttributeKey.SelectedFunds )]
    [CodeEditorField(
        "Lava Template",
        "Lava template to use to display content",
        CodeEditorMode.Lava,
        CodeEditorTheme.Rock,
        400,
        true,
        "",
        "",
        1,
        AttributeKey.LavaTemplate )]

    public partial class PushpayMissionsMerchantFundsListLava : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string SelectedFunds = "SelectedFunds";
            public const string LavaTemplate = "LavaTemplate";
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
            this.AddConfigurationUpdateTrigger(upnlContent);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (!Page.IsPostBack)
            {
                LoadContent();
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
            LoadContent();
        }

        #endregion

        #region Methods

        protected void LoadContent()
        {
            // get pushpay merchant fund guids from block setting
            List<Guid> selectedFundGuids = GetAttributeValue( AttributeKey.SelectedFunds ).SplitDelimitedValues().ToList().AsGuidList();

            // load Pushpay merchant funds from the selected guids
            var fundsQry = new Service<com.pushpay.RockRMS.Model.MerchantFund>( new RockContext() )
                    .Queryable()
                    .Where( a => selectedFundGuids.Contains( a.Guid ) );

            // Add to helper classes and merge into lava template
            List<MerchantFund> funds = new List<MerchantFund>();
            foreach ( var fund in fundsQry )
            {
                funds.Add( new MerchantFund()
                    {
                        Id = fund.Id,
                        Name = fund.Name,
                        FundKey = fund.FundKey
                    } );
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Funds", funds );

            string template = GetAttributeValue( AttributeKey.LavaTemplate );
            lContent.Text = template.ResolveMergeFields( mergeFields );
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Lightweight object for each account item
        /// </summary>
        [Serializable]
        [DotLiquid.LiquidType( "Id", "Name", "FundKey" )]
        protected class MerchantFund
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string FundKey { get; set; }

            public MerchantFund(  )
            {
            }
        }

        #endregion
    }
}
