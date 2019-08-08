
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI.WebControls;
using church.ccv.Datamart.Model;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Groups
{
    [DisplayName( "Invite List" )]
    [Category( "CCV > Groups" )]
    [Description( "" )]

    [DataViewField( "Invite List Filter", "A required data view that provides initial filtering to the invite list.", true, "", "Rock.Model.Person", "", 0 )]
    [AttributeField( "72657ED8-D16E-492E-AC12-144C5E7567E7", "Last Invite Date Attribute", "A person attribute (Date) used to track when a person last received an invite.", true, false, "", "", 1 )]
    [IntegerField( "Days Till Next Invite", "How many days to wait before a person can be invited again.", true, 60, "", 2 )]
    [TextField( "Email Subject", "The email subject.", true, "Invitation to Neighborhood Group", "", 3 )]
    [CodeEditorField( "Message Template", "The message that is sent to the invitee.", Rock.Web.UI.Controls.CodeEditorMode.Html, Rock.Web.UI.Controls.CodeEditorTheme.Rock, 450, true,
@"We are the {{ CurrentPerson.LastName }} family and we lead a CCV Neighborhood Group close to you. As our neighbor I would like to invite you to our CCV Neighborhood group. We meet {{ Group.Schedule.FriendlyScheduleText }} at {{ Location.Street1 }}.
 
Our group is designed to building relationships together, encourage one another, and challenge one to grow in our faith as we all tackle life together. 
 
If your {{ Group.Schedule.WeeklyDayOfWeek }} nights are open, we’d love to have you join us!
 
If you have any further questions, please don’t hesitate to contact me directly. 
 
{{ CurrentPerson | FamilySalutation }}", "", 4 )]
    [IntegerField( "Max Distance From Group", "In Miles.  Limits the farthest distance a person can be considered for an invite.", true, 1, "", 5 )]
    [IntegerField( "Max Number of Households", "The maximum number of households that a coach is allowed to email.", true, 15, "", 6 )]
    [CodeEditorField( "Lava Template", "Lava template to use to display content.", Rock.Web.UI.Controls.CodeEditorMode.Lava, Rock.Web.UI.Controls.CodeEditorTheme.Rock, 450, true,
@"<div class='header'>
    <h4>Invite List</h4>
</div>

{[ googlemap showzoom:'false' showfullscreen:'false' markerannimation:'drop' ]}
    {% for household in InviteHouseholds %}
        [[ marker location:'{{ household.Location.GeoPoint }}' ]]
            {% for person in household.People %}
            <div class='media'>
                <div class='pull-left'>
                    <img src='{{ person.PhotoUrl }}' class='media-object' style='width:50px'>
                </div>
                <div class='media-body'>
                    <b>{{ person.NickName }} {{ person.LastName }}</b><br/>
                    <p>{{ person.Age }}</p>
                </div>
            </div>
            {% endfor %}
            <br/>
            <b>Address</b><br/>
            {{ household.Location.FormattedHtmlAddress }} ({{ household.Distance | DividedBy:1609.344 }} mi)
        [[ endmarker ]]
    {% endfor %}
{[ endgooglemap ]}", "", 7 )]
    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for this HTML block.", false, order: 8 )]
    [TextField( "Message Preview Notification", "The message that is displayed before sending invites.", true, "{0} people are selected to receive this invite.  Feel free to personalize the invite below.", "", 9 )]
    public partial class InviteList : RockBlock, ICustomGridColumns
    {
        #region Fields

        private List<InviteHousehold> _inviteHouseholds;
        private Location _location;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            _inviteHouseholds = ViewState["InviteHouseholds"].ToString().FromJsonOrNull<List<InviteHousehold>>();
            _location = ViewState["Location"].ToString().FromJsonOrNull<Location>();

            RenderTemplate( _inviteHouseholds, _location );
            BindGrid( _inviteHouseholds );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.GridRebind += gList_GridRebind;
            gList.ShowActionRow = false;
            gList.DataKeyNames = new string[] { "Id" };

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();

                if ( groupId.HasValue )
                {
                    _location = GetGroupLocation( groupId.Value );
                    if ( _location != null )
                    {
                        _inviteHouseholds = GetInviteList( _location );
                        if ( _inviteHouseholds != null && _inviteHouseholds.Any() )
                        {
                            RenderTemplate( _inviteHouseholds, _location );
                            BindGrid( _inviteHouseholds );
                        }
                    }
                    else
                    {
                        nbNotifications.Text = "Could not find a location for this group.";
                        nbNotifications.NotificationBoxType = NotificationBoxType.Warning;
                        nbNotifications.Visible = true;
                        pnlView.Visible = false;
                    }
                }
            }
            else
            {
                RenderTemplate( _inviteHouseholds, _location );
            }
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["InviteHouseholds"] = _inviteHouseholds.ToJson();
            ViewState["Location"] = _location.ToJson();

            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            //BindGrid();
        }

        protected void btnOpenEditor_Click( object sender, EventArgs e )
        {
            // display a message of how many people will be invited so that it is clear to the leader
            int personCount = 0;
            if ( gList.SelectedKeys.Count > 0 )
            {
                // if people were selected in the grid, only count those who were selected
                personCount = gList.SelectedKeys.Count();
            }
            else
            {
                // else count everyone
                personCount = _inviteHouseholds.SelectMany( h => h.People.Select( p => p ) ).Count();
            }
            nbRecepientCount.Text = string.Format( GetAttributeValue( "MessagePreviewNotification" ), personCount );

            var template = GetAttributeValue( "MessageTemplate" );

            // Going to pre-resolve the mergefields so the email template looks "ready to send" to the leader.
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            //mergeFields.Add( "CurrentPerson", CurrentPerson );

            int groupId = PageParameter( "GroupId" ).AsInteger();
            Group group = new GroupService( new RockContext() ).Get( groupId );
            mergeFields.Add( "Group", group );
            mergeFields.Add( "Location", GetGroupLocation( group.Id ) );

            template = template.ResolveMergeFields( mergeFields );

            // Add the person nickname to the top of the template to be resolved again when the email is sent
            template = "{{ Person.NickName }}," + System.Environment.NewLine + template;

            tbEmailEditor.Text = template;
            mdConfirmInvitation.Show();
        }

        /// <summary>
        /// Handles the Click event of the btnSendEmail control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendEmail_Click( object sender, EventArgs e )
        {
            List<int> personIds = new List<int>();
            if ( gList.SelectedKeys.Count > 0 )
            {
                // if people were selected in the grid, only count those who were selected
                personIds = gList.SelectedKeys.OfType<int>().ToList();
            }
            else
            {
                // else count everyone
                personIds = _inviteHouseholds.SelectMany( h => h.People.Select( p => p.Id ) ).ToList();
            }

            if ( personIds.Any() )
            {
                var rockContext = new RockContext();
                var attributeKey = AttributeCache.Read( GetAttributeValue( "LastInviteDateAttribute" ).AsGuid() ).Key;

                // get the list of people
                var people = new PersonService( rockContext ).Queryable().Where( p => personIds.Contains( p.Id ) ).ToList();

                var template = tbEmailEditor.Text;
                if ( template.IsNotNullOrWhiteSpace() )
                {
                    var recipients = new List<RecipientData>();

                    foreach ( var person in people )
                    {
                        // Save last invite date
                        person.LoadAttributes();
                        person.SetAttributeValue( attributeKey, RockDateTime.Now );
                        person.SaveAttributeValue( attributeKey, rockContext );

                        // add recipient
                        var personDict = new Dictionary<string, object>();
                        personDict.Add( "Person", person );
                        recipients.Add( new RecipientData( person.Email, personDict ) );
                    }

                    // send email
                    if ( recipients.Any() )
                    {
                        var emailMessage = new RockEmailMessage();
                        emailMessage.FromName = CurrentPerson.FullName;
                        emailMessage.FromEmail = CurrentPerson.Email;
                        emailMessage.Subject = GetAttributeValue( "EmailSubject" );
                        emailMessage.Message = template.ConvertCrLfToHtmlBr();
                        emailMessage.SetRecipients( recipients );
                        emailMessage.CreateCommunicationRecord = true;
                        emailMessage.Send();
                    }
                }
                else
                {
                    // handle missing template error
                }
            }
            else
            {
                // handle error
            }

            mdConfirmInvitation.Hide();
            nbNotifications.Text = "Invites sent!";
            nbNotifications.NotificationBoxType = NotificationBoxType.Success;
            nbNotifications.Visible = true;
            pnlView.Visible = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid( List<InviteHousehold> invitees )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );

            var people = invitees.SelectMany( i => i.People,
                ( i, People ) => new
                {
                    Id = People.Id,
                    Name = People.FullName,
                    NickName = People.NickName,
                    LastName = People.LastName,
                    Gender = People.Gender,
                    Email = People.Email,
                    Location = i.Location.Street1,
                } );

            gList.DataSource = people.OrderBy( p => p.LastName ).ThenBy( p => p.NickName ).ToList();
            gList.DataBind();
        }


        private List<InviteHousehold> GetInviteList( Location location )
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            IQueryable<int> inviteListPersonIds = null;

            //
            // first use the dataview to get our initial list of people
            //
            var dataViewGuId = GetAttributeValue( "InviteListFilter" ).AsGuidOrNull();
            if ( dataViewGuId.HasValue )
            {
                var dataView = new DataViewService( rockContext ).Get( dataViewGuId.Value );
                if ( dataView != null )
                {
                    var errorMessages = new List<string>();
                    inviteListPersonIds = dataView.GetQuery( null, rockContext, 120, out errorMessages ).Select( d => d.Id );
                }
            }
            else
            {
                // handle error
                nbNotifications.Text = "Please select a dataview in the block settings.";
                nbNotifications.Visible = true;

                return null;
            }

            //
            // Next, we will get a list of people that who were already notified recently
            //
            IQueryable<int> alreadyNotifiedPersonIdsQry = null;
            int daysTillNextInvite = GetAttributeValue( "DaysTillNextInvite" ).AsInteger();
            var attribCache = AttributeCache.Read( GetAttributeValue( "LastInviteDateAttribute" ).AsGuid() );
            if ( attribCache != null )
            {
                alreadyNotifiedPersonIdsQry = new AttributeValueService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( av => av.AttributeId == attribCache.Id &&
                                  av.EntityId.HasValue &&
                                  av.ValueAsDateTime != null &&
                                  av.ValueAsDateTime >= DbFunctions.AddDays( RockDateTime.Now, -daysTillNextInvite ) )
                    .Select( av => av.EntityId.Value );
            }
            else
            {
                // handle error
                nbNotifications.Text = "Please select a 'Last Invite Date Attribute' person attribute in the block settings.";
                nbNotifications.Visible = true;

                return null;
            }


            //
            // Finally, we will build a list of the closest invitees
            //
            var datamartFamilyQry = new Service<DatamartFamily>( rockContext ).Queryable().AsNoTracking();

            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var groupTypeFamilyId = GroupTypeCache.GetFamilyGroupType().Id;
            var groupMemberServiceQry = groupMemberService.Queryable( "Group" ).AsNoTracking()
                .Where( xx => xx.Group.GroupTypeId == groupTypeFamilyId );

            // filter by data view list
            groupMemberServiceQry = groupMemberServiceQry.Where( gm => inviteListPersonIds.Contains( gm.PersonId ) );

            // filter out people that have already been invited
            if ( alreadyNotifiedPersonIdsQry.Any() )
            {
                groupMemberServiceQry = groupMemberServiceQry.Where( gm => !alreadyNotifiedPersonIdsQry.Contains( gm.PersonId ) );
            }

            // filter only people with an email address and email allowed
            groupMemberServiceQry = groupMemberServiceQry.Where( gm => gm.Person.Email != null && gm.Person.EmailPreference == EmailPreference.EmailAllowed );

            int groupLocationTypeHomeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;
            if ( location.GeoPoint != null )
            {
                var query = groupMemberServiceQry
                    .Join( datamartFamilyQry,
                            groupMember => groupMember.GroupId,
                            datamartFamily => datamartFamily.FamilyId,
                            ( groupMember, datamartFamily ) => new
                            {
                                Person = groupMember.Person,
                                Family = datamartFamily
                            } );

                // filter those that have a home location that is within the max distance
                double miles = GetAttributeValue( "MaxDistanceFromGroup" ).AsInteger();
                double meters = miles * Location.MetersPerMile;
                List<Invitee> peopleWithDistance = query
                    .Where( xx => xx.Family.GeoPoint != null && 
                                  location.GeoPoint.Buffer( meters ).Intersects( xx.Family.GeoPoint ) )
                    .Select( xx => new Invitee
                    {
                        Person = xx.Person,
                        LocationId = xx.Family.LocationId,
                        Distance = xx.Family.GeoPoint.Distance( location.GeoPoint ),
                    } )
                    .ToList();

                // group by location since multiple people share the same location
                List<InviteHousehold> inviteHouseholds = peopleWithDistance
                    .GroupBy( gm => gm.Distance )
                    .Select( gm => new InviteHousehold
                    {
                        LocationId = gm.Select( i => i.LocationId ).FirstOrDefault(),
                        Distance = gm.Key,
                        People = gm.Select( i => i.Person ).ToList()
                    } )
                    .ToList();

                foreach ( var household in inviteHouseholds )
                {
                    if ( household.LocationId.HasValue )
                    {
                        household.Location = new LocationService( rockContext ).Get( household.LocationId.Value );
                    }                    
                }

                // return the max number of invitees that are closest
                int maxNum = GetAttributeValue( "MaxNumberofHouseholds" ).AsInteger();
                return inviteHouseholds
                    .Where( d => d.Distance != null )
                    .OrderBy( d => d.Distance )
                    .Take( maxNum )
                    .ToList();
            }

            return null;
        }

        /// <summary>
        /// Gets the group location that contains a valid geopoint.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns></returns>
        private Location GetGroupLocation( int groupId )
        {
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );

            Group group = groupService.Get( groupId );

            if ( group != null )
            {
                foreach ( var location in group.GroupLocations
                    .Where( gl => gl.Location.GeoPoint != null ) )
                {
                    return location.Location;
                }
            }

            return null;
        }

        private void RenderTemplate( List<InviteHousehold> inviteHouseholds, Location location )
        {
            string lavaTemplate = GetAttributeValue( "LavaTemplate" );
            if ( lavaTemplate.IsNotNullOrWhiteSpace() )
            {
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "InviteHouseholds", inviteHouseholds );
                mergeFields.Add( "Location", location );

                lTemplate.Text = lavaTemplate.ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
            }
        }

        #endregion
    }

    #region Helper Classes 

    public class Invitee
    {
        public Person Person { get; set; }

        public int? LocationId { get; set; }

        public double? Distance { get; set; }
    }

    [DotLiquid.LiquidType( "Location", "Distance", "People" )]
    public class InviteHousehold
    {
        public int? LocationId { get; set; }

        public Location Location { get; set; }

        public double? Distance { get; set; }

        public List<Person> People { get; set; }
    }

    #endregion
}