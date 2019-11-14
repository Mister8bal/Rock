<%@ Control Language="C#" AutoEventWireup="true" CodeFile="InviteList.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Groups.InviteList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="row">
            <div class="col-md-12">
                <Rock:NotificationBox ID="nbNotifications" runat="server" NotificationBoxType="Warning" Visible="false" />
            </div>
        </div>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <Rock:ModalDialog ID="mdConfirmInvitation" runat="server" Title="Preview Message" SaveButtonText="Send" OnSaveClick="btnSendEmail_Click">
                <Content>
                    <Rock:NotificationBox ID="nbRecepientCount" runat="server" NotificationBoxType="Info" />
                    <Rock:RockTextBox ID="tbEmailEditor" runat="server" TextMode="MultiLine" Rows="16" Wrap="true" />
                </Content>
             </Rock:ModalDialog>

            <asp:Literal ID="lTemplate" runat="server" />

            <div class="action-row" style="padding: 15px 20px 0 20px">
                <asp:LinkButton ID="btnOpenEditor" runat="server" Text="Send Invitations" CssClass="btn btn-primary" OnClick="btnOpenEditor_Click"  />
                <hr style="margin: 15px -20px 0 -20px;" />
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" AllowSorting="true" AllowPaging="false" >
                        <Columns>
                            <Rock:SelectField ></Rock:SelectField>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <Rock:RockBoundField DataField="Gender" HeaderText="Gender" SortExpression="Gender" />
                            <Rock:RockBoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                            <Rock:RockBoundField DataField="Location" HeaderText="Address" SortExpression="Location" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
