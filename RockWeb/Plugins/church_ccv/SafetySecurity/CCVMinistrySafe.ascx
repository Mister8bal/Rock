<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVMinistrySafe.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.SafetySecurity.CCVMinistrySafe" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-flag"></i>&nbsp;Ministry Safe Import</h1>
            </div>
            <div class="panel-body">

                <Rock:FileUploader ID="fuImport" runat="server" Label="Import File" OnFileUploaded="LoadCsvFile" />
                <Rock:NotificationBox ID="nbResult" runat="server" Visible="false" />
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>