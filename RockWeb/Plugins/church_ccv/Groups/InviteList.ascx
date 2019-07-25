﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="InviteList.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Groups.InviteList" %>

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
                    <Rock:RockTextBox ID="tbEmailEditor" runat="server" TextMode="MultiLine" Rows="16" Wrap="true" />
                </Content>
             </Rock:ModalDialog>

            <asp:Literal ID="lTemplate" runat="server" />

            <div class="action-row margin-t-md">
<%--                <Rock:BootstrapButton ID="btnOpenEditor" runat="server" Text="Send Invitations" CssClass="btn btn-primary margin-h-md" OnClick="btnOpenEditor_Click"  />--%>
                <asp:LinkButton ID="btnOpenEditor" runat="server" Text="Send Invitations" CssClass="btn btn-primary margin-h-md" OnClick="btnOpenEditor_Click"  />
                <hr />
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" AllowSorting="true">
                        <Columns>
                            <Rock:RockBoundField DataField="FullName" HeaderText="Name" SortExpression="FullName" />
                            <Rock:RockBoundField DataField="Gender" HeaderText="Gender" SortExpression="Gender" />
                            <Rock:RockBoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>