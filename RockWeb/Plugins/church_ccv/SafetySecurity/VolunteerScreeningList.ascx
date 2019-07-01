<%@ Control Language="C#" AutoEventWireup="true" CodeFile="VolunteerScreeningList.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.SafetySecurity.VolunteerScreeningList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pVolunteerScreeningList" runat="server" class="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Volunteer Screening</h4>
                    <br />
                </div>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel vs-list">
                    <Rock:Grid ID="gGrid" runat="server" OnRowSelected="gGrid_Edit">
                        <Columns>
                            <Rock:RockBoundField DataField="State" HeaderText="Status" SortExpression="State" />
                             <Rock:RockBoundField DataField="InitiatedBy" HeaderText="Initiated By" SortExpression="InitiatedBy" />
                             <Rock:RockBoundField DataField="ApplicationType" HeaderText="Application Type" SortExpression="ApplicationType" />
                            <Rock:RockBoundField DataField="SentDate" HeaderText="Application Sent" SortExpression="SentDate" />
                            <Rock:RockBoundField DataField="CompletedDate" HeaderText="Application Completed" SortExpression="CompletedDate" />
                            <Rock:DeleteField OnClick="gGrid_Delete" />
                        </Columns>
                    </Rock:Grid>

                    <asp:HyperLink ID="hlSendApplication" runat="server" Text="Send Application" CssClass="btn btn-default app-btn" />
                    
                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
