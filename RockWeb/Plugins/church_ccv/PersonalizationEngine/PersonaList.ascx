<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonaList.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.PersonalizationEngine.PersonaList" %>

<asp:UpdatePanel ID="upnlSettings" runat="server">
    <ContentTemplate>
        <div class ="panel panel-block">
            <div class="panel-heading">
                <div class="row col-sm-4">
                    <h4 class="panel-title">Personas</h4>
                </div>
            </div>        
            <div class="panel-body">
                <Rock:GridFilter ID="rPersonaFilter" runat="server" >
                    <Rock:RockTextBox ID="filterTbTitle" runat="server" Label="Name" />
                </Rock:GridFilter>
                <Rock:Grid ID="gPersonaGrid" AllowSorting="true" runat="server" OnRowSelected="PersonaGrid_RowSelected">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                        <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                        <Rock:DeleteField HeaderText="Remove" OnClick="PersonaGrid_Remove" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
