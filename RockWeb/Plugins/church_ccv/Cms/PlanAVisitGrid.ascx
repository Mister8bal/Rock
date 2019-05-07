<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PlanAVisitGrid.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Cms.PlanAVisitGrid" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pVolunteerScreeningList" runat="server" class="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Plan A Visit</h4>
                    <br />
                </div>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">

                    <Rock:Grid ID="gGrid" runat="server" AutoGenerateColumns="False" OnRowSelected="gGrid_RowSelected">
                        <Columns>
                            <Rock:RockBoundField DataField="ScheduledDate" HeaderText="Scheduled Date" DataFormatString="{0:MM/d/yyyy}" />
                            <Rock:RockBoundField DataField="ServiceTime" HeaderText="Service Time" />
                            <Rock:RockBoundField DataField="Campus" HeaderText="Campus"/>
                            <Rock:RockBoundField DataField="Person" HeaderText="Person" HtmlEncode="false" />
                            <Rock:RockBoundField DataField="BringingSpouse" HeaderText="Bringing Spouse" />                    
                            <Rock:RockBoundField DataField="BringingChildren" HeaderText="Bringing Kids" />
                            <Rock:RockBoundField DataField="AttendedDate" HeaderText="Attended Date" DataFormatString="{0:MM/d/yyyy}" />
                            <Rock:DeleteField OnClick="gGrid_Delete" />
                        </Columns>
                    </Rock:Grid>

                    <asp:Panel ID="pnlManageVisit" runat="server" Visible="true">
                        <Rock:ModalDialog ID="mdManageVisit" runat="server" SaveButtonText="Save" CancelLinkVisible="true" OnSaveClick="mdManageVisit_SaveClick">
                            <Content>
                                <h3>Editing <asp:Label runat="server" ID="lblModalPersonName" />'s Visit</h3>

                                <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" />

                                <Rock:Toggle ID="tglBringingSpouse" runat="server" Label="Bringing Spouse?" OnText="Yes" OffText="No" />

                                <Rock:Toggle ID="tglBringingChildren" runat="server" Label="Bringing Children?" OnText="Yes" OffText="No" />


                                <asp:HiddenField ID="hfModalVisitId" runat="server" Value="" />

                            </Content>
                        </Rock:ModalDialog>
                    </asp:Panel>

                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

