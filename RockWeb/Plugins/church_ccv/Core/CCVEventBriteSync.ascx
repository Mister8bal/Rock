<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVEventBriteSync.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Core.CCVEventBriteSync" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-cloud-upload"></i>&nbsp;Event Brite Attendee Import</h1>
            </div>
            <div class="panel-body">

                <Rock:DataTextBox ID="tbGroupId" runat="server" SourceTypeName="Rock.Model.Group, Rock" PropertyName="Id" />
                <%-- Start Panel --%>
                <asp:Panel ID="pnlStart" runat="server">
                    <Rock:FileUploader ID="fuImport" runat="server" Label="Import File" OnFileUploaded="fuImport_FileUploaded" UploadAsTemporary="true" />
                </asp:Panel>

                <%-- Done Panel --%>
                <asp:Panel ID="pnlDone" runat="server" Visible="false">
                    <Rock:NotificationBox ID="nbSuccess" runat="server" CssClass="alert alert-success" Title="" Text="Import successfully completed!" />
                </asp:Panel>
                <%-- Error Panel --%>
                <asp:Panel ID="pnlError" runat="server" Visible="false">
                    <Rock:NotificationBox ID="nbFail" runat="server" CssClass="alert alert-warning" Title="Warning" Text="Import completed with errors. Please contact App Development Team." />
                </asp:Panel>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>