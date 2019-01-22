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
			<Rock:Zone Name="Section A" runat="server" />
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
		<a name="stars-locations"></a>
		<h2 class="main-feature-title">Sports By Location</h2>

		<div class="row main-feature-locations">
			<div class="col-md-6">
			
				<div class="module module-with-footer">
					<div class="module-body">
						<h3 class="module-title margin-b-lg">Peoria</h3>
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
						<h3 class="module-title margin-b-lg">Avondale</h3>
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
						<h3 class="module-title margin-b-lg">North Phoenix</h3>
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
						<h3 class="module-title margin-b-lg">Surprise</h3>
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
						<h3 class="module-title margin-b-lg">East Valley</h3>
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
				
				<div class="module module-with-footer">
					<div class="module-body">
						<h3 class="module-title margin-b-lg">Chandler</h3>
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
						<h3 class="module-title margin-b-lg">Verrado</h3>
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
    </section>

</asp:Content>

<asp:Content ID="ctFooter" ContentPlaceHolderID="footer" runat="server">
    <footer class="mainfooter">
		<Rock:Zone Name="Footer" runat="server" />
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
