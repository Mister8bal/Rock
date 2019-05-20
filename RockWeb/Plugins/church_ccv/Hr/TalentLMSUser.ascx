<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TalentLMSUser.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Hr.TalentLMSUser" %>

<link rel="stylesheet" href="/Plugins/church_ccv/Hr/Styles/talent-lms.css">

<asp:HiddenField runat="server" ID="hfUserId" />

<asp:Panel runat="server" ID="pnlTalentLMS" CssClass="talentlms">

    <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-warning" />
    
    <asp:Panel runat="server" ID="pnlTalentLMSDashboard">
        
        <div class="talentlms-navigation">
            <h4 id="myCoursesToggle" class="navigation-item selected">My Courses</h4>
            <h4 id="allCoursesToggle" class="navigation-item">Available Courses</h4>
        </div>
        <div id="myCourses" class="course-grid">

            <asp:Repeater ID="rptUserCourses" runat="server">
                <HeaderTemplate>

                    <div class="course-header">
                        <span class="course-header-text">Course Name</span>
                        <span class="course-header-progress course-header-text">Progress</span>
                    </div>
    
                </HeaderTemplate>
                <ItemTemplate>
    
                    <div class="course-row">
                        <span class="course-text"><%# Eval("Name") %></span>
                        <div class="course-row-right">
                            <span class="course-progress"><%# Eval("Progress") %></span>
                            <span><%# Eval("LaunchButton") %></span>
                        </div>
                    </div>
  
                </ItemTemplate>
            </asp:Repeater>
            
        </div>
        <div id="allCourses" class="course-grid hidden">

            <asp:Repeater ID="rptAllCourses" runat="server">
                <ItemTemplate>

                    <div class="collapsible"><%# Eval("Name") %><i id='tglCourse<%# Eval("Id") %>' class="fas fa-chevron-down"></i></div>
                    <div class="course-info">
                        <div class="info-wrapper">
                            <p><%# Eval("Description") %></p>
                            <!-- Disabling Enroll button from Rock, Enroll rights are set at the talent LMS security group level and we need to discuss if its needed adding this -->
                            <!-- <asp:Button runat="server" ID="btnEnroll" CssClass="btn btn-primary btn-talentlms btn-enroll" OnClick="btnEnroll_Click" Text="Enroll" CommandArgument='<%# Eval("Id") %>' /> -->
                        </div>
                    </div>       

                </ItemTemplate>
            </asp:Repeater>

        </div>

    </asp:Panel>

</asp:Panel>

<script>


    function pageLoad() {
        $('#myCoursesToggle').on('click', function () {
            showCoursesPanel('#myCourses');
            hideCoursesPanel('#allCourses');
        });

        $('#allCoursesToggle').on('click', function () {
            showCoursesPanel('#allCourses');
            hideCoursesPanel('#myCourses');
        });

        var collapsibleRows = document.getElementsByClassName("collapsible");
        var i;

        for (i = 0; i < collapsibleRows.length; i++)
        {
            collapsibleRows[i].addEventListener("click", function ()
            {
                var content = this.nextElementSibling;
                var chevron = this.childNodes[1];

                if (content.style.maxHeight)
                {
                    content.style.maxHeight = null;
                    chevron.classList.remove('fa-chevron-up');
                    chevron.classList.add('fa-chevron-down');
                } else
                {
                    content.style.maxHeight = content.scrollHeight + "px";
                    chevron.classList.remove('fa-chevron-down');
                    chevron.classList.add('fa-chevron-up');
                }
            });
        }
    };

    function showCoursesPanel(panel) {
        $(panel).removeClass('hidden');
        $(panel + 'Toggle').addClass('selected');
    };

    function hideCoursesPanel(panel) {
        $(panel).addClass('hidden');
        $(panel + 'Toggle').removeClass('selected');
    };
</script>
