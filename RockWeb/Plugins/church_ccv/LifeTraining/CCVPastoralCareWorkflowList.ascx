<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVPastoralCareWorkflowList.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Workflow.CCVPastoralCareWorkflowList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lIconHtml" runat="server" ><i class="fa fa-gear"></i> Pastoral Care Requests</asp:Literal>
                </h1>
            </div>
            <div class="panel-body">
                <Rock:HighlightLabel ID="hlblWorkflowId" runat="server" LabelType="Info" />
                <Rock:HighlightLabel ID="hlblDateAdded" runat="server" LabelType="Default" />
            </div>
            <asp:Literal ID="lContents" runat="server" />
        </div>
    </ContentTemplate>
</asp:UpdatePanel>