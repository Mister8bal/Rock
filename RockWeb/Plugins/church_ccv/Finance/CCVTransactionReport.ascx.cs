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
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.church_ccv.Finance
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "CCV Transaction Report" )]
    [Category( "CCV > Finance" )]
    [Description( "Block that reports transactions for the currently logged in user with filters." )]
    [TextField( "Transaction Label", "The label to use to describe the transactions (e.g. 'Gifts', 'Donations', etc.)", true, "Gifts", "", 1 )]
    [TextField( "Account Label", "The label to use to describe accounts.", true, "Accounts", "", 2 )]
    [AccountsField( "Accounts", "List of accounts to allow the person to view", false, "", "", 3 )]
    [BooleanField( "Include Child Accounts", "Include child accounts for any account selected.", true, "", 4 )]
    [BooleanField( "Show Account Checkbox Filter", "When specific accounts are selected, display a checkbox list allowing the user to filter.", true, "", 5 )]
    [BooleanField( "Show Transaction Code", "Show the transaction code column in the table.", true, "", 6, "ShowTransactionCode" )]
    [BooleanField( "Show Foreign Key", "Show the transaction foreign key column in the table.", true, "", 7, "ShowForeignKey" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE, "Transaction Types", "Optional list of transation types to limit the list to (if none are selected all types will be included).", false, true, "", "", 8 )]
    [BooleanField( "Use Person Context", "Determines if the person context should be used instead of the CurrentPerson.", false, order: 9 )]

    [ContextAware]
    public partial class CCVTransactionReport : Rock.Web.UI.RockBlock
    {
        #region Properties

        /// <summary>
        /// Gets the target person.
        /// </summary>
        /// <value>
        /// The target person.
        /// </value>
        protected Person TargetPerson { get; private set; }

        #endregion


        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( GetAttributeValue( "UsePersonContext" ).AsBoolean() )
            {
                TargetPerson = ContextEntity<Person>();
            }
            else
            {
                TargetPerson = CurrentPerson;
            }

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
            cblAccounts.Label = GetAttributeValue( "AccountLabel" );
            gTransactions.DataKeyNames = new string[] { "Id" };
            gTransactions.RowItemText = GetAttributeValue( "TransactionLabel" );
            gTransactions.EmptyDataText = string.Format( "No {0} found with the provided criteria.", GetAttributeValue( "TransactionLabel" ).ToLower() );
            gTransactions.GridRebind += gTransactions_GridRebind;

            gTransactions.Actions.ShowMergeTemplate = false;
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
                // set default date range
                drpFilterDates.LowerValue = new DateTime( RockDateTime.Now.Year, 1, 1 );
                drpFilterDates.UpperValue = RockDateTime.Now;

                // load account list
                LoadAccounts();

                BindGrid();
            }
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
            //
        }

        /// <summary>
        /// Handles the GridRebind event of the gTransactions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gTransactions_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the bbtnApply control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bbtnApply_Click( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the accounts.
        /// </summary>
        class ConfiguredFinancialAccount
        {
            public int Id;
            public string PublicName;
        }

        private void LoadAccounts()
        {
            var rockContext = new RockContext();
            IQueryable<FinancialAccount> accountServiceQuery = new FinancialAccountService( rockContext ).Queryable().AsNoTracking();

            List<Guid> selectedAccounts = new List<Guid>();

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "Accounts" ) ) )
            {
                selectedAccounts = GetAttributeValue( "Accounts" ).Split( ',' ).AsGuidList();
            }

            // if there are selected accounts, we'll build a list into the CBLAccounts and check for child accounts
            if ( selectedAccounts.Count > 0 )
            {
                // this will contain our entire account list with all children (if selected)
                var fullAccountList = new List<ConfiguredFinancialAccount>();

                // take the selected accounts 
                var configuredAccounts = accountServiceQuery
                                                    .Where( a => selectedAccounts.Contains( a.Guid ) )
                                                    .OrderBy( a => a.Order )
                                                    .Select( a => new ConfiguredFinancialAccount
                                                    {
                                                        Id = a.Id,
                                                        PublicName = a.PublicName
                                                    } ).ToList();


                
                // get child accounts?
                bool includeChildAccounts = GetAttributeValue( "IncludeChildAccounts" ).AsBoolean();
                if ( includeChildAccounts == true )
                {
                    var childAccounts = new List<ConfiguredFinancialAccount>();

                    // go thru each top level account and recursively get all children
                    foreach ( var account in configuredAccounts )
                    {
                        var accountsFound = GetAccountsRecursive( account.Id, accountServiceQuery );
                        childAccounts.AddRange( accountsFound );
                    }

                    fullAccountList.AddRange( childAccounts );
                }
                else
                {
                    // if no children were selected, we can just make the full account list the configured accounts
                    fullAccountList = configuredAccounts;
                }

                // build the cblAccounts list with all accounts found
                if ( fullAccountList.Any() )
                {
                    foreach ( var account in fullAccountList )
                    {
                        ListItem checkbox = new ListItem( account.PublicName, account.Id.ToString(), true );
                        checkbox.Selected = true;

                        cblAccounts.Items.Add( checkbox );
                    }
                }
                else
                {
                    cblAccounts.Items.Clear();
                }

                // only show Account Checkbox list if there are accounts are configured for the block AND the option is enabled
                bool showAccountCbl = GetAttributeValue( "ShowAccountCheckboxFilter" ).AsBoolean();
                cblAccounts.Visible = ( showAccountCbl && fullAccountList.Any() );
            }
            else
            {
                cblAccounts.Visible = false;
            }
        }

        private List<ConfiguredFinancialAccount> GetAccountsRecursive( int financialAccountId, IQueryable<FinancialAccount> faQuery )
        {
            List<ConfiguredFinancialAccount> foundAccounts = new List<ConfiguredFinancialAccount>();

            // for the account Id passed in, get its account data, and a list of its child Ids
            var financialAccount = faQuery.Where( fa => fa.Id == financialAccountId )
                                          .Select( fa => new {
                                                                fa.Id,
                                                                fa.PublicName,
                                                                ChildAccountIds = fa.ChildAccounts.Select( ca => ca.Id ).ToList()
                                                             }
                                            ).SingleOrDefault();

            // add the passed-in account to found accounts
            foundAccounts.Add( new ConfiguredFinancialAccount
            {
                Id = financialAccount.Id,
                PublicName = financialAccount.PublicName
            } );

            // now recursively do the same for all children
            foreach ( int childAccountId in financialAccount.ChildAccountIds )
            {
                List<ConfiguredFinancialAccount> nextTierList = GetAccountsRecursive( childAccountId, faQuery );
                foundAccounts.AddRange( nextTierList );
            }

            return foundAccounts;
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            FinancialTransactionService transService = new FinancialTransactionService( rockContext );
            var qry = transService.Queryable( "TransactionDetails.Account,FinancialPaymentDetail" );

            List<int> personAliasIds;

            if ( TargetPerson != null )
            {
                personAliasIds = new PersonAliasService( rockContext ).Queryable().Where( a => a.Person.GivingId == TargetPerson.GivingId ).Select( a => a.Id ).ToList();
            }
            else
            {
                personAliasIds = new List<int>();
            }

            qry = qry.Where( t => t.AuthorizedPersonAliasId.HasValue && personAliasIds.Contains( t.AuthorizedPersonAliasId.Value ) );


            // Are specific accounts configured? (The checkbox list will have items if so)
            if ( cblAccounts.Items.Count > 0 )
            {
                // get list of selected accounts 
                // (which, if the checkbox list is hidden, will be all configured accounts otherwise, it'll be just the ones the user ticked)
                List<int> selectedAccountIds = cblAccounts.Items.Cast<ListItem>()
                                                .Where( i => i.Selected == true )
                                                .Select( i => int.Parse( i.Value ) ).ToList();
                qry = qry.Where( t => t.TransactionDetails.Any( d => selectedAccountIds.Contains( d.AccountId ) ) );
            }

            if ( drpFilterDates.LowerValue.HasValue )
            {
                qry = qry.Where( t => t.TransactionDateTime.Value >= drpFilterDates.LowerValue.Value );
            }

            if ( drpFilterDates.UpperValue.HasValue )
            {
                var lastDate = drpFilterDates.UpperValue.Value.AddDays( 1 ); // add one day to ensure we get all transactions till midnight
                qry = qry.Where( t => t.TransactionDateTime.Value < lastDate );
            }

            // Transaction Types
            var transactionTypeValueIdList = GetAttributeValue( "TransactionTypes" ).SplitDelimitedValues().AsGuidList().Select( a => DefinedValueCache.Read( a ) ).Where( a => a != null ).Select( a => a.Id ).ToList();

            if ( transactionTypeValueIdList.Any() )
            {
                qry = qry.Where( t => transactionTypeValueIdList.Contains( t.TransactionTypeValueId ) );
            }

            qry = qry.OrderByDescending( a => a.TransactionDateTime );

            var txns = qry.ToList();

            // get account totals
            Dictionary<string, decimal> accountTotals = new Dictionary<string, decimal>();

            foreach ( var transaction in txns )
            {
                foreach ( var transactionDetail in transaction.TransactionDetails )
                {
                    if ( accountTotals.Keys.Contains( transactionDetail.Account.Name ) )
                    {
                        accountTotals[transactionDetail.Account.Name] += transactionDetail.Amount;
                    }
                    else
                    {
                        accountTotals.Add( transactionDetail.Account.Name, transactionDetail.Amount );
                    }
                }
            }

            lAccountSummary.Text = string.Empty;
            if ( accountTotals.Count > 0 )
            {
                pnlSummary.Visible = true;
                foreach ( var key in accountTotals.Keys )
                {
                    lAccountSummary.Text += string.Format( "<li>{0}: {2}{1}</li>", key, accountTotals[key], GlobalAttributesCache.Value( "CurrencySymbol" ) );
                }
            }
            else
            {
                pnlSummary.Visible = false;
            }

            gTransactions.EntityTypeId = EntityTypeCache.Read<FinancialTransaction>().Id;
            gTransactions.DataSource = txns.Select( t => new
            {
                t.Id,
                t.TransactionDateTime,
                CurrencyType = FormatCurrencyType( t ),
                t.TransactionCode,
                t.ForeignKey,
                Summary = FormatSummary( t ),
                t.TotalAmount
            } ).ToList();

            gTransactions.ColumnsOfType<Rock.Web.UI.Controls.RockBoundField>().First( c => c.HeaderText == "Transaction Code" ).Visible =
                GetAttributeValue( "ShowTransactionCode" ).AsBoolean();

            gTransactions.ColumnsOfType<Rock.Web.UI.Controls.RockBoundField>().First( c => c.HeaderText == "Foreign Key" ).Visible =
                GetAttributeValue( "ShowForeignKey" ).AsBoolean();

            gTransactions.DataBind();
        }

        /// <summary>
        /// Formats the type of the currency.
        /// </summary>
        /// <param name="txn">The TXN.</param>
        /// <returns></returns>
        protected string FormatCurrencyType( FinancialTransaction txn )
        {
            string currencyType = string.Empty;
            string creditCardType = string.Empty;

            if ( txn.FinancialPaymentDetail != null && txn.FinancialPaymentDetail.CurrencyTypeValueId.HasValue )
            {
                int currencyTypeId = txn.FinancialPaymentDetail.CurrencyTypeValueId.Value;

                var currencyTypeValue = DefinedValueCache.Read( currencyTypeId );
                currencyType = currencyTypeValue != null ? currencyTypeValue.Value : string.Empty;

                if ( txn.FinancialPaymentDetail.CreditCardTypeValueId.HasValue )
                {
                    int creditCardTypeId = txn.FinancialPaymentDetail.CreditCardTypeValueId.Value;
                    var creditCardTypeValue = DefinedValueCache.Read( creditCardTypeId );
                    creditCardType = creditCardTypeValue != null ? creditCardTypeValue.Value : string.Empty;

                    return string.Format( "{0} - {1}", currencyType, creditCardType );
                }
            }

            return currencyType;
        }

        /// <summary>
        /// Formats the summary.
        /// </summary>
        /// <param name="txn">The TXN.</param>
        /// <returns></returns>
        private string FormatSummary( FinancialTransaction txn )
        {
            var sb = new StringBuilder();
            foreach ( var transactionDetail in txn.TransactionDetails )
            {
                sb.AppendFormat( "{0} ({2}{1})<br>", transactionDetail.Account, transactionDetail.Amount, GlobalAttributesCache.Value( "CurrencySymbol" ) );
            }

            return sb.ToString();
        }

        #endregion
    }
}