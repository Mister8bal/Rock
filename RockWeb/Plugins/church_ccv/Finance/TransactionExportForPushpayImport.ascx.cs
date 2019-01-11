using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Reflection;
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
    [Description( "Utility to export financial transactions to a CSV file that can be used to import to Pushpay" )]

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
            }


            // TODO: Add back in mailing addresses and fix any slowness that comes with them
            //       Add in somekind of feedback Message stating how many records were exported.
        }

        protected void btnExport_Click( object sender, EventArgs e )
        {
            // cetup rock context with increased timeout
            RockContext rockContext = new RockContext();
            rockContext.Database.CommandTimeout = 240;

            // tables for join
            var personAliasTable = new PersonAliasService( rockContext ).Queryable().AsNoTracking();
            var datamartPersonTable = new DatamartPersonService( rockContext ).Queryable().AsNoTracking();

            // date range filter - If no date range, default to past 12 months
            DateTime startDate = drpDateRange.LowerValue.HasValue ? ( DateTime ) drpDateRange.LowerValue : DateTime.Now.AddMonths( -4 );
            DateTime endDate = drpDateRange.UpperValue.HasValue ? ( DateTime ) drpDateRange.UpperValue : DateTime.MaxValue;

            // get the financial transactions filtered by fund, source type, and date
            // dont include Pushpay Source ID 11308
            var qry = new FinancialTransactionDetailService( rockContext ).Queryable().AsNoTracking()
                    .Where( a => a.Transaction.TransactionDateTime.HasValue )
                    .Where( a => a.Transaction.TransactionDateTime.Value >= startDate )
                    .Where( a => a.Transaction.TransactionDateTime.Value <= endDate )
                    .Where( a => a.Transaction.SourceTypeValueId != 11308 );

            // filter by accounts
            if ( apAccount.SelectedValues.FirstOrDefault() != "0" )
            {
                qry = qry.Where( a => apAccount.SelectedValues.Contains( a.Account.Id.ToString() ) );
            }

            // filter by person
            if ( ppPerson.PersonId.HasValue )
            {
                var person = new PersonService( rockContext ).Get( ppPerson.PersonId.Value );

                qry = qry.Where( a => a.Transaction.AuthorizedPersonAliasId == person.PrimaryAliasId );
            }

            // filter currency type
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
                join aliasPerson in personAliasTable on transaction.Transaction.AuthorizedPersonAliasId equals aliasPerson.Id into aliases
                from alias in aliases.DefaultIfEmpty()
                join datamartPerson in datamartPersonTable on alias.PersonId equals datamartPerson.PersonId into dpPeople
                from people in dpPeople.DefaultIfEmpty()
                select new
                {
                    TransactionId = transaction.Id,
                    Date = SqlFunctions.StringConvert((double)transaction.Transaction.TransactionDateTime.Value.Month).Trim() + "/" + SqlFunctions.StringConvert((double)transaction.Transaction.TransactionDateTime.Value.Day).Trim() + "/" + SqlFunctions.StringConvert((double)transaction.Transaction.TransactionDateTime.Value.Year).Trim(),
                    Time = SqlFunctions.StringConvert((double)transaction.Transaction.TransactionDateTime.Value.Hour).Trim() + ":" + SqlFunctions.StringConvert((double)transaction.Transaction.TransactionDateTime.Value.Minute).Trim(),
                    Amount = transaction.Amount.ToString(),
                    Method = transaction.Transaction.FinancialPaymentDetail.CurrencyTypeValue.Value,
                    Source = transaction.Transaction.SourceTypeValue.Value,
                    FundCode = transaction.Account.Id.ToString(),
                    FundName = transaction.Account.Name,
                    PersonAliasId = transaction.Transaction.AuthorizedPersonAliasId,
                    MobileNumber = people.CellPhone,
                    Person = alias.Person
                };

            // setup transactions object
            List<Transaction> transactions = new List<Transaction>();

            // create a transaction for each item in export query and add to transactions object
            foreach (var item in exportQry )
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

                transaction.YourID = item.PersonAliasId.ToString();
                transaction.PersonID = item.PersonAliasId.ToString();

                if (item.Person.FirstName.IsNotNullOrWhitespace())
                {
                    transaction.FirstName = item.Person.FirstName;
                }
                else
                {
                    transaction.FirstName = "Business";
                }
                transaction.LastName = item.Person.LastName;
                transaction.Email = item.Person.Email;

                if (item.MobileNumber.IsNotNullOrWhitespace())
                {
                    if (item.MobileNumber.Length == 14)
                    {
                        transaction.MobileNumber = item.MobileNumber;

                    }
                }


                //Location mailingAddress = item.Person.GetMailingLocation();

                //if (mailingAddress != null)
                //{
                //    transaction.AddressOne = mailingAddress.Street1;
                //    transaction.AddressTwo = mailingAddress.Street2;
                //    transaction.City = mailingAddress.City;
                //    transaction.State = mailingAddress.State;
                //    transaction.Zip = mailingAddress.PostalCode;
                //    transaction.Country = mailingAddress.Country;
                //}


                transaction.DOB = item.Person.BirthDate.ToString();
                transaction.Gender = item.Person.Gender.ToString();

                transactions.Add(transaction);
            }

            // testing
            ltlMessage.Text = transactions.Count.ToString();
            
            // Convert transactions List to DataTable
            DataTable dt = new DataTable();
            ListToDataTableConverter converter = new ListToDataTableConverter();
            dt = converter.ToDataTable(transactions);

            // Setup file name and export datatable to an excel file
            ExportToExcel(dt, rtbFileName.Text );
        }

        private bool ExportToExcel( DataTable dt, string fileName )
        {
            ExcelPackage excel = new ExcelPackage();

            excel.Workbook.Properties.Title = "Export";

            ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add( "Export" );
            var totalCols = dt.Columns.Count;
            var totalRows = dt.Rows.Count;

            for ( var col = 1; col <= totalCols; col++ )
            {
                worksheet.Cells[1, col].Value = dt.Columns[col - 1].ColumnName;
            }

            for ( var row = 1; row <= totalRows; row++ )
            {
                for ( var col = 0; col < totalCols; col++ )
                {
                    worksheet.Cells[row + 1, col + 1].Value = dt.Rows[row - 1][col];
                }
            }

            using ( var memoryStream = new MemoryStream() )
            {
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader( "content-disposition", string.Format( "attachment; filename={0}.xlsx", fileName.IsNotNullOrWhitespace() ? fileName : "export" ) );

                try
                {
                    excel.SaveAs( memoryStream );
                    memoryStream.WriteTo( Response.OutputStream );
                    Response.Flush();
                    Response.End();

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
                //Get all the properties  
                PropertyInfo[] Props = typeof( T ).GetProperties( BindingFlags.Public | BindingFlags.Instance );
                // Loop through all the properties  
                foreach ( PropertyInfo prop in Props )
                {
                    //Setting column names as Property names  
                    dataTable.Columns.Add( prop.Name );
                }

                foreach ( T item in items )
                {
                    var values = new object[Props.Length];
                    for ( int i = 0; i < Props.Length; i++ )
                    {
                        //inserting property values to datatable rows  
                        values[i] = Props[i].GetValue( item, null );
                    }
                    // Finally add value to datatable  
                    dataTable.Rows.Add( values );

                }
                //put a breakpoint here and check datatable of return values  
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