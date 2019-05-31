<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Tester.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.PersonalizationEngine.Tester" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Tester</h4>
                </div>
            </div>

            <div class="panel-body">
                <div class="persona-editable-item">
                    <Rock:PersonPicker ID="ppTarget" runat="server" Label="Person" OnSelectPerson="ppPerson_SelectPerson" />
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

<asp:UpdatePanel ID="upnlPersonas" runat="server">
    <ContentTemplate>
        <br/>
        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Personas</h4>
                </div>
            </div>

            <div class="panel-body">
                <asp:Literal ID="lNoPersona" Visible="false" runat="server">This person does not fit any Personas.</asp:Literal>
                <Rock:Grid ID="gPersonas" Title="Attached Personas" runat="server" DisplayType="Light" AllowSorting="true" RowItemText="Persona" AllowPaging="false">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                        <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                    </Columns>
                </Rock:Grid>
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
                <asp:Literal ID="lNoCampaigns" Visible="false" runat="server">This person will not see any campaigns.</asp:Literal>
                <Rock:Grid ID="gCampaigns" Title="Attached Campaigns" runat="server" DisplayType="Light" AllowSorting="true" RowItemText="Campaign" AllowPaging="false">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                        <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                        <Rock:RockBoundField DataField="IsDefault" HeaderText="Default" SortExpression="Default" />
                        <Rock:RockBoundField DataField="Priority" HeaderText="Priority" SortExpression="Priority" />
                        <Rock:RockBoundField DataField="Type" HeaderText="Locations" SortExpression="Type" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>
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