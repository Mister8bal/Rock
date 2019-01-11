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
        width: 400px;
    }

    .btn-export {

    }
</style>

<asp:Panel ID="pnlTransactionExport" runat="server">

    <asp:Literal runat="server" ID="ltlMessage"></asp:Literal>

    <h3>Filter</h3>
    <div class="filter-row">
        <div class="filter-item">

            <Rock:DateRangePicker ID="drpDateRange" runat="server" Label="Date Range" ToolTip="If date range is not specified, query will default to past 12 months" />
        
        </div>
        <div class="filter-item">
        
            <Rock:AccountPicker ID="apAccount" runat="server" AllowMultiSelect="true" Label="Account(s)" />
        
        </div>
        <div class="filter-item">
        
            <rock:personpicker id="ppPerson" runat="server" label="person" includebusinesses="true" />
        
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
    

</asp:Panel>


