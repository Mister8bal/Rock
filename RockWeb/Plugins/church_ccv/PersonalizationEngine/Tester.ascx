<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Tester.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.PersonalizationEngine.Tester" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class ="panel panel-block">
                <div class="panel-heading">
                    <div class="row col-sm-4">
                        <h4 class="panel-title">Tester</h4>
                    </div>
                </div>

                <div class="panel-body">
                    <div class="row">
                        <div class="col-md-3">
                            <Rock:PersonPicker ID="ppTarget" runat="server" Label="Person" OnSelectPerson="ppPerson_SelectPerson" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-sm-6">
                            <asp:Literal runat="server"><strong>Target Date</strong></asp:Literal>
                            <Rock:DatePicker runat="server" ClientIDMode="Static" ID="dtpTargetDate" AutoPostBack="true" OnTextChanged="dpTargetDate_TextChanged"/>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-sm-12">
                            <br />
                            <span>Campaign Start Date is inclusive, and End Date is exclusive.</span>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-sm-12">
                            <span><strong>Example:</strong> Start: 6-1-19 End: 6-7-19 means it begins showing on 6-1-19, and 6-6-19 is the last day it shows.</span>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>
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
                <Rock:Grid ID="gCampaigns" Title="Attached Campaigns" runat="server" DisplayType="Light" RowItemText="Campaign" AllowPaging="false">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                        <Rock:RockBoundField DataField="Priority" HeaderText="Priority" />
                        <Rock:RockBoundField DataField="Type" HeaderText="Locations" />
                        <Rock:RockBoundField DataField="StartDate" HeaderText="Start Date" SortExpression="StartDate" DataFormatString="{0:M/dd/yy}" />
                        <Rock:RockBoundField DataField="EndDate" HeaderText="End Date" SortExpression="EndDate" DataFormatString="{0:M/dd/yy}"/>
                    </Columns>
                </Rock:Grid>
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
                <Rock:Grid ID="gPersonas" Title="Attached Personas" runat="server" DisplayType="Light" RowItemText="Persona" AllowPaging="false">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                        <Rock:RockBoundField DataField="Description" HeaderText="Description" />
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

    .error {
        border-width: 3px;
        border-color: #9d3f3d;
        border-style: solid;
    }

    /* hide the "Today" piece of the date time picker */
    .datepicker-days tfoot {
        display: none;
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