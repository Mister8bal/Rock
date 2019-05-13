<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PlanAVisitGrid.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Cms.PlanAVisitGrid" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlPlanAVisitGrid" runat="server" class="panel panel-block">

            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Plan A Visit</h4>
                    <br />
                </div>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">

                    <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue">
                        <Rock:RockCheckBoxList ID="cblCampusFilter" runat="server" Label="Campus" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                        <Rock:RockCheckBoxList ID="cblScheduledServiceFilter" runat="server" Label="Scheduled Service" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                        <Rock:DateRangePicker ID="drpScheduledDateFilter" runat="server" Label="Scheduled Date" />
                        <Rock:RockTextBox ID="tbPersonNameFilter" runat="server" Label="Person's Name" />
                        <Rock:RockDropDownList ID="ddlBringingSpouseFilter" runat="server" Label="Bringing Spouse">
                            <asp:ListItem></asp:ListItem>
                            <asp:ListItem Text="Yes" Value="Yes"></asp:ListItem>
                            <asp:ListItem Text="No" Value="No"></asp:ListItem>
                        </Rock:RockDropDownList>
                        <Rock:RockDropDownList ID="ddlBringingChildrenFilter" runat="server" Label="Bringing Kids">
                            <asp:ListItem></asp:ListItem>
                            <asp:ListItem Text="Yes" Value="Yes"></asp:ListItem>
                            <asp:ListItem Text="No" Value="No"></asp:ListItem>
                        </Rock:RockDropDownList>
                        <Rock:RockDropDownList ID="ddlHasAttendedFilter" runat="server" Label="Has Attended">
                            <asp:ListItem></asp:ListItem>
                            <asp:ListItem Text="Yes" Value="Yes"></asp:ListItem>
                            <asp:ListItem Text="No" Value="No"></asp:ListItem>
                        </Rock:RockDropDownList>
                        <Rock:RockCheckboxList ID="cblAttendedServiceFilter" runat="server" Label="Attended Service" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                        <Rock:DateRangePicker ID="drpAttendedDateFilter" runat="server" Label="Attended Date" />
                    </Rock:GridFilter>

                    <Rock:Grid ID="gGrid" runat="server" AutoGenerateColumns="False" OnRowSelected="gGrid_RowSelected" AllowSorting="true">
                        <Columns>
                            <Rock:SelectField />
                            <Rock:RockBoundField DataField="Person" HeaderText="Person" HtmlEncode="false" />
                            <Rock:RockBoundField DataField="PersonId" Visible="false" />
                            <Rock:RockBoundField DataField="Campus" HeaderText="Campus" SortExpression="Campus"/>
                            <Rock:RockBoundField DataField="ScheduledDate" HeaderText="Scheduled Date" DataFormatString="{0:MM/d/yyyy}" SortExpression="ScheduledDate" />
                            <Rock:RockBoundField DataField="ScheduledServiceName" HeaderText="Scheduled Service" SortExpression="ScheduledServiceName" />
                            <Rock:RockBoundField DataField="BringingSpouse" HeaderText="Bringing Spouse" SortExpression="BringingSpouse" />                    
                            <Rock:RockBoundField DataField="BringingChildren" HeaderText="Bringing Kids" SortExpression="BringingChildren" />
                            <Rock:RockBoundField DataField="AttendedDate" HeaderText="Attended Date" DataFormatString="{0:MM/d/yyyy}" SortExpression="AttendedDate" />
                            <Rock:RockBoundField DataField="AttendedServiceName" HeaderText="Attended Service" SortExpression="AttendedServiceName" />
                            <Rock:DeleteField OnClick="gGrid_Delete" Visible="false" ID="gGridDeleteField" />
                        </Columns>
                    </Rock:Grid>

                    <asp:Panel ID="pnlManageVisit" runat="server" Visible="true">

                        <Rock:ModalDialog ID="mdManageVisit" runat="server" SaveButtonText="Save" CancelLinkVisible="true" OnSaveClick="mdManageVisit_SaveClick">
                            <Content>

                                <Rock:DatePicker ID="dpDateAttended" runat="server" Label="Date Attended?" AutoPostBack="true" OnTextChanged="dpDateAttended_TextChanged"  />

                                <Rock:RockDropDownList ID="ddlServiceAttended" runat="server" Label="Service Attended?" />

                                <asp:HiddenField ID="hfModalVisitId" runat="server" Value="" />

                            </Content>
                        </Rock:ModalDialog>

                    </asp:Panel>

                </div>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

