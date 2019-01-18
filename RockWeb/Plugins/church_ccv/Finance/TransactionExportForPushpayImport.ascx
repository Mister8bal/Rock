<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TransactionExportForPushpayImport.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Finance.TransactionExportForPushpayImport" %>
<style>
    .filter-row {
        display: flex;
        justify-content: space-between;
    }

    .filter-item {
        width: 100%;
    }

    .filter-item .form-group {
        width: 80%;
    }

    .action-item {
        width: 300px;
    }

    .btn-export {
        margin-bottom: 20px;
    }
</style>

<asp:Panel ID="pnlTransactionExport" runat="server">

        <h3><strong>Export Options</strong></h3>
        <p><strong>Note:</strong> Exporting a large number of transactions can take a long time, please be patient.</p>
        <div class="filter-row">
            <div class="filter-item">

                <Rock:DateRangePicker ID="drpDateRange" runat="server" Label="Date Range" Required="true" />
        
            </div>
            <div class="filter-item">
        
                <Rock:AccountPicker ID="apAccount" runat="server" AllowMultiSelect="true" Label="Account(s)" Required="true" />
        
            </div>
            <div class="filter-item">
        
                <rock:personpicker id="ppPerson" runat="server" label="Person" includebusinesses="true" />
        
            </div>
        </div>
        <div class="filter-row">
            <div class="filter-item">

                <Rock:DataDropDownList ID="ddlSourceType" runat="server" Label="Source Type" SourceTypeName="Rock.Model.FinancialTransaction, Rock" PropertyName="SourceTypeValueId" />

            </div>
            <div class="filter-item">

                <Rock:DataDropDownList ID="ddlCurrencyType" runat="server" Label="Currency Type" SourceTypeName="Rock.Model.FinancialPaymentDetail, Rock" PropertyName="CurrencyTypeValueId" />

            </div>
            <div class="filter-item"></div>
        </div>
          
        <Rock:RockTextBox ID="rtbFileName" runat="server" Label="File Name" CssClass="action-item" />
    
        <asp:Button runat="server" ID="btnExport" CssClass="btn btn-primary btn-export" Text="Export" OnClick="btnExport_Click" />

        <Rock:NotificationBox runat="server" ID="nbExportMessage" Visible="false" />

</asp:Panel>
