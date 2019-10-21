<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

	<main>
        <!-- Start Content Area -->

        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>

        <div id="section-a-bg">

            <Rock:Zone Name="Section A" runat="server" />
            
        </div>

        <div id="section-b-bg">

            <Rock:Zone Name="Section B" runat="server" />

        </div>
        <!-- End Content Area -->

        </div>
	</main>

</asp:Content>

