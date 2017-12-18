﻿<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

    <div class="container">
        <!-- Start Content Area -->

        <!-- Page Title -->
        <Rock:PageIcon ID="PageIcon" runat="server" /> <h1><Rock:PageTitle ID="PageTitle" runat="server" /></h1>
        <Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />

        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>
    </div>

    <Rock:Zone Name="Feature" runat="server" />

    <main class="container">

        <div class="row">
            <div class="col-md-3 col-md-push-9">
                <Rock:Zone Name="Sidebar 1" runat="server" />
            </div>
            <div class="col-md-9 col-md-pull-3">
                <Rock:Zone Name="Main" runat="server" />
            </div>
        </div>

    </main>

    <Rock:Zone Name="Section A" runat="server" />

    <div class="container">

        <div class="row">
            <div class="col-md-4">
                <Rock:Zone Name="Section B" runat="server" />
            </div>
            <div class="col-md-4">
                <Rock:Zone Name="Section C" runat="server" />
            </div>
            <div class="col-md-4">
                <Rock:Zone Name="Section D" runat="server" />
            </div>
        </div>

        <!-- End Content Area -->

    </div>

</asp:Content>