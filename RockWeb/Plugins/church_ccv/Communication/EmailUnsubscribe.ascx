<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EmailUnsubscribe.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Communication.EmailUnsubscribe" %>

<style>
    #<%=upSettings.ClientID%> strong {
        font-weight: bold;
    }

    #<%=upSettings.ClientID%> p {
        color: #000;
    }

    #<%=upSettings.ClientID%> ul {
        margin-bottom: 0;
    }

    #<%=upSettings.ClientID%> {
        width: 800px;
        min-height: 400px;
        margin: 0 auto;
    }

    #<%=divSettings.ClientID%>, #<%=divSuccess.ClientID%> {
        display: flex;
        flex-direction: column;
        align-items: center;
    }

    #<%=upSettings.ClientID%> h2 {
        margin-top: 40px;
    }

    .radio {
        align-self: flex-start;
        margin-left: 40px;
    }

    .radio label, .checkbox label {
        padding-left: 0;
    }

    #<%=divUnsubscribeCategories.ClientID%> li {
        display: flex;
        align-items: center;
    }

    .do-not-email-note {
        align-self: flex-start;
        margin-top: -12px;
        margin-left: 42px;
        font-size: 12px;
    }

    #<%=divNotInvolved.ClientID%> {
        width: 90%;
    }
</style>

<asp:UpdatePanel ID="upSettings" runat="server">
    <ContentTemplate>
        <div id="divSettings" runat="server">
            <h2>Update your email preferences</h2>
            <p>We're sorry if you've been receiving emails you don't want. It's our goal to only send emails that interest you! Please adjust your email preferences below and we'll update what we send you:</p>
            <br />
            <div class="radio">
                <Rock:RockRadioButton ID="rbUnsubscribe" runat="server" Text="I would like to receive the following types of marketing emails:" GroupName="EmailPreference" DisplayInline="false" CssClass="js-email-radio-option" Checked="true" />
                <div id="divUnsubscribeCategories" runat="server">
                    <ul>
                        <li>
                            <Rock:RockCheckBox ID="cbGeneral" runat="server" Checked="true" />
                            <label for="<%=cbGeneral.ClientID%>"><strong>CCV Updates & Newsletters:&nbsp</strong>Announcements, special events</label>
                        </li>
                        <li>
                            <Rock:RockCheckBox ID="cbNextGen" runat="server" Checked="true" />
                            <label for="<%=cbNextGen.ClientID%>"><strong>Kids and Students Ministry:&nbsp</strong>Special events, program updates</label>
                        </li>
                        <li>
                            <Rock:RockCheckBox ID="cbSummerCamps" runat="server" Checked="true" />
                            <label for="<%=cbSummerCamps.ClientID%>"><strong>Summer Camps:&nbsp</strong>Save the dates, planning updates</label>
                        </li>
                        <li>
                            <Rock:RockCheckBox ID="cbStars" runat="server" Checked="true" />
                            <label for="<%=cbStars.ClientID%>"><strong>Stars Sports:&nbsp</strong>Seasonal updates, registration dates</label>
                        </li>
                        <li>
                            <Rock:RockCheckBox ID="cbSpecialNeeds" runat="server" Checked="true" />
                            <label for="<%=cbSpecialNeeds.ClientID%>"><strong>Special Needs Ministry:&nbsp</strong>Special events, program updates</label>
                        </li>
                        <li>
                            <Rock:RockCheckBox ID="cbMissions" runat="server" Checked="true" />
                            <label for="<%=cbMissions.ClientID%>"><strong>Missions Ministry:&nbsp</strong>Upcoming trips, new destinations</label>
                        </li>
                        <li>
                            <Rock:RockCheckBox ID="cbMusic" runat="server" Checked="true" />
                            <label for="<%=cbMusic.ClientID%>"><strong>CCV Music:&nbsp</strong>Special events, album releases</label>
                        </li>
                    </ul>
                </div>
            </div>
            <div class="radio">
                <Rock:RockRadioButton ID="rbEmailPreferenceDoNotEmail" runat="server" Text="Please unsubscribe me from all emails." GroupName="EmailPreference" DisplayInline="false" CssClass="js-email-radio-option" />
            </div>
            <div class="do-not-email-note">
                *You will still receive important emails and updates regarding any ministry or programs you're actively involved in.
            </div>
            <div class="radio">
                <Rock:RockRadioButton ID="rbNotInvolved" runat="server" Text="I am no longer involved with Christ's Church of the Valley." GroupName="EmailPreference" DisplayInline="false" CssClass="js-email-radio-option" />
            </div>
            <div id="divNotInvolved" runat="server" style="display: none;">
                <Rock:RockDropDownList ID="ddlInactiveReason" runat="server" Label="Reason" />
                <Rock:RockTextBox ID="tbInactiveNote" runat="server" Label="More Info (optional)" TextMode="MultiLine" Rows="3" MaxLength="1000" />
            </div>
            <div class="actions">
                <asp:LinkButton ID="btnSubmit" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
            </div>
            <br />
            <br />
            <br />
        </div>
        <div id="divSuccess" runat="server" visible="false">            
            <h2 id="hSuccessTitle" runat="server">Thank You</h2>
            <br />
            <p id="pSuccessContent" runat="server">We have saved your email preference.</p>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>

<script>
    Sys.Application.add_load(function () {
        function toggleVisibility() {
            if ($('#<%=rbNotInvolved.ClientID%>').is(':checked')) {
                $('#<%=divNotInvolved.ClientID%>').slideDown('fast');

            } else if ($('#<%=rbUnsubscribe.ClientID%>').is(':checked')) {
                $('#<%=divNotInvolved.ClientID%>').slideUp('fast');
            }
            else {
                $('#<%=divNotInvolved.ClientID%>').slideUp('fast');
            }
        }

        $('.js-email-radio-option').click(function () {
            toggleVisibility();
        });

        toggleVisibility();
    });
</script>