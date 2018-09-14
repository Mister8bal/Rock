<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TalentLMSUser.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Hr.TalentLMSUser" %>

<link rel="stylesheet" href="/Plugins/church_ccv/Hr/Styles/talent-lms.css">

<asp:Panel runat="server" ID="pnlTalentLMS">

    <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-warning" />

    <asp:Panel runat="server" ID="pnlTalentLMSDashboard">

        <div id="userCourses">

            <asp:Panel runat="server" ID="pnlUserCourses"></asp:Panel>

        </div>
        <div id="allCourses">

            <Rock:Grid runat="server" ID="gGrid">
                <Columns>
                    <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                    <Rock:RockBoundField DataField="Action" HeaderText="" />
                </Columns>
            </Rock:Grid>

        </div>

    </asp:Panel>



</asp:Panel>
