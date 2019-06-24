//
// Copyright (C) Spark Development Network - All Rights Reserved
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using com.pushpay.RockRMS;
using com.pushpay.RockRMS.ApiModel;
using com.pushpay.RockRMS.Model;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Utility;
using Rock.VersionInfo;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_pushPay.RockRMS
{
    /// <summary>
    /// Lists all packages or packages for a organizationan.
    /// </summary>
    [DisplayName( "Merchant Listing List" )]
    [Category( "Pushpay" )]
    [Description( "Lists all the merchant listings for a particular Pushpay account." )]

    [LinkedPage( "Detail Page", "Page used to view merchant listing details.", true, "", "", 0)]
    [TextField( "Batch Name Prefix", "The batch prefix name to use when creating a new batch", false, "Pushpay", "", 1 )]
    [LinkedPage( "Batch Detail Page", "The page used to display details of a batch.", false, "", "", 2 )]
    [CampusField( "Default Campus", "The deafult campus to use for new people when their gift is not made a unique campus-specific fund.", true, "", "", 3)]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, "368DD475-242C-49C4-A42C-7278BE690CC2", "", 4 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "", 5 )]
    public partial class MerchantList : Rock.Web.UI.RockBlock
    {

        public int? _accountId { get; set; }

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gMerchants.DataKeyNames = new string[] { "Id" };
            gMerchants.GridRebind += gMerchants_GridRebind;
            gMerchants.Actions.ShowAdd = false;
            gMerchants.IsDeleteEnabled = false;

            _accountId = PageParameter( "AccountId" ).AsIntegerOrNull();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                if ( _accountId.HasValue )
                {
                    BindGrid();
                }
            }
            else
            {
                ShowDialog();
            }

            //Verify that the version of Rock in use can support manual payment downloads.
            CheckVersionCompatibility();

            base.OnLoad( e );
        }

        /// <summary>
        /// Verifies that the version of Rock in use can support manual payment downloads.
        /// This should be removed when the minimum supported version of this plugin is >= 1.8.5.
        /// 
        /// When this code is removed, the nbDownloadNotSupported NotificationBox should also be
        /// removed from the markup file.
        /// </summary>
        [Obsolete( "This code is only necessary to support Rock versions prior to 1.8.5.", false )]
        private void CheckVersionCompatibility()
        {
            bool isDownloadSupported = VersionSupportsManualDownload();

            if ( !isDownloadSupported )
            {
                var downloadColumn = gMerchants.ColumnsOfType<EditField>()
                    .First( c => c.ToolTip.ToLower() == "Download Payments".ToLower() );
                downloadColumn.Visible = false;
                nbDownloadNotSupported.Visible = true;
                var installedVersion = VersionInfo.GetRockSemanticVersionNumber();
                nbDownloadNotSupported.Text = string.Format(
                    "The version of Rock you're using ({0}) does not support manual payment downloads. Please upgrade to Version 8.5 or later to enable this functionality.",
                    installedVersion.Replace( "1.8", "8" )
                );
            }

        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the RowSelected event of the gMerchants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gMerchants_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", new Dictionary<string, string> { { "MerchantId", e.RowKeyId.ToString() } } );
        }

        /// <summary>
        /// Handles the Edit event of the gMerchants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gMerchants_Edit( object sender, RowEventArgs e )
        {
            ddlMemoReferenceField.Items.Clear();
            ddlMemoReferenceField.Items.Add( new ListItem() );

            dvTransactionType.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE.AsGuid() ) );
            var dfltTrnType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() );

            using ( var rockContext = new RockContext() )
            {
                var merchant = new MerchantService( rockContext ).Get( e.RowKeyId );
                if ( merchant != null )
                {
                    var referenceFields = JsonConvert.DeserializeObject<List<ReferenceDefinition>>( merchant.ReferenceFieldsJson );
                    if ( referenceFields != null )
                    {
                        foreach ( var field in referenceFields.OrderBy( f => f.Order ) )
                        {
                            ddlMemoReferenceField.Items.Add( new ListItem( field.Label, field.Id.ToString() ) );
                        }
                    }

                    hfMerchantId.Value = merchant.Id.ToString();
                    if ( merchant.MemoReferenceFieldId != null )
                    {
                        ddlMemoReferenceField.SetValue( merchant.MemoReferenceFieldId.Value.ToString() );
                    }
                    apDefaultAccount.SetValue( merchant.DefaultFinancialAccount );
                    cbActive.Checked = merchant.IsActive;
                    ceBatchSuffix.Text = merchant.BatchNameSuffix;
                    dvTransactionType.SetValue( merchant.TransactionTypeId ?? dfltTrnType.Id );

                    mdEditMerchantSettings.Title = merchant.Name + " Settings";
                    ShowDialog( "SETTINGS", true );
                }
            }
        }

        /// <summary>
        /// Handles the Download event of the gMerchants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gMerchants_Download( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var merchant = new MerchantService( rockContext ).Get( e.RowKeyId );
                if ( merchant != null )
                {
                    hfMerchantId.Value = merchant.Id.ToString();

                    if ( merchant.LastDownloadToDate.HasValue )
                    { 
                        drpDates.LowerValue = merchant.LastDownloadToDate.Value.Date;
                        drpDates.UpperValue = RockDateTime.Today.Date;
                    }
                    else
                    {
                        drpDates.LowerValue = null;
                        drpDates.UpperValue = null;
                    }

                    nbDownload.Text = string.Empty;
                    nbDownload.Visible = false;

                    ShowDialog( "DOWNLOAD", true );
                }
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the mdEditMerchantSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdEditMerchantSettings_SaveClick( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var merchantService = new MerchantService( rockContext );
                var fundService = new MerchantFundService( rockContext );

                Merchant merchant = null;

                int? merchantId = hfMerchantId.Value.AsIntegerOrNull();
                if ( merchantId.HasValue )
                {
                    merchant = merchantService.Get( merchantId.Value );
                    if ( merchant != null )
                    {
                        merchant.MemoReferenceFieldId = ddlMemoReferenceField.SelectedValueAsInt();
                        merchant.MemoReferenceFieldName = ddlMemoReferenceField.SelectedItem != null ? ddlMemoReferenceField.SelectedItem.Text : string.Empty;
                        merchant.DefaultFinancialAccountId = apDefaultAccount.SelectedValueAsId();
                        merchant.IsActive = cbActive.Checked;
                        merchant.BatchNameSuffix = ceBatchSuffix.Text;
                        merchant.TransactionTypeId = dvTransactionType.SelectedValueAsInt();

                        rockContext.SaveChanges();

                        Merchant.RefreshFunds( merchant.Id );
                    }
                }
            }

            HideDialog();
            BindGrid();

        }

        /// <summary>
        /// Handles the Click event of the btnDownload control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDownload_Click( object sender, EventArgs e )
        {
            int? defaultcampusId = null;
            var campus = CampusCache.All().FirstOrDefault( c => c.Guid.Equals( GetAttributeValue( "DefaultCampus" ).AsGuid() ) );
            if ( campus != null )
            {
                defaultcampusId = campus.Id;
            }
            var connectionStatus = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
            var recordStatus = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() );

            string batchNamePrefix = GetAttributeValue( "BatchNamePrefix" );

            DateTime? startDateTime = drpDates.LowerValue;
            DateTime? endDateTime = drpDates.UpperValue;

            using ( var rockContext = new RockContext() )
            {
                var merchantService = new MerchantService( rockContext );
                var merchant = merchantService.Get( hfMerchantId.ValueAsInt() );
                if ( merchant != null && startDateTime.HasValue && endDateTime.HasValue )
                {
                    if ( merchant.IsActive && 
                        merchant.Account.IsActive  &&
                        merchant.DefaultFinancialAccount != null && 
                        merchant.DefaultFinancialAccount.IsActive )
                    {
                        var financialGateway = new FinancialGatewayService( rockContext )
                            .Queryable()
                            .FirstOrDefault( g =>
                                g.EntityType != null &&
                                g.EntityType.Name == "com.pushpay.RockRMS.Gateway" );

                        if ( financialGateway != null )
                        {
                            financialGateway.LoadAttributes( rockContext );

                            DateTime start = startDateTime.Value;
                            DateTime end = endDateTime.Value;

                            var qryParam = new Dictionary<string, string>();
                            qryParam.Add( "batchId", "9999" );
                            string batchUrlFormat = LinkedPageUrl( "BatchDetailPage", qryParam ).Replace( "9999", "{0}" );

                            string resultSummary = MerchantService.ProcessPayments( financialGateway, merchant, batchNamePrefix, batchUrlFormat,
                                defaultcampusId, recordStatus, connectionStatus, startDateTime.Value, endDateTime.Value );

                            if ( !string.IsNullOrWhiteSpace( resultSummary ) )
                            {
                                nbDownload.Text = string.Format( "<ul>{0}</ul>", resultSummary );
                                nbDownload.NotificationBoxType = NotificationBoxType.Success;
                            }
                            else
                            {
                                nbDownload.Text = string.Format( "There were not any transactions downloaded.", resultSummary );
                                nbDownload.NotificationBoxType = NotificationBoxType.Warning;
                            }
                        }
                        else
                        {
                            nbDownload.Text = "The Pushpay financial gateway could not be loaded.";
                            nbDownload.NotificationBoxType = NotificationBoxType.Danger;
                        }
                    }
                    else
                    {
                        nbDownload.Text = "The selected Merchant Listing is not active, or it's account is not active, or it does not have an active default account!";
                        nbDownload.NotificationBoxType = NotificationBoxType.Danger;
                    }

                }
                else
                {
                    nbDownload.Text = "The merchant listing, start, and/or end times could not be determined.";
                    nbDownload.NotificationBoxType = NotificationBoxType.Warning;
                }
            }

            nbDownload.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the btnRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRefresh_Click( object sender, EventArgs e )
        {
            Account.RefreshMerchants( _accountId );
            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gMerchants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gMerchants_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks the version of Rock to notify users that they need to upgrade if they are running
        /// a version that doesn't support manually downloading payments.
        /// 
        /// Reason:  A bug which prevents the download from functioning was introduced in 1.8.0 and
        /// resolved in 1.8.5.
        /// </summary>
        [Obsolete("This code is only necessary to support Rock versions prior to 1.8.5.", false)]
        private bool VersionSupportsManualDownload()
        {
            RockSemanticVersion rockVersion = RockSemanticVersion.Parse( VersionInfo.GetRockSemanticVersionNumber() );
            if ( rockVersion >= RockSemanticVersion.Parse("1.8.0") && rockVersion < RockSemanticVersion.Parse("1.8.5") )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            if ( _accountId.HasValue )
            {
                var service = new AccountService( new RockContext() );
                var sortProperty = gMerchants.SortProperty;

                var account = service.Get( _accountId.Value );
                if ( account != null )
                {
                    lAccountName.Text = account.Name;

                    var qry = account.Merchants.AsQueryable();

                    if ( sortProperty != null )
                    {
                        qry = qry.Sort( sortProperty );
                    }
                    else
                    {
                        qry = qry.OrderBy( c => c.Name );
                    }

                    gMerchants.DataSource = qry
                        .ToList()
                        .Select( m => new
                            {
                                m.Id,
                                m.Name,
                                MemoField = m.MemoReferenceFieldName,
                                DefaultAccount = m.DefaultFinancialAccount != null ?
                                    m.DefaultFinancialAccount.Name :
                                    "<span data-toggle='tooltip' title='A default account has not been selected for the funds' class='label label-danger'>None</span>",
                                m.IsActive,
                                Funds = m.FundHtmlBadge
                            } )
                        .ToList();
                    gMerchants.DataBind();
                }
            }
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( string dialog, bool setValues = false )
        {
            hfActiveDialog.Value = dialog.ToUpper().Trim();
            ShowDialog( setValues );
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( bool setValues = false )
        {
            switch ( hfActiveDialog.Value )
            {
                case "SETTINGS":
                    mdEditMerchantSettings.Show();
                    break;
                case "DOWNLOAD":
                    mdDownload.Show();
                    break;
            }
        }

        /// <summary>
        /// Hides the dialog.
        /// </summary>
        private void HideDialog()
        {
            switch ( hfActiveDialog.Value )
            {
                case "SETTINGS":
                    mdEditMerchantSettings.Hide();
                    break;
                case "DOWNLOAD":
                    mdDownload.Hide();
                    break;
            }

            hfMerchantId.Value = string.Empty;
            hfActiveDialog.Value = string.Empty;
        }


        #endregion

        protected void mdDownload_SaveClick( object sender, EventArgs e )
        {
            HideDialog();
        }
}
}