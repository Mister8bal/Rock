<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LifeTrainingResourcePicker.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.LifeTraining.LifeTrainingResourcePicker" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"> Life Training Resources </h1>
            </div>
            <asp:Literal ID="lContents" runat="server" />
        </div>
    </ContentTemplate>
</asp:UpdatePanel>