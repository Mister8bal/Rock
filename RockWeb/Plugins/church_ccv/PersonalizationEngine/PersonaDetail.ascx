<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonaDetail.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.PersonalizationEngine.PersonaDetail" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <div ID="error-panel" class="alert alert-danger" style="visibility: hidden; position: fixed; z-index:9999; left: 50%; top: 50%; transform: translate( -50%, -50%);">
            <p><i class="fa fa-times"></i> Fill in the fields colored red and try again.</p>
        </div>

        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Persona</h4>
                </div>
            </div>

            <div class="panel-body">
                <div class="persona-editable-item">
                    <asp:Literal runat="server"><p class="title">Persona Name</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ClientIDMode="Static" ID="tbPersonaName"></Rock:RockTextBox>
                </div>
                <br/>
                <div class="persona-editable-item">
                    <asp:Literal runat="server"><p class="title">Persona Description</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ClientIDMode="Static" ID="tbPersonaDesc"></Rock:RockTextBox>
                </div>
                <br/>
                <div class="persona-editable-item">
                    <asp:Literal runat="server"><p class="title">Persona Rock SQL</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="10" ID="tbPersonaRockSQL"></Rock:RockTextBox>
                </div>
                <br/>
                <asp:LinkButton ID="btnSave" ClientIDMode="Static" runat="server" Text="Save" CssClass="btn btn-primary" OnClientClick="return validateFields();" OnClick="btnSave_Click" />
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

<asp:UpdatePanel ID="upnlCampaigns" runat="server">
    <ContentTemplate>
        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Campaigns</h4>
                </div>
            </div>

            <div class="panel-body">
                <Rock:Grid ID="gCampaigns" Title="Attached Campaigns" runat="server" DisplayType="Light" AllowSorting="true" RowItemText="Campaign" AllowPaging="false" OnRowSelected="CampaignsGrid_RowSelected">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                        <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                        <Rock:DeleteField OnClick="CampaignsGrid_Delete" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>

        <Rock:ModalAlert ID="maWarning" runat="server" />
        <Rock:ModalDialog ID="mdAddCampaign" runat="server" ScrollbarEnabled="true" SaveButtonText="Add" OnSaveClick="mdAddCampaign_AddClick"  Title="Select Campaign">
            <Content>
                <Rock:RockDropDownList ID="ddlCampaign" runat="server" Label="Select Campaign" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>

<style>
    .persona-editable-item{
        display: flex;
        justify-content: flex-start;
    }
    
    .persona-editable-item .title {
        min-width: 150px;
    }

    .persona-types {
    }

    .persona-type {
        display: flex;
        flex-direction: column;
        margin-top: 25px;

        border-width: 1px;
        border-color: #b8b2aa;
        border-style: solid;
        border-radius: 4px;
        padding: 5px;
    }

    .persona-type-header {
    }

    .persona-type-template-item {
        display: flex;
        flex-direction: row;
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

<script>
    function pageLoad() {

        // setup event handlers for input that clear the red error box around fields.
        $("#tbPersonaName").on('input', function () {
            $(this).removeClass("error");
        });

        $("#tbPersonaDesc").on('input', function () {
            $(this).removeClass("error");
        });
    }

    function validateFields() {
        var success = true;

        // validate the persona name
        if (validateField("#tbPersonaName") == false) {
            success = false;
        }

        // validate the persona description
        if (validateField("#tbPersonaDesc") == false) {
            success = false;
        }

        if (success == false) {
            $("#error-panel").css("visibility", "visible");
            setTimeout(hideAlert, 5000);
        }

        return success;
    }

    function validateField(fieldId) {
        var fieldVal = $(fieldId).val();

        if (fieldVal) {
            $(fieldId).removeClass("error");
            return true;
        }
        else {
            $(fieldId).addClass("error");
            return false;
        }
    }

    function hideAlert() {
        $("#error-panel").css("visibility", "hidden");
    }
</script>