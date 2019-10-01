<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CCVPublicProfileEdit.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Cms.CCVPublicProfileEdit" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <script>
            $(function () {
                $(".photo a").fluidbox();
            });
        </script>

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-user"></i>&nbsp;My Account</h1>
            </div>
            <div class="panel-body">
                <asp:HiddenField ID="hfPersonId" runat="server" />
                <Rock:NotificationBox ID="nbNotAuthorized" runat="server" Text="You must be logged in to view your account." NotificationBoxType="Danger" Visible="false" />
                <asp:Panel ID="pnlView" runat="server">
                    <div>
                        <div class="person-head-row">
                            <div class="person-row">
                                <div class="person-photo">
                                    <asp:Literal ID="lImage" runat="server" />
                                    <div class="nextstep-modal-baptism-buttons-person-profile">
                                        <div>
                                            <asp:Literal ID="lLeaderBaptismPhoto" runat="server" />
                                        </div>
                                        <div>
                                            <asp:Literal ID="lLeaderCertificate" runat="server" />
                                        </div>
                                    </div>
                                </div>
                                <div class="person-profile-info no-flex-zone">
                                    <h1 class="person-head-name">
                                        <asp:Literal ID="lName" runat="server" />
                                        <div>
                                            <Rock:RockDropDownList ID="ddlGroup" runat="server" DataTextField="Name" DataValueField="Id" OnSelectedIndexChanged="ddlGroup_SelectedIndexChanged" AutoPostBack="true" Visible="false" />
                                        </div>
                                    </h1>
                                    <div class="person-info-details">
                                        <div class="details">
                                            <ul class="person-demographics list-unstyled">
                                                <li>
                                                    <asp:Literal ID="lAge" runat="server" /></li>
                                                <li>
                                                    <asp:Literal ID="lGender" runat="server" /></li>
                                                <li>
                                                    <asp:Literal ID="lMaritalStatus" runat="server" /></li>
                                                <li>
                                                    <asp:Literal ID="lGrade" runat="server" /></li>
                                            </ul>
                                            <div class="person-head-address"><asp:Literal ID="lAddress" runat="server" /></div>
                                        </div>
                                        <div class="person-contact-info">
                                            <ul class="phone-list list-unstyled">
                                                <div class="phone-numbers">
                                                    <asp:Repeater ID="rptPhones" runat="server">
                                                        <ItemTemplate>
                                                            <li><%# (bool)Eval("IsUnlisted") ? "Unlisted" : FormatPhoneNumber( Eval("CountryCode"), Eval("Number") ) %> <small><%# Eval("NumberTypeValue.Value") %></small></li>
                                                        </ItemTemplate>
                                                    </asp:Repeater>
                                                </div>
                                                <div class="person-head-email"><asp:Literal ID="lEmail" runat="server" /></div>
                                            </ul>
                                        </div>
                                    </div>                             
                                    <div>
                                        <asp:Repeater ID="rptPersonAttributes" runat="server">
                                            <ItemTemplate>
                                                <div>
                                                    <b><%# Eval("Name") %></b></br><small><%# Eval("Value") %></small>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                    <div>
                                        <div>
                                            <asp:Literal ID="lFamilyHeader" runat="server" Text="<h4>Family Information</h4>" Visible="false" />
                                        </div>
                                        <asp:Repeater ID="rptGroupAttributes" runat="server">
                                            <ItemTemplate>
                                                <div>
                                                    <b><%# Eval("Name") %></b></br><small><%# Eval("Value") %></small>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </div>
                            </div>
                      <div class="person-head-button"><asp:LinkButton ID="lbEditPerson" runat="server" CssClass="btn btn-primary btn-xs btn-spacing" OnClick="lbEditPerson_Click" CausesValidation="false"> Update</asp:LinkButton></div>
                   </div>

                    </div>
                    <hr />

                    <h3>
                        <asp:Literal ID="lGroupName" runat="server" />
                    </h3>
                    <asp:Repeater ID="rptGroupMembers" runat="server" OnItemDataBound="rptGroupMembers_ItemDataBound" OnItemCommand="rptGroupMembers_ItemCommand">
                        <ItemTemplate>
                            <div>
                                <div class="person-row">
                                    <div class="person-photo">
                                        <asp:Literal ID="lGroupMemberImage" runat="server" />
                                         <div class="nextstep-modal-baptism-buttons-person-profile">
                                            <div>
                                                <asp:Literal ID="lBaptismPhoto" runat="server" />
                                            </div>
                                            <div>
                                                <asp:Literal ID="lCertificate" runat="server" />
                                            </div>
                                        </div>
                                    </div>
                                    <div class="person-profile-info">
                                        <div class="person-info-details">
                                            <div class="details">
                                                <div class="person-family-name">
                                                  <asp:Literal ID="lGroupMemberName" runat="server" /></b>
                                                </div>
                                                <ul class="person-demographics list-unstyled">
                                                    <li>
                                                        <asp:Literal ID="lAge" runat="server" /></li>
                                                    <li>
                                                        <asp:Literal ID="lGender" runat="server" /></li>
                                                    <li>
                                                        <asp:Literal ID="lMaritalStatus" runat="server" /></li>
                                                    <li>
                                                        <asp:Literal ID="lGrade" runat="server" /></li>
                                                </ul>
                                            </div>
                                            <div class="person-contact-info">
                                                <ul class="phone-list list-unstyled">
                                                    <div class="phone-numbers">
                                                        <asp:Repeater ID="rptGroupMemberPhones" runat="server">
                                                            <ItemTemplate>
                                                                <li><%# (bool)Eval("IsUnlisted") ? "Unlisted" : FormatPhoneNumber( Eval("CountryCode"), Eval("Number") ) %> <small><%# Eval("NumberTypeValue.Value") %></small></li>
                                                            </ItemTemplate>
                                                        </asp:Repeater>
                                                    </div>
                                                    <div><asp:Literal ID="lGroupMemberEmail" runat="server" /></div>
                                                </ul>
                                            </div>
                                         </div>
                                    </div>
                                </div>
                               
                                <div>
                                    <div>
                                        <asp:Repeater ID="rptGroupMemberAttributes" runat="server">
                                            <ItemTemplate>
                                                <div>
                                                    <b><%# Eval("Name") %></b></br><small><%# Eval("Value") %></small>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>  
                                </div>
                                <div class="person-edit-buttons">
                                    <asp:LinkButton ID="lbEditGroupMember" runat="server" CssClass="btn btn-primary btn-xs btn-spacing" CommandArgument='<%# Eval("PersonId") %>' CommandName="Update"> Update</asp:LinkButton>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                   <div class="person-add-buttons">
                       <asp:LinkButton ID="lbAddGroupMember" runat="server" CssClass="btn btn-primary btn-xs" OnClick="lbAddGroupMember_Click"> Add New Family Member</asp:LinkButton>
                       <asp:LinkButton ID="lbRequestChanges" runat="server" CssClass="btn btn-primary btn-xs" OnClick="lbRequestChanges_Click"> Request Additional Changes</asp:LinkButton>
                   </div> 
                </asp:Panel>

                <asp:Panel ID="pnlEdit" runat="server">
                    <asp:ValidationSummary ID="valValidation" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                    <div>

                        <div>
                            <Rock:ImageEditor ID="imgPhoto" runat="server" Label="Photo" BinaryFileTypeGuid="03BD8476-8A9F-4078-B628-5B538F967AFC" />
                            <Rock:NotificationBox ID="nbPhotoWarning" runat="server" NotificationBoxType="Warning" Visible="false" />
                        </div>

                        <div>
                            <Rock:RockDropDownList ID="ddlTitle" runat="server" CssClass="input-width-md" Label="Title" />
                            <Rock:DataTextBox ID="tbFirstName" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="FirstName" Required="true" />
                            <Rock:DataTextBox ID="tbNickName" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="NickName" />
                            <Rock:DataTextBox ID="tbLastName" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="LastName" Required="true" />
                            <Rock:RockDropDownList ID="ddlSuffix" CssClass="input-width-md" runat="server" Label="Suffix" />
                            <Rock:BirthdayPicker ID="bpBirthDay" runat="server" Label="Birthday" />
                            <Rock:RockRadioButtonList ID="rblRole" runat="server" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" Label="Role" Visible="false" AutoPostBack="true" OnSelectedIndexChanged="rblRole_SelectedIndexChanged" />
                            <div>
                                <div>
                                    <Rock:RockRadioButtonList ID="rblGender" runat="server" RepeatDirection="Horizontal" Label="Gender" Required="true">
                                        <asp:ListItem Text="Male" Value="Male" />
                                        <asp:ListItem Text="Female" Value="Female" />
                                        <asp:ListItem Text="Unknown" Value="Unknown" />
                                    </Rock:RockRadioButtonList>
                                </div>
                                <div>
                                    <%-- This YearPicker is needed for the GradePicker to work --%>
                                    <div style="display: none;">
                                        <Rock:YearPicker ID="ypGraduation" runat="server" Label="Graduation Year" Help="High School Graduation Year." />
                                    </div>
                                    <Rock:GradePicker ID="ddlGradePicker" runat="server" Label="Grade" UseAbbreviation="true" UseGradeOffsetAsValue="true" Visible="false" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <hr />

                    <asp:Panel ID="pnlPersonAttributes" runat="server">
                        <div class="panel-heading clearfix">
                            <h4 class="panel-title">Additional Information</h4>
                        </div>
                        <div class="panel-body">
                            <Rock:DynamicPlaceHolder ID="phPersonAttributes" runat="server" />
                        </div>
                        <hr />
                    </asp:Panel>

                    <asp:Panel ID="pnlFamilyAttributes" runat="server">
                        <div class="panel-heading clearfix">
                            <h4 class="panel-title">Family Information</h4>
                        </div>
                        <div class="panel-body">
                            <Rock:DynamicPlaceHolder ID="phFamilyAttributes" runat="server" />
                        </div>
                        <hr />
                    </asp:Panel>

                    <div class="contact-information">
                        <h3>Contact Info</h3>
                        <asp:Repeater ID="rContactInfo" runat="server">
                            <ItemTemplate>
                                <div>
                                    <div class="control-label"><%# Eval("NumberTypeValue.Value")  %></div>
                                    <div class="controls">
                                        <div>
                                            <div>
                                                <asp:HiddenField ID="hfPhoneType" runat="server" Value='<%# Eval("NumberTypeValueId")  %>' />
                                                <Rock:PhoneNumberBox ID="pnbPhone" runat="server" CountryCode='<%# Eval("CountryCode")  %>' Number='<%# Eval("NumberFormatted")  %>' />
                                            </div>
                                            <div>
                                                <div>
                                                    <div>
                                                        <asp:CheckBox ID="cbSms" runat="server" Text="SMS" Checked='<%# (bool)Eval("IsMessagingEnabled") %>' CssClass="js-sms-number" />
                                                    </div>
                                                    <div>
                                                        <asp:CheckBox ID="cbUnlisted" runat="server" Text="Unlisted" Checked='<%# (bool)Eval("IsUnlisted") %>' />
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <div>
                            <div class="control-label">Email</div>
                            <div class="controls">
                                <Rock:DataTextBox ID="tbEmail" PrependText="<i class='fa fa-envelope'></i>" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="Email" Label="" />
                            </div>
                        </div>

                        <div>
                            <div class="controls">
                                <Rock:RockRadioButtonList ID="rblEmailPreference" runat="server" RepeatDirection="Horizontal" Label="Email Preference">
                                    <asp:ListItem Text="Email Allowed" Value="EmailAllowed" />
                                    <asp:ListItem Text="No Mass Emails" Value="NoMassEmails" />
                                    <asp:ListItem Text="Do Not Email" Value="DoNotEmail" />
                                </Rock:RockRadioButtonList>
                                
                                <Rock:RockRadioButtonList ID="rblCommunicationPreference" runat="server" RepeatDirection="Horizontal" Label="Communication Preference" >
                                    <asp:ListItem Text="Email" Value="1" />
                                    <asp:ListItem Text="SMS" Value="2" />
                                </Rock:RockRadioButtonList>
                            </div>
                        </div>
                    </div>

                    <asp:Panel ID="pnlAddress" runat="server">
                        <fieldset>
                            <legend>
                                <asp:Literal ID="lAddressTitle" runat="server" /></legend>

                            <div class="clearfix">
                                <div>
                                    <asp:Literal ID="lPreviousAddress" runat="server" />
                                </div>
                                <div>
                                    <asp:LinkButton ID="lbMoved" CssClass="btn btn-default btn-xs" runat="server" OnClick="lbMoved_Click"><i class="fa fa-truck"></i> Moved</asp:LinkButton>
                                </div>
                            </div>

                            <asp:HiddenField ID="hfStreet1" runat="server" />
                            <asp:HiddenField ID="hfStreet2" runat="server" />
                            <asp:HiddenField ID="hfCity" runat="server" />
                            <asp:HiddenField ID="hfState" runat="server" />
                            <asp:HiddenField ID="hfPostalCode" runat="server" />
                            <asp:HiddenField ID="hfCountry" runat="server" />

                            <Rock:AddressControl ID="acAddress" runat="server" RequiredErrorMessage="Your Address is Required" />

                            <div>
                                <Rock:RockCheckBox ID="cbIsMailingAddress" runat="server" Text="This is my mailing address" Checked="true" />
                                <Rock:RockCheckBox ID="cbIsPhysicalAddress" runat="server" Text="This is my physical address" Checked="true" />
                            </div>
                        </fieldset>
                    </asp:Panel>

                    <div class="actions">
                        <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                    </div>

                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
