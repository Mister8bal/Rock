<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MobileAppAPBoard.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Groups.MobileAppAPBoard" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <asp:Literal ID="lTitle" runat="server"></asp:Literal>
                </div>
            </div>

            <div class="panel-body">
                <div class="editable-item">
                    <asp:Literal runat="server"><p class="title">Content</p></asp:Literal>
                    <Rock:RockTextBox TextMode="MultiLine" Rows="3" runat="server" ClientIDMode="Static" ID="tbContent"></Rock:RockTextBox>
                </div>
                <br/>
                <div class="editable-item">
                    <asp:Literal runat="server"><p class="title">Tip of the Week</p></asp:Literal>
                    <Rock:RockTextBox runat="server" TextMode="MultiLine" Rows="2" ClientIDMode="Static" ID="tbTipOfTheWeek"></Rock:RockTextBox>
                </div>
                <br/>
                <div class="editable-item">
                    <asp:Literal runat="server"><p class="title">Video (Wistia Id)</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ClientIDMode="Static" ID="tbVideo"></Rock:RockTextBox>
                </div>
                <br/>
                <asp:LinkButton ID="btnSave" ClientIDMode="Static" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

<asp:UpdatePanel ID="errorPanel" runat="server" Visible="false">
    <ContentTemplate>
        <div ID="error-panel" class="alert alert-danger" style="position: fixed; z-index:9999; left: 50%; top: 50%; transform: translate( -50%, -50%);">
            <p><i class="fa fa-times"></i> Couldn't load an AP Board Item.</p>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>


<style>
    .editable-item{
        display: flex;
        justify-content: flex-start;
        flex-wrap: wrap;
    }
    
    .editable-item .title {
        min-width: 150px;
    }

    .form-control {
        width: 500px;
    }

    .error {
        border-width: 3px;
        border-color: #9d3f3d;
        border-style: solid;
    }
</style>