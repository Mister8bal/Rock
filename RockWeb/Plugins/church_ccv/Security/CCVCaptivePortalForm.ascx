<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVCaptivePortalForm.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Security.CCVCaptivePortalForm" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfMacAddress" runat="server" />
        <asp:HiddenField ID="hfCampusId" runat="server" />
        <Rock:NotificationBox ID="nbAlert" runat="server" NotificationBoxType="Warning" />
        <asp:ValidationSummary ID="valCaptivePortal" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="CaptivePortal" />
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block captive-portal-form" runat="server">

            <h3>

                <asp:Label ID="lblTitleText" runat="server" Text="Thank you for joining us." CssClass="captive-portal-title-text" />

            </h3>
            <div id="welcomePanel">

                <Rock:RockLiteral ID="lWelcomeContent" runat="server" Text="Thanks you for visiting our campus." />

                <div class="login-actions">
                    <div>
                        <div class="btn btn-primary" onclick="startForm();">Next</div>
                    </div>
                </div>
            </div>
            <div id="loginForm" class="hidden">
                <div class="login-form">
                    <div class="person-name">

                        <Rock:RockTextBox ID="tbFirstName" runat="server" Required="true" Label="First Name" ValidationGroup="CaptivePortal" />
                        <Rock:RockTextBox ID="tbLastName" runat="server" Required="true" Label="Last Name" ValidationGroup="CaptivePortal" />

                    </div>
                    <div class="person-contact">

                        <Rock:RockTextBox ID="tbEmail" runat="server" Required="false" Label="Email Address" ValidationGroup="CaptivePortal" />
                        <Rock:PhoneNumberBox ID="tbMobilePhone" runat="server" Required="false" Label="Mobile Number" ValidationGroup="CaptivePortal" />

                    </div>
                </div>
                <div class="login-actions">
                    <div>

                        <asp:LinkButton ID="btnConnect" runat="server" Text="Connect To WiFi" CssClass="btn btn-primary" OnClick="btnConnect_Click" style="width:100%;" ValidationGroup="CaptivePortal" />
                    
                    </div>
                </div>
            </div>
            
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
<script>
    function startForm() {
        $('#welcomePanel').addClass('hidden');
        $('#loginForm').removeClass('hidden');
    }
</script>
