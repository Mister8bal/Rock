using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using church.ccv.Datamart.Model;
using OfficeOpenXml;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.church_ccv.Finance
{
    #region Block Attributes

    /// <summary>
    /// Grid of Scheduled Transactions for current user 
    /// with option to delete
    /// </summary>
    [DisplayName( "Transaction Export For Pushpay Import" )]
    [Category( "CCV > Finance" )]
    [Description( "Utility to export financial transactions to an Excel file that can be used to import to Pushpay" )]

    #endregion

    public partial class TransactionExportForPushpayImport : RockBlock
    {
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
                // bind CurrencyType dropdown
                ddlCurrencyType.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE ) ), true );

                // bind SourceType dropdown, dont include Pushpay source
                ddlSourceType.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE )), true );
                ddlSourceType.Items.Remove( ddlSourceType.Items.FindByText( "Pushpay" ) );

                // ensure message box is hidden
                nbExportMessage.Visible = false;
            }
        }
        
        /// <summary>
        /// Click event for Export button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnExport_Click( object sender, EventArgs e )
        {
            // setup rock context with increased timeout
            RockContext rockContext = new RockContext();
            rockContext.Database.CommandTimeout = 240;

            // tables for join
            var personAliasTable = new PersonAliasService( rockContext ).Queryable().AsNoTracking();
            var datamartPersonTable = new DatamartPersonService( rockContext ).Queryable().AsNoTracking();
            var groupTable = new GroupService( rockContext ).Queryable().AsNoTracking();
            var groupLocationTable = new GroupLocationService( rockContext ).Queryable().AsNoTracking();
            var locationTable = new LocationService( rockContext ).Queryable().AsNoTracking();

            // date range filter 
            // date range is required input, but just in case default to past 1 day
            DateTime startDate = drpDateRange.LowerValue.HasValue ? ( DateTime ) drpDateRange.LowerValue : DateTime.Now.AddDays( -1 );
            DateTime endDate = drpDateRange.UpperValue.HasValue ? ( DateTime ) drpDateRange.UpperValue : DateTime.MaxValue;

            // get the financial transactions filtered by fund, source type, and date
            // dont include Pushpay - SourceTypeValueId: 11308
            var qry = new FinancialTransactionDetailService( rockContext ).Queryable().AsNoTracking()
                    .Where( a => apAccount.SelectedValues.Contains( a.AccountId.ToString() ) )
                    .Where( a => a.Transaction.TransactionDateTime.HasValue )
                    .Where( a => a.Transaction.TransactionDateTime.Value >= startDate )
                    .Where( a => a.Transaction.TransactionDateTime.Value <= endDate )
                    .Where( a => a.Transaction.SourceTypeValueId != 11308 );

            // filter Anonymous users
            // Anonymous Anonymous: 351648
            // Anonymous Giver: 197390
            // Name Unlocated: 114093
            qry = qry.Where( a => a.Transaction.AuthorizedPersonAlias.PersonId != 351648 && 
                                  a.Transaction.AuthorizedPersonAlias.PersonId != 197390 &&
                                  a.Transaction.AuthorizedPersonAlias.PersonId != 114093 );

            // filter by person
            if ( ppPerson.PersonId.HasValue )
            {
                var person = new PersonService( rockContext ).Get( ppPerson.PersonId.Value );

                qry = qry.Where( a => a.Transaction.AuthorizedPersonAliasId == person.PrimaryAliasId );
            }

            // filter by currency type
            int currencyTypeId = int.MinValue;
            if ( int.TryParse( ddlCurrencyType.SelectedValue, out currencyTypeId ) )
            {
                qry = qry.Where( a => a.Transaction.FinancialPaymentDetail.CurrencyTypeValueId == currencyTypeId );
            }

            // filter by source type
            int sourceTypeId = int.MinValue;
            if ( int.TryParse( ddlSourceType.SelectedValue, out sourceTypeId ) )
            {
                qry = qry.Where( a => a.Transaction.SourceTypeValueId == sourceTypeId );
            }

            // prep for export
            var exportQry =
                from transaction in qry
                // get person from person alias
                join aliasPerson in personAliasTable on transaction.Transaction.AuthorizedPersonAliasId equals aliasPerson.Id into aliases
                from alias in aliases.DefaultIfEmpty()
                join datamartPerson in datamartPersonTable on alias.PersonId equals datamartPerson.PersonId into dpPeople
                from people in dpPeople.DefaultIfEmpty()
                // get mailing address - Exclude Previous Address locations Type 137
                join groupLocation in groupLocationTable on people.FamilyId equals groupLocation.GroupId into groupLocations
                from locations in groupLocations.Where(l => l.IsMailingLocation == true && l.GroupLocationTypeValueId != 137 ).DefaultIfEmpty()
                join location in locationTable on locations.LocationId equals location.Id into familyLocations
                from familyLocation in familyLocations.DefaultIfEmpty()
                select new
                {
                    TransactionId = transaction.Id,
                    Date = SqlFunctions.StringConvert( ( double ) transaction.Transaction.TransactionDateTime.Value.Month ).Trim() + "/" + SqlFunctions.StringConvert( ( double ) transaction.Transaction.TransactionDateTime.Value.Day ).Trim() + "/" + SqlFunctions.StringConvert( ( double ) transaction.Transaction.TransactionDateTime.Value.Year ).Trim(),
                    Time = SqlFunctions.StringConvert( ( double ) transaction.Transaction.TransactionDateTime.Value.Hour ).Trim() + ":" + SqlFunctions.StringConvert( ( double ) transaction.Transaction.TransactionDateTime.Value.Minute ).Trim(),
                    Amount = transaction.Amount.ToString(),
                    Method = transaction.Transaction.FinancialPaymentDetail.CurrencyTypeValue.Value,
                    Source = transaction.Transaction.SourceTypeValue.Value,
                    FundCode = transaction.Account.Id.ToString(),
                    FundName = transaction.Account.Name,
                    GLCode = transaction.Account.GlCode,
                    Memo = transaction.Transaction.Summary,
                    MobileNumber = people.CellPhone,
                    Person = alias.Person,
                    Street1 = familyLocation.Street1,
                    Street2 = familyLocation.Street2,
                    City = familyLocation.City,
                    State = familyLocation.State,
                    PostalCode = familyLocation.PostalCode,
                    Country = familyLocation.Country
                };

            // setup transactions list object
            List<Transaction> transactions = new List<Transaction>();

            // create a transaction object for each item in export query and add to transactions list
            foreach (var item in exportQry )
            {
                // check if transaction already exists in transactions list and skip if it does
                if ( !transactions.Exists( a => a.PaymentID == item.TransactionId.ToString() ) )
                {
                    Transaction transaction = new Transaction();

                    transaction.PaymentID = item.TransactionId.ToString();
                    transaction.Date = item.Date;
                    transaction.Time = item.Time;
                    transaction.Amount = item.Amount;

                    transaction.Method = item.Method;

                    transaction.Source = item.Source;
                    transaction.FundCode = item.FundCode;
                    transaction.FundName = item.FundName;

                    // if mission fund, assign trip name and person name to memo field
                    if ( item.GLCode.IsNotNullOrWhitespace() && item.GLCode.StartsWith( "8" ) )
                    {
                        //Note: Im sure i could combine the next few lines into one regex, but quicker for me not to
                        // Remove everything between () 
                        var missionName = Regex.Replace( item.FundName, "\\([^)]*\\)", "" );
                        // Remove everything between []
                        missionName = Regex.Replace( missionName, "\\[[^)]*\\]", "" );
                        // Remove all non alpha characters
                        missionName = Regex.Replace( missionName, "[^a-zA-Z ]", "" );

                        // check for person name in memo field and build name string if found
                        var nameString = "";
                        if ( item.Memo.IsNotNullOrWhitespace() && item.Memo.Contains( "CCV Online Contribution" ) )
                        {
                            // remove everything from memo that is before the : - effectively leaving the person name
                            nameString = " - " + Regex.Replace( item.Memo, "^[^:]*:", "" ).Trim();
                        }

                        // build the memo string
                        transaction.Memo = missionName.Trim() + nameString;
                    }

                    transaction.YourID = item.Person.PrimaryAliasId.ToString();
                    transaction.PersonID = item.Person.PrimaryAliasId.ToString();

                    // making assumption if first name is blank its a business
                    if ( item.Person.FirstName.IsNotNullOrWhitespace() )
                    {
                        transaction.FirstName = item.Person.FirstName;
                    }
                    else
                    {
                        transaction.FirstName = "Business";
                    }
                    transaction.LastName = item.Person.LastName;
                    transaction.Email = item.Person.Email;

                    if ( item.MobileNumber.IsNotNullOrWhitespace() )
                    {
                        if ( item.MobileNumber.Length == 14 )
                        {
                            transaction.MobileNumber = item.MobileNumber;

                        }
                    }

                    transaction.AddressOne = item.Street1;
                    transaction.AddressTwo = item.Street2;
                    transaction.City = item.City;
                    transaction.State = item.State;
                    transaction.Zip = item.PostalCode;
                    transaction.Country = item.Country;
                    transaction.DOB = item.Person.BirthDate.ToString();
                    transaction.Gender = item.Person.Gender.ToString();

                    transactions.Add( transaction );
                }
            }

            // convert transactions List to dataTable
            DataTable dt = new DataTable();
            ListToDataTableConverter converter = new ListToDataTableConverter();
            dt = converter.ToDataTable(transactions);

            // export datatable to an excel file
            if ( !ExportToExcel( dt, rtbFileName.Text ) )
            {
                nbExportMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Danger;
                nbExportMessage.Text = "Export failed";
                nbExportMessage.Visible = true;
            }
        }

        /// <summary>
        /// Export a dataTable to excel
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool ExportToExcel( DataTable dataTable, string fileName )
        {
            // initiate excel instance
            ExcelPackage excel = new ExcelPackage();

            excel.Workbook.Properties.Title = "Export";

            // add worksheet
            ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add( "Export" );

            var totalColumns = dataTable.Columns.Count;
            var totalRows = dataTable.Rows.Count;

            // poulate the columns
            for ( var col = 1; col <= totalColumns; col++ )
            {
                worksheet.Cells[1, col].Value = dataTable.Columns[col - 1].ColumnName;
            }

            // populate the rows
            for ( var row = 1; row <= totalRows; row++ )
            {
                for ( var col = 0; col < totalColumns; col++ )
                {
                    worksheet.Cells[row + 1, col + 1].Value = dataTable.Rows[row - 1][col];
                }
            }

            // send the file to the browser
            using ( var memoryStream = new MemoryStream() )
            {
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader( "content-disposition", string.Format( "attachment; filename={0}.xlsx", fileName.IsNotNullOrWhitespace() ? fileName : "export" ) );

                try
                {
                    excel.SaveAs( memoryStream );
                    memoryStream.WriteTo( Response.OutputStream );

                    // close the stream
                    Response.Flush();
                    Response.SuppressContent = true;
                    ApplicationInstance.CompleteRequest();

                    return true;
                }
                catch ( Exception )
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// List to DataTable Converter
        /// </summary>
        public class ListToDataTableConverter
        {
            public DataTable ToDataTable<T>( List<T> items )
            {
                DataTable dataTable = new DataTable( typeof( T ).Name );
                
                // get all the properties  
                PropertyInfo[] Props = typeof( T ).GetProperties( BindingFlags.Public | BindingFlags.Instance );

                // set column names as property names  
                foreach ( PropertyInfo prop in Props )
                {
                    dataTable.Columns.Add( prop.Name );
                }

                // populate values to datatable
                foreach ( T item in items )
                {
                    var values = new object[Props.Length];
                    for ( int i = 0; i < Props.Length; i++ )
                    {
                        values[i] = Props[i].GetValue( item, null );
                    }

                    dataTable.Rows.Add( values );
                }

                return dataTable;
            }
        }

        /// <summary>
        /// Binds the defined type dropdown.
        /// </summary>
        /// <param name="ListControl">The list control.</param>
        /// <param name="definedTypeGuid">The defined type GUID.</param>
        /// <param name="userPreferenceKey">The user preference key.</param>
        private void BindDefinedTypeDropdown( ListControl listControl, Guid definedTypeGuid, string userPreferenceKey )
        {
            listControl.BindToDefinedType( DefinedTypeCache.Read( definedTypeGuid ) );
            listControl.Items.Insert( 0, new ListItem( string.Empty, string.Empty ) );
        }

        protected class Transaction
        {
            public string PaymentID { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public string Amount { get; set; }
            public string RoutingNumber { get; set; }
            public string AccountNumber { get; set; }
            public string CheckNumber { get; set; }
            public string Method { get; set; }
            public string Source { get; set; }
            public string FundCode { get; set; }
            public string FundName { get; set; }
            public string Memo { get; set; }
            public string YourID { get; set; }
            public string PersonID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string MobileNumber { get; set; }
            public string AddressOne { get; set; }
            public string AddressTwo { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Country { get; set; }
            public string DOB { get; set; }
            public string Gender { get; set; }

            public Transaction()
            {

            }
        }
    }
}