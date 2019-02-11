<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVScheduledTransactionGrid.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Finance.CCVScheduledTransactionGrid" %>
<style>
    .modal.modal-overflow {
        top: 40%;
    }
    .modal.container {
        width: unset;
        margin: -250px;
    }
    .modal-content.rock-modal .modal-footer {
        background-color: #ffffff;
        
    }
    .modal-footer {
        border-top: none;
    }
    .btn-details {
        color: #333;
        font-weight: 400;
        border: 2px solid lightgrey;
        line-height: 19px;
        min-width: 200px;
    }
</style>

<asp:UpdatePanel ID="upPanel" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="mdGridWarning" runat="server"/>

        <div class="transaction-alerts">

            <Rock:NotificationBox ID="nbMessage" runat="server" Visible="false"></Rock:NotificationBox>

        </div>
        
        <div class="grid grid-panel">
            <Rock:Grid ID="gScheduledTransactions" DisplayType="Light" runat="server" AutoGenerateColumns="False" AllowSorting="false" AllowPaging="false" RowItemText="Scheduled Transaction" ClientIDMode="Static" >
                <Columns>
                    <Rock:RockBoundField DataField="GatewayScheduleId" HeaderText="Gateway Schedule Id" />
                    <Rock:RockBoundField DataField="TotalAmount" HeaderText="Amount" DataFormatString="{0:C}"/>
                    <Rock:RockBoundField DataField="CurrencyTypeValue" HeaderText="Payment Type"/>
                    <Rock:RockBoundField DataField="AccountNumberMasked" HeaderText="Payment Account" />
                    <Rock:RockBoundField DataField="TransactionFrequencyValue" HeaderText="Frequency" />                    
                    <Rock:RockBoundField DataField="NextPaymentDate" HeaderText="Next Gift" DataFormatString="{0:MM/d/yyyy}" />
                    <Rock:RockBoundField DataField="StartDate" HeaderText="First Gift" DataFormatString="{0:MM/d/yyyy}" />
                    <Rock:LinkButtonField Text="Transfer Gift" OnClick="gScheduledTransactions_Manage" CssClass="btn btn-details" />   
                    <Rock:DeleteField OnClick="gScheduledTransactions_Delete" />
                </Columns>
            </Rock:Grid>
        </div>

        <asp:Panel ID="pnlManageSchedule" runat="server" Visible="true">
            <Rock:ModalDialog ID="mdManageSchedule" runat="server" SaveButtonText="Transfer Gift">
                <Content>
                    <h3>Attention</h3>

                    <p>You will now be taken to our giving provider, Pushpay, to complete your transaction.</p>

                    <p>For security reasons, you will need to re-enter your payment information.</p>

                    <asp:HiddenField ID="hfGatewayScheduleId" runat="server" />
                </Content>
            </Rock:ModalDialog>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

