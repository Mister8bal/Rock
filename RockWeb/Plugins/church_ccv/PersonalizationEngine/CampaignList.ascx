<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CampaignList.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.PersonalizationEngine.CampaignList" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Campaigns</h4>
                </div>
            </div>
        </div>
        <Rock:GridFilter ID="rCampaignFilter" runat="server" >
            <Rock:RockTextBox ID="filterTbTitle" runat="server" Label="Name" />
            <Rock:RockCheckBoxList ID="filterCblType" runat="server" Label="Type" DataTextField="Type" DataValueField="Type" RepeatDirection="Horizontal" />
            <Rock:DateRangePicker ID="filterDrpDates" runat="server" Label="Dates" />
        </Rock:GridFilter>
        <Rock:Grid ID="gCampaignGrid" AllowSorting="true" runat="server" OnRowSelected="CampaignGrid_RowSelected">
            <Columns>
                <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                <Rock:RockBoundField DataField="StartDate" HeaderText="Start Date" SortExpression="StartDate" DataFormatString="{0:M/dd/yy}" />
                <Rock:RockBoundField DataField="EndDate" HeaderText="End Date" SortExpression="EndDate" DataFormatString="{0:M/dd/yy}"/>
                <Rock:RockBoundField DataField="Type" HeaderText="Locations" SortExpression="Type" />
                <Rock:RockBoundField DataField="Priority" HeaderText="Priority" SortExpression="Priority" />
                <Rock:DeleteField HeaderText="Remove" OnClick="CampaignGrid_Remove" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
