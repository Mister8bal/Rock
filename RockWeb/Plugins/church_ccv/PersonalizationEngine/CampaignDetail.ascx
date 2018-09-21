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

    .error {
        border-width: 3px;
        border-color: #9d3f3d;
        border-style: solid;
    }

</style>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <div ID="error-panel" class="alert alert-danger" style="visibility: hidden; position: fixed; z-index:9999; left: 50%; top: 50%; transform: translate( -50%, -50%);">
            <p><i class="fa fa-times"></i> Fill in the fields colored red and try again.</p>
        </div>

        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Campaign</h4>
                </div>
            </div>

            <div class="panel-body">
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">Campaign Name</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ClientIDMode="Static" ID="tbCampaignName"></Rock:RockTextBox>
                </div>
                <br/>
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">Campaign Description</p></asp:Literal>
                    <Rock:RockTextBox runat="server" ClientIDMode="Static" ID="tbCampaignDesc"></Rock:RockTextBox>
                </div>
                <br/>
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">Start Date</p></asp:Literal>
                    <Rock:DatePicker runat="server" ClientIDMode="Static" ID="dtpStartDate"/>
                </div>
                <br/>
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">End Date</p></asp:Literal>
                    <Rock:DatePicker runat="server" ClientIDMode="Static" ID="dtpEndDate"/>
                </div>
                <br/>
                <div class="campaign-editable-item">
                    <asp:Literal runat="server"><p class="title">Priority</p></asp:Literal>
                    <Rock:RockTextBox ID="tbPriorty" ClientIDMode="Static" runat="server" type="number"/>
                </div>
                <br/>
                <div class="campaign-types">
                    <asp:PlaceHolder runat="server" ID="phContentJson"></asp:PlaceHolder>
                </div>

                <br/>
                <asp:LinkButton ID="btnSave" ClientIDMode="Static" runat="server" Text="Save" CssClass="btn btn-primary" OnClientClick="return validateFields();" OnClick="btnSave_Click" />
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

<script>
    function pageLoad() {

        // setup event handlers for input that clear the red error box around fields.
        $("#tbCampaignName").on('input', function () {
            $(this).removeClass("error");
        });

        $("#tbCampaignDesc").on('input', function () {
            $(this).removeClass("error");
        });

        // for the start date, take CHANGE, since the value can be inserted via the date button
        $("#dtpStartDate").on('change', function () {
            $(this).removeClass("error");
        });

        $("#tbPriorty").on('input', function () {
            $(this).removeClass("error");
        });
    }

    function validateFields() {
        var success = true;

        // validate the campaign name
        if (validateField("#tbCampaignName") == false) {
            success = false;
        }

        // validate the campaign description
        if (validateField("#tbCampaignDesc") == false) {
            success = false;
        }

        // validate the campaign start date
        if (validateField("#dtpStartDate") == false) {
            success = false;
        }

        // note - the end date CAN be blank, so we don't validate that.

        // validate the campaign priority
        if (validateField("#tbPriorty") == false) {
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