<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TalentLMSUser.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Hr.TalentLMSUser" %>

<link rel="stylesheet" href="/Plugins/church_ccv/Hr/Styles/talent-lms.css">

<asp:Panel runat="server" ID="pnlTalentLMS">

    <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-warning" />

    <asp:Panel runat="server" ID="pnlTalentLMSDashboard">

        <div id="userCourses">

            <asp:Panel runat="server" ID="pnlUserCourses"></asp:Panel>

        </div>
        <div id="allCourses">

            <asp:Panel runat="server" ID="pnlAllCourses"></asp:Panel>

        </div>

    </asp:Panel>



</asp:Panel>
