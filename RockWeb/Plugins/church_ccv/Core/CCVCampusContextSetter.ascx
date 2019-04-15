<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVCampusContextSetter.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Core.CCVCampusContextSetter" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Literal ID="lOutput" runat="server" />

        <asp:Literal ID="lDebug" runat="server" Visible="false" />

    </ContentTemplate>
</asp:UpdatePanel>
