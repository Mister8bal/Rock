<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctHeader" ContentPlaceHolderID="header" runat="server">
   <!-- Page Header -->
   <header class="masthead" runat="server" id="masthead">
        
        <Rock:Zone Name="AnnouncementBar" runat="server" />
    
        <div class="masthead-upper">
            <div class="masthead-brand">
                <button class="navbar-toggle" type="button" onClick="toggleNavbar(true);">
                    <i class="fa fa-bars"></i>
                </button>
                <Rock:Zone Name="Header" runat="server" />
            </div>

            <nav class="masthead-nav">
                <div class="navbar-collapse js-all-collapse collapse">
                    <Rock:Zone Name="Navigation" runat="server" />
                </div>
            </nav>
        </div>
        <div class="masthead-fullmenu offscreen">
            <Rock:Zone Name="Full Menu" runat="server" />
        </div>
</header>
</asp:Content>

<asp:Content ID="ctFeature" ContentPlaceHolderID="feature" runat="server">


    <section class="main">
        <div>
            <div class="col-md-12">
                <Rock:Zone Name="Section A" runat="server" />
            </div>
        </div>
    </section>  

    <div class="container">
        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>
    </div>
  
    <section class="main-feature">
        <div class="container">
            <h2 class="margin-v-lg">Sports By Location</h2>

            <div class="row">
                <div class="col-md-6">
                    <div class="module module-with-footer">
                        <div class="module-body">
                            <h3 class="module-title margin-b-lg">Peoria Campus</h3>
                            <Rock:Zone Name="PeoriaSports" runat="server" />
                        </div>
                        <div class="module-mid js-fieldstatus fieldstatus clearfix">
                            <strong class="pull-left margin-r-sm">Field Status:</strong>
                            <div class="pull-left">
                                <Rock:Zone Name="PeoriaStatus" runat="server" />
                            </div>
                        </div>
                        <div class="module-footer">
                            <Rock:Zone Name="PeoriaContact" runat="server" />
                        </div>
                    </div>
                    <div class="module module-with-footer">
                        <div class="module-body">
                            <h3 class="module-title margin-b-lg">Avondale Campus</h3>
                            <Rock:Zone Name="AvondaleSports" runat="server" />
                        </div>
                        <div class="module-mid js-fieldstatus fieldstatus clearfix">
                            <strong class="pull-left margin-r-sm">Field Status:</strong>
                            <div class="pull-left">
                                <Rock:Zone Name="AvondaleStatus" runat="server" />
                            </div>
                        </div>
                        <div class="module-footer">
                            <Rock:Zone Name="AvondaleContact" runat="server" />
                        </div>
                    </div>
                    <div class="module module-with-footer">
                        <div class="module-body">
                            <h3 class="module-title margin-b-lg">North Phoenix Campus</h3>
                            <Rock:Zone Name="NorthPhoenixSports" runat="server" />
                        </div>
                        <div class="module-mid js-fieldstatus fieldstatus clearfix">
                            <strong class="pull-left margin-r-sm">Field Status:</strong>
                            <div class="pull-left">
                                <Rock:Zone Name="NorthPhoenixStatus" runat="server" />
                            </div>
                        </div>
                        <div class="module-footer">
                            <Rock:Zone Name="NorthPhoenixContact" runat="server" />
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="module module-with-footer">
                        <div class="module-body">
                            <h3 class="module-title margin-b-lg">Surprise Campus</h3>
                            <Rock:Zone Name="SurpriseSports" runat="server" />
                        </div>
                        <div class="module-mid js-fieldstatus fieldstatus clearfix">
                            <strong class="pull-left margin-r-sm">Field Status:</strong>
                            <div class="pull-left">
                                <Rock:Zone Name="SurpriseStatus" runat="server" />
                            </div>
                        </div>
                        <div class="module-footer">
                            <Rock:Zone Name="SurpriseContact" runat="server" />
                        </div>
                    </div>

                    <div class="module module-with-footer">
                        <div class="module-body">
                            <h3 class="module-title margin-b-lg">East Valley Campus</h3>
                            <Rock:Zone Name="EastValleySports" runat="server" />
                        </div>
                        <div class="module-mid js-fieldstatus fieldstatus clearfix">
                            <strong class="pull-left margin-r-sm">Field Status:</strong>
                            <div class="pull-left">
                                <Rock:Zone Name="EastValleyStatus" runat="server" />
                            </div>
                        </div>
                        <div class="module-footer">
                            <Rock:Zone Name="EastValleyContact" runat="server" />
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="module module-with-footer">
                        <div class="module-body">
                            <h3 class="module-title margin-b-lg">Chandler Campus</h3>
                            <Rock:Zone Name="ChandlerSports" runat="server" />
                        </div>
                        <div class="module-mid js-fieldstatus fieldstatus clearfix">
                            <strong class="pull-left margin-r-sm">Field Status:</strong>
                            <div class="pull-left">
                                <Rock:Zone Name="ChandlerStatus" runat="server" />
                            </div>
                        </div>
                        <div class="module-footer">
                            <Rock:Zone Name="ChandlerContact" runat="server" />
                        </div>
                    </div>
                    <div class="module module-with-footer">
                        <div class="module-body">
                            <h3 class="module-title margin-b-lg">Verrado Campus</h3>
                            <Rock:Zone Name="VerradoSports" runat="server" />
                        </div>
                        <div class="module-mid js-fieldstatus fieldstatus clearfix">
                            <strong class="pull-left margin-r-sm">Field Status:</strong>
                            <div class="pull-left">
                                <Rock:Zone Name="VerradoStatus" runat="server" />
                            </div>
                        </div>
                        <div class="module-footer">
                            <Rock:Zone Name="VerradoContact" runat="server" />
                        </div>
                    </div>
                </div>
            </div>
            <Rock:Zone Name="Feature" runat="server" />
        </div>
    </section>

</asp:Content>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

    <section class="sub-feature">
        <div class="container">
            <Rock:Zone Name="Sub Feature" runat="server" />
        </div>
    </section>

	<main class="container">

        <!-- Start Content Area -->

        <div class="row">
            <div id="sectionb-bg" class="col-md-4">
                <Rock:Zone Name="Section B" runat="server" />
            </div>
            <div id="sectionc-bg" class="col-md-4">
                <Rock:Zone Name="Section C" runat="server" />
            </div>
            <div id="sectiond-bg" class="col-md-4">
                <Rock:Zone Name="Section D" runat="server" />
            </div>
        </div>

        <!-- End Content Area -->

	</main>

</asp:Content>

<asp:Content ID="ctFooter" ContentPlaceHolderID="footer" runat="server">
    <footer class="mainfooter mainfooter-dark">
        <div class="container">
            <Rock:Zone Name="Footer" runat="server" />
        </div>
    </footer>
</asp:Content>

<asp:Content ID="ctScripts" ContentPlaceHolderID="scripts" runat="server">
   <script>
       $(document).ready(function(){
           $('.js-fieldstatus').each(function(){
                var $this = $(this)
                if ($this.text().toLowerCase().indexOf('some') !== -1) {
                    $this.addClass('alert-warning')
                    return
                }
                if ($this.text().toLowerCase().indexOf('open') !== -1) {
                    $this.addClass('alert-success')
                    return
                }
                if ($this.text().toLowerCase().indexOf('close') !== -1) {
                    $this.addClass('alert-danger')
                    return
                }
           })
       })
   </script>
</asp:Content>
