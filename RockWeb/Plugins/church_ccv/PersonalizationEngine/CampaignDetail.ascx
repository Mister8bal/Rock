<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CampaignDetail.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.PersonalizationEngine.CampaignDetail" %>

<%--Temp! Move this to a style file --%>
<style>
    .campaign-editable-item{
        display: flex;
        justify-content: flex-start;
    }
    
    .campaign-editable-item .title {
        min-width: 150px;
    }

    .campaign-types {
        margin-top: 100px;
    }

    .campaign-type {
        display: flex;
        flex-direction: column;
        margin-top: 25px;

        border-width: 1px;
        border-color: black;
        border-style: solid;
    }

    .campaign-type-header {
    }

    .campaign-type-template-item {
        display: flex;
        flex-direction: row;
    }

    .form-control {
        width: 500px;
    }

</style>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Campaign</h4>
                </div>
            </div>

            <div class="panel-body">
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">Campaign Name</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ID="tbCampaignName"></Rock:RockTextBox>    
                </div>
                <br/>
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">Campaign Description</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ID="tbCampaignDesc"></Rock:RockTextBox>
                </div>
                <br/>
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">Start Date</p></asp:Literal>
                    <Rock:DateTimePicker ID="dtpStartDate" runat="server" Required="true" />
                </div>
                <br/>
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">End Date</p></asp:Literal>
                    <Rock:DateTimePicker ID="dtpEndDate" runat="server" Required="true" />
                </div>
                <br/>
                <div class="campaign-types">
                    <asp:PlaceHolder runat="server" ID="phContentJson"></asp:PlaceHolder>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
