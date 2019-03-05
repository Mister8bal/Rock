<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVMyWorkflowsLava.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Workflow.CCVMyWorkflowsLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

          <div class="panel-heading">
                        <h1 class="panel-title">
                            <asp:Literal ID="lIconHtml" runat="server" ><i class="fa fa-gear"></i></asp:Literal>
                        </h1>
                        <div class="panel-labels">
                            <Rock:HighlightLabel ID="hlblWorkflowId" runat="server" LabelType="Info" />
                            <Rock:HighlightLabel ID="hlblDateAdded" runat="server" LabelType="Default" />
                        </div>
                    </div>

        <asp:Literal ID="lContents" runat="server" />

    </ContentTemplate>
</asp:UpdatePanel>