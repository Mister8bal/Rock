<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ScheduleAVisit.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Cms.ScheduleAVisit" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="form-alerts">

            <Rock:NotificationBox ID="nbMessage" runat="server"></Rock:NotificationBox>

            <span id="nbHTMLMessage" class="alert hidden"></span>
        </div>

        <%-- Form Panel --%>
        <asp:Panel ID="pnlForm" runat="server">

            <div class="schedule-a-visit">

                <%-- Progress Tracker --%>
                <div class="progress">
                    <div class="step">

                        <asp:Button ID="btnProgressAdults" runat="server" ClientIDMode="Static" OnClientClick="btnProgress_OnClick('pnlChildren'); return false;" Text="1" CssClass="step-number active" />

                        <h5>Adults</h5>
                    </div>
                    <div class="step">

                        <asp:Button ID="btnProgressChildren" runat="server" ClientIDMode="Static" OnClientClick="btnProgress_OnClick('pnlSubmit'); return false;" Text="2" CssClass="step-number" />

                        <h5>Children</h5>
                    </div>
                    <div class="step">

                        <%-- Even though button not used for confirm, leaving it as disabled button for consistent/easier styling --%>
                        <asp:Button ID="btnProgressSubmit" runat="server" ClientIDMode="Static" OnClientClick="return false;" Text="3" CssClass="step-number" Enabled="false" />

                        <h5>Submit</h5>
                    </div>
                </div>

                <%-- Adults Form --%>
                <asp:Panel ID="pnlAdults" runat="server" ClientIDMode="Static" CssClass="panel-adults">

                    <div class="form-header">
                        <h2>Let us know you're coming</h2>
                        <p>We want to make your first visit as smooth and enjoyable as possible</p>
                    </div>

                    <div class="form">
                        <div class="adults-header">
                            <h4>Your name</h4>
                            <p>required</p>
                        </div>
                        <div class="form-row">
                            <Rock:RockTextBox ID="tbAdultFirstName" runat="server" Label="First" CssClass="required" />
                            <Rock:RockTextBox ID="tbAdultLastName" runat="server" Label="Last" CssClass="required" />
                        </div>
                        <div class="form-row">
                            <Rock:RockTextBox ID="tbAdultEmail" runat="server" Label="Email" CssClass="required" />
                        </div>
                        <div class="form-row">
                            <div class="form-field">
                                <Rock:DatePicker ID="dpVisitDate" runat="server" Label="Desired Date" CssClass="required" />
                            </div>
                            <div class="form-field">
                                <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" CssClass="required hidden" />
                            </div>
                            <div class="form-field">
                                <Rock:RockDropDownList ID="ddlServiceTime" runat="server" Label="Service Time" CssClass="required hidden" />
                            </div>
                        </div>
                        <div class="form-row hidden">
                            <Rock:RockTextBox ID="tbSpouseFirstName" runat="server" Label="First" />
                            <Rock:RockTextBox ID="tbSpouseLastName" runat="server" Label="Last" />
                        </div>
                    </div>

                    <div class="form-navigation">
                        <asp:Button ID="btnAdultsNext" runat="server" ClientIDMode="Static" OnClientClick="btnNext_OnClick('pnlChildren'); return false;" Text="Next" CssClass="btn btn-primary" Enabled="false" />                       
                    </div>

                </asp:Panel>

                <%-- Children Form --%>
                <asp:Panel ID="pnlChildren" runat="server" ClientIDMode="Static" CssClass="panel-children hidden">

                    <div class="children-form hidden">

                        <div class="form-header">
                            <h2>Child Pre-Register</h2>
                            <p>Security is very important to us. Your mobile number will be used to log into your family on our system, and to contact you for any reason during service.</p>
                        </div>

                        <div class="form">
                            <div class="children-header">
                                <p>required</p>
                            </div>
                            <div class="form-row">
                                <Rock:RockTextBox ID="tbMobileNumber" runat="server" Label="Mobile (Parent or Guardian)" CssClass="required" />
                            </div>
                            <div class="form-row">
                                <Rock:RockTextBox ID="tbChildFirstName" runat="server" Label="Child's First Name" CssClass="required" />
                            </div>
                            <div class="form-row">
                                <Rock:RockTextBox ID="tbChildLastName" runat="server" Label="Child's Last Name" CssClass="required" />
                            </div>
                            <div class="form-row">
                                <Rock:DatePartsPicker ID="dppChildBDay" runat="server" Label="Birthday" OnSelectedDatePartsChanged="dppChildBDay_SelectedDatePartsChanged" />
                            </div>
                            <div class="child-optional">
                                <div class="form-row">
                                    <div class="optional-button-group">


                                    </div>
                                    <div class="optional-button-group">

                                    </div>
                                </div>
                                <div class="form-row">
                                    <div class="form-field">
                                        <Rock:RockTextBox ID="tbAllergies" runat="server" Placeholder="List Allergies:" CssClass="allergies hidden" />
                                    </div>
                                    <div class="form-field">
                                        <Rock:RockDropDownList ID="ddlChildGrade" runat="server" Label="Grade" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="form-navigation">
                        <asp:Button ID="btnChildrenBack" runat="server" ClientIDMode="Static" OnClientClick="btnNext_OnClick('pnlAdults'); return false;" Text="Back" CssClass="btn" />                       
                        <asp:Button ID="btnChildrenAddAnother" runat="server" OnClick="btnChildrenAddAnother_Click" Text="Add another child?" CssClass="btn" />
                        <asp:Button ID="btnChildNext" runat="server" ClientIDMode="Static" OnClientClick="btnNext_OnClick('pnlSubmit'); return false;" Text="Next" CssClass="btn btn-primary" />
                    </div>

                </asp:Panel>

                <%-- Submit Panel --%>
                <asp:Panel ID="pnlSubmit" runat="server" ClientIDMode="Static" CssClass="panel-submit hidden">

                     <div class="submit-form hidden">

                        <div class="form-header">
                            <h2>Confirm Your Visit</h2>
                            <p>Make sure to visit the New to CCV table when you arrive!</p>
                        </div>

                        <div class="visit-details">
                            
                            <asp:LinkButton ID="lbEditVisitDetails" runat="server" Text="Edit details" />
                        </div>

                         <div class="submit-survey">
                             <h4>How did you hear about CCV?</h4>
                             <div class="survey-buttons">
                                 <div>Drive By / New to area</div>
                                 <div>Current Member</div>
                                 <div>Online Search</div>
                                 <div>Advertising</div>
                                 <div>Stars sports program</div>
                                 <div>Other</div>
                             </div>
                         </div>

                        <div class="form-navigation">
                            <asp:Button ID="btnSubmitBack" runat="server" ClientIDMode="Static" OnClientClick="btnNext_OnClick('pnlChildren'); return false;" Text="Back" CssClass="btn" />                       
                            <asp:Button ID="btnSubmitNext" runat="server" ClientIDMode="Static" OnClick="btnSubmitNext_Click" OnClientClick="btnNext_OnClick('pnlSubmit'); return false;" Text="Submit" CssClass="btn btn-primary" />
                        </div>


                </asp:Panel>

            </div>


        </asp:Panel>

        <asp:Panel ID="pnlSuccess" runat="server">


        </asp:Panel>






    </ContentTemplate>
</asp:UpdatePanel>

<script src="<%= RockPage.ResolveRockUrl( "~/Plugins/church_ccv/Cms/scripts/schedule-a-visit.js", true ) %>"></script>
