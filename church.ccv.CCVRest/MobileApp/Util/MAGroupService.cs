using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Datamart.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    class MAGroupService
    {
        const int GroupRoleId_NeighborhoodGroupCoach = 50;
        const int GroupTypeId_NeighborhoodGroup = 49;

        const string GroupDescription_Key = "GroupDescription";
        const string ChildcareDescription_Key = "Childcare";
        const string FamilyPicture_Key = "FamilyPicture";

        const string GroupFilters_Key = "GroupFilters";
        const string ChildcareProvided_FilterKey = "Childcare Provided";

        public static bool IsNeighborhoodGroup( Group group )
        {
            if ( group.GroupTypeId == GroupTypeId_NeighborhoodGroup )
            {
                return true;
            }

            return false;
        }

        public static MAGroupMemberView GetViewForMember( GroupMember groupMember )
        {
            // simply use the status and role of the group member to determine
            // which MemberView they should get access to
            if ( groupMember.GroupRoleId == GroupRoleId_NeighborhoodGroupCoach )
            {
                return MAGroupMemberView.CoachView;
            }
            else if ( groupMember.GroupMemberStatus == GroupMemberStatus.Active )
            {
                return MAGroupMemberView.MemberView;
            }
            else
            {
                return MAGroupMemberView.NonMemberView;
            }
        }

        public static List<MAGroupModel> GetMobileAppGroups( string nameKeyword,
                                                                    string descriptionKeyword,
                                                                    Location locationForDistance,
                                                                    bool? requiresChildcare,
                                                                    int? skip,
                                                                    int top )
        {
            // Gets Neighborhood Groups, searches by the provided arguments, and returns matching values as MobileAppGroupModels

            // The id for the group description on Neighborhood groups. Used for joining the attributeValue if a descriptionKeyword is provided.
            const int AttributeId_GroupDescription = 13055;

            // The id for the group filters on Neighborhood groups. Used for joininig the attributeValue to see if it contains "Childcare"
            const int AttributeId_GroupFilters = 42850;

            // First get all neighborhood groups, filtered by name and description if the caller provided those keywords
            RockContext rockContext = new RockContext();

            // get all groups of this group type that are public, and have a long/lat we can use
            GroupService groupService = new GroupService( rockContext );
            IEnumerable<Group> groupList = groupService.Queryable( "Schedule,GroupLocations.Location" ).AsNoTracking()
                                                       .Where( a => a.GroupTypeId == GroupTypeId_NeighborhoodGroup && a.IsPublic == true )
                                                       .Include( a => a.GroupLocations ).Where( a => a.GroupLocations.Any( x => x.Location.GeoPoint != null ) );

            // if they provided name keywords, filter by those
            if ( string.IsNullOrWhiteSpace( nameKeyword ) == false )
            {
                groupList = groupList.Where( a => a.Name.ToLower().Contains( nameKeyword.ToLower() ) );
            }

            // if they provided description, we need to join the attribute value table
            if ( string.IsNullOrWhiteSpace( descriptionKeyword ) == false )
            {
                // Join the attribute value that defines the GroupDescription with the group
                var avQuery = new AttributeValueService( rockContext ).Queryable().AsNoTracking().Where( av => av.AttributeId == AttributeId_GroupDescription );
                var joinedQuery = groupList.Join( avQuery, g => g.Id, av => av.EntityId, ( g, av ) => new { Group = g, GroupDesc = av.Value } );

                // see if the GroupDescription attribute value has the description keyword in it
                groupList = joinedQuery.Where( g => g.GroupDesc.ToLower().Contains( descriptionKeyword.ToLower() ) ).Select( g => g.Group );
            }

            // if they require childcare, we again need to join the attribute value table
            if ( requiresChildcare == true )
            {
                // Join the attribute vale that defines Group Filters (where Childcare is) with the group
                var avQuery = new AttributeValueService( rockContext ).Queryable().AsNoTracking().Where( av => av.AttributeId == AttributeId_GroupFilters );
                var joinedQuery = groupList.Join( avQuery, g => g.Id, av => av.EntityId, ( g, av ) => new { Group = g, Filter = av.Value } );

                // see if the GroupFilter attribute value contains the ChildcareProvided filter key
                groupList = joinedQuery.Where( g => g.Filter.Contains( ChildcareProvided_FilterKey ) ).Select( g => g.Group );
            }


            // calculate the distance of each of the group's locations from the specified geoFence
            if ( locationForDistance != null )
            {
                // pull it into memory, because we have to in order to store distances on the location and sort by that
                // (This could be optimized by creating a lookup table and sending that to sql)
                groupList = groupList.ToList();

                foreach ( var group in groupList )
                {
                    foreach ( var gl in group.GroupLocations )
                    {
                        // Calculate distance
                        if ( gl.Location.GeoPoint != null )
                        {
                            double meters = gl.Location.GeoPoint.Distance( locationForDistance.GeoPoint ) ?? 0.0D;
                            gl.Location.SetDistance( meters * Location.MilesPerMeter );
                        }
                    }
                }

                // and sort by the set distance
                groupList = groupList.OrderBy( a => a.GroupLocations.First().Location.Distance ).ToList();
            }


            // grab the nth set
            if ( skip.HasValue )
            {
                groupList = groupList.Skip( skip.Value ).ToList();
            }

            // and take the top amount
            groupList = groupList.Take( top ).ToList();


            // Now package the groups into GroupResult objects that store what the Mobile App cares about
            List<MAGroupModel> groupResultList = new List<MAGroupModel>();

            // now take only what we need from each group
            foreach ( Group group in groupList )
            {
                MAGroupModel groupResult = GetMobileAppGroup( group, MAGroupMemberView.NonMemberView );

                if ( groupResult != null )
                {
                    groupResultList.Add( groupResult );
                }
            }

            return groupResultList;
        }

        public enum MAGroupMemberView
        {
            NonMemberView,
            MemberView,
            CoachView
        }
        
        public static MAGroupModel GetMobileAppGroup( Group group, MAGroupMemberView groupMemberView )
        {
            RockContext rockContext = new RockContext();
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // now get the group leader. If there isn't one, we'll fail, because we don't want a group with no leader
            GroupMember leader = group.Members.Where( gm => GroupRoleId_NeighborhoodGroupCoach == gm.GroupRole.Id ).FirstOrDefault();
            if ( leader == null )
            {
                return null;
            }

            // make sure the leader has a datamart entry, or again, we need to simply fail
            var datamartPersonService = new DatamartPersonService( rockContext ).Queryable().AsNoTracking();
            var datamartPerson = datamartPersonService.Where( dp => dp.PersonId == leader.Person.Id ).SingleOrDefault();
            if ( datamartPerson == null )
            {
                return null;
            }

            // we are guaranteed that there will be a location object due to our initial query
            Location locationObj = group.GroupLocations.First().Location;
            MAGroupModel groupResult = new MAGroupModel()
            {
                Id = group.Id,

                Name = group.Name,

                Longitude = locationObj.Longitude.Value,
                Latitude = locationObj.Latitude.Value,
                DistanceFromSource = locationObj.Distance,

                MeetingTime = group.Schedule != null ? group.Schedule.FriendlyScheduleText : "",

                Street = locationObj.Street1,
                City = locationObj.City,
                State = locationObj.State,
                Zip = locationObj.PostalCode
            };

            // if the leader has a neighborhood pastor (now called associate pastor) defined, grab their person object. (This is allowed to be null)
            Person associatePastor = null;
            if ( datamartPerson.NeighborhoodPastorId.HasValue )
            {
                // get the AP, but guard against a null value (could happen if the current ID is merged and the datamart hasn't re-run)
                associatePastor = new PersonService( rockContext ).Queryable().AsNoTracking()
                                                                  .Where( p => p.Id == datamartPerson.NeighborhoodPastorId.Value )
                                                                  .SingleOrDefault();
            }

            // take the person object and status for all active & pending non-coach members of the group
            var nonCoachGroupMembers = group.Members.Where( gm => GroupRoleId_NeighborhoodGroupCoach != gm.GroupRole.Id && gm.GroupMemberStatus != GroupMemberStatus.Inactive )
                                                    .Select( gm => new { gm.GroupMemberStatus, gm.Person }  )
                                                    .ToList();

            // Now setup the group members. The role passed in to this function determines the level of info we grab for each member
            groupResult.Members = new List<MAGroupMemberModel>();

            switch ( groupMemberView )
            {
                // a coach should see everything about everyone
                case MAGroupMemberView.CoachView:
                {
                    // AP
                    if ( associatePastor != null )
                    {
                        MAGroupMemberModel maAssociatePastor = GetMAGroupMemberModel( associatePastor, MAGroupRole.AssociatePastor, true, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
                        groupResult.Members.Add( maAssociatePastor );
                    }

                    // Coach (could be themself)
                    MAGroupMemberModel coachGroupMember = GetMAGroupMemberModel( leader.Person, MAGroupRole.Coach, true, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    groupResult.Members.Add( coachGroupMember );

                    // And all members (pending AND active)
                    foreach ( var groupMember in nonCoachGroupMembers )
                    {
                        MAGroupMemberModel maGroupMember = GetMAGroupMemberModel( groupMember.Person, MAGroupRole.Member, true, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                        groupResult.Members.Add( maGroupMember );
                    }
                    break;
                }

                // a member should see everything about the AP and Coach, and only names for the other members
                case MAGroupMemberView.MemberView:
                {
                    // AP
                    if ( associatePastor != null )
                    {
                        MAGroupMemberModel maAssociatePastor = GetMAGroupMemberModel( associatePastor, MAGroupRole.AssociatePastor, true, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
                        groupResult.Members.Add( maAssociatePastor );
                    }

                    // Coach
                    MAGroupMemberModel coachGroupMember = GetMAGroupMemberModel( leader.Person, MAGroupRole.Coach, true, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    groupResult.Members.Add( coachGroupMember );

                    // And all ACTIVE members, without their contact info
                    foreach ( var groupMember in nonCoachGroupMembers )
                    {
                        if ( groupMember.GroupMemberStatus == GroupMemberStatus.Active )
                        {
                            MAGroupMemberModel maGroupMember = GetMAGroupMemberModel( groupMember.Person, MAGroupRole.Member, false, Guid.Empty );
                            groupResult.Members.Add( maGroupMember );
                        }
                    }
                    break;
                }

                // non members should only see the AP and Coach, and get no contact info about anyone
                case MAGroupMemberView.NonMemberView:
                {
                    // AP
                    if ( associatePastor != null )
                    {
                        MAGroupMemberModel maAssociatePastor = GetMAGroupMemberModel( associatePastor, MAGroupRole.AssociatePastor, false, Guid.Empty );
                        groupResult.Members.Add( maAssociatePastor );
                    }

                    // Coach
                    MAGroupMemberModel coachGroupMember = GetMAGroupMemberModel( leader.Person, MAGroupRole.Coach, false, Guid.Empty );
                    groupResult.Members.Add( coachGroupMember );
                    break;
                }
            }


            // Finally, load attributes so we can set additional group info
            group.LoadAttributes();

            if ( group.AttributeValues.ContainsKey( GroupDescription_Key ) )
            {
                groupResult.Description = group.AttributeValues[GroupDescription_Key].Value;
            }

            if ( group.AttributeValues.ContainsKey( FamilyPicture_Key ) )
            {
                // build a URL for retrieving the group's pic
                Guid photoGuid = group.AttributeValues[FamilyPicture_Key].Value.AsGuid();
                if ( photoGuid.IsEmpty() == false )
                {
                    groupResult.PhotoURL = publicAppRoot + "GetImage.ashx?Guid=" + photoGuid;
                }
            }

            // get the childcare description whether the Childcare filter is set or NOT. This is
            // because some groups (like mine!) explain that they'd be willing to start childcare if the group grew.
            if ( group.AttributeValues.ContainsKey( ChildcareDescription_Key ) )
            {
                groupResult.ChildcareDesc = group.AttributeValues[ChildcareDescription_Key].Value;
            }

            // filters contain a comma delimited list of features the group offers. See if it has any.
            if ( group.AttributeValues.ContainsKey( GroupFilters_Key ) )
            {
                // The only one we currently care about it Childcare.
                if ( group.AttributeValues[GroupFilters_Key].Value.Contains( ChildcareProvided_FilterKey ) )
                {
                    groupResult.Childcare = true;
                }
            }

            return groupResult;
        }

        private static MAGroupMemberModel GetMAGroupMemberModel( Person person, MAGroupRole maGroupRole, bool includeContactInfo, Guid phoneType )
        {
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            MAGroupMemberModel maGroupMember = new MAGroupMemberModel();
            maGroupMember.PrimaryAliasId = person.PrimaryAliasId.Value;
            maGroupMember.Name = person.NickName + " " + person.LastName;
            maGroupMember.Role = maGroupRole;

            if ( person.PhotoId.HasValue )
            {
                maGroupMember.PhotoURL = publicAppRoot + "GetImage.ashx?Id=" + person.PhotoId.Value;
            }

            // if we should include contact info, put it
            if ( includeContactInfo )
            {
                var phoneNumber = person.GetPhoneNumber( phoneType );
                if ( phoneNumber != null )
                {
                    maGroupMember.PhoneNumberDigits = phoneNumber.Number;
                }

                maGroupMember.Email = person.Email;
            }

            return maGroupMember;
        }

        public enum RegisterPersonResult
        {
            Success,
            GroupNotFound,
            SecurityIssue,
            AlreadyInGroup
        }

        public static RegisterPersonResult RegisterPersonInGroup( JoinGroupModel regModel )
        {
            // the workflow type id for the alert note re-route
            const int AlertNoteReReouteWorkflowId = 166;

            // setup all variables we'll need
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var groupService = new GroupService( rockContext );

            DefinedValueCache connectionStatusPending = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT );
            DefinedValueCache recordStatusPending = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING );

            Person person = null;

            // first, get the group the person wants to join
            Group requestedGroup = groupService.Get( regModel.GroupId );
            if ( requestedGroup != null )
            {
                // Try to find person by name/email 
                var matches = personService.GetByMatch( regModel.FirstName.Trim(), regModel.LastName.Trim(), regModel.Email.Trim() );
                if ( matches.Count() == 1 )
                {
                    person = matches.First();
                }

                // Check to see if this is a new person
                if ( person == null )
                {
                    // If so, create the person and family record for the new person
                    person = new Person();
                    person.FirstName = regModel.FirstName.Trim();
                    person.LastName = regModel.LastName.Trim();
                    person.Email = regModel.Email.Trim();
                    person.IsEmailActive = true;
                    person.EmailPreference = EmailPreference.EmailAllowed;
                    person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    person.ConnectionStatusValueId = connectionStatusPending.Id;
                    person.RecordStatusValueId = recordStatusPending.Id;
                    person.Gender = Gender.Unknown;

                    PersonService.SaveNewPerson( person, rockContext, requestedGroup.CampusId, false );
                }

                // Save all changes
                rockContext.SaveChanges();

                // now, it's time to either add them to the group, or kick off the Alert Re-Route workflow
                // (Or nothing if there's no problem but they're already in the group)
                GroupMember primaryGroupMember = PersonToGroupMember( rockContext, person, requestedGroup );

                // does the person registering have alert notes?
                int alertNoteCount = new NoteService( rockContext ).Queryable().Where( n => n.EntityId == person.Id && n.IsAlert == true ).Count();

                if ( alertNoteCount > 0 )
                {
                    // First, check to see if an alert re-route workflow should be launched
                    WorkflowTypeCache alertRerouteWorkflowType = WorkflowTypeCache.Read( AlertNoteReReouteWorkflowId );

                    // yes they do. so kick off the re-route workflow so security can review.
                    Common.Util.LaunchWorkflow( rockContext, alertRerouteWorkflowType, primaryGroupMember );

                    return RegisterPersonResult.SecurityIssue;
                }
                // if above, we didn't flag that they should not join the group, let's add them
                else
                {
                    // try to add them to the group (would only fail if the're already in it)
                    if ( TryAddGroupMemberToGroup( rockContext, primaryGroupMember, requestedGroup ) )
                    {
                        return RegisterPersonResult.Success;
                    }
                    else
                    {
                        return RegisterPersonResult.AlreadyInGroup;
                    }
                }
            }

            return RegisterPersonResult.GroupNotFound;
        }

        private static GroupMember PersonToGroupMember( RockContext rockContext, Person person, Group group )
        {
            // puts a person into a group member object, so that we can pass it to a workflow
            GroupMember newGroupMember = new GroupMember();
            newGroupMember.PersonId = person.Id;
            newGroupMember.GroupRoleId = group.GroupType.DefaultGroupRole.Id;
            newGroupMember.GroupMemberStatus = GroupMemberStatus.Pending;
            newGroupMember.GroupId = group.Id;

            return newGroupMember;
        }

        /// <summary>
        /// Adds the group member to the group if they aren't already in it
        /// </summary>
        private static bool TryAddGroupMemberToGroup( RockContext rockContext, GroupMember newGroupMember, Group group )
        {
            if ( !group.Members.Any( m =>
                                      m.PersonId == newGroupMember.PersonId &&
                                      m.GroupRoleId == group.GroupType.DefaultGroupRole.Id ) )
            {
                var groupMemberService = new GroupMemberService( rockContext );
                groupMemberService.Add( newGroupMember );

                rockContext.SaveChanges();

                return true;
            }

            return false;
        }

        internal static APBoardModel GetAPBoardContent( int primaryAliasId )
        {
            // this will find the Content Channel associated with the coach's Associate Pastor
            // and package it into an APBoardModel

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            RockContext rockContext = new RockContext();

            // first, find the coach
            PersonAliasService paService = new PersonAliasService( rockContext );
            PersonAlias personAlias = paService.Get( primaryAliasId );
            if ( personAlias == null )
            {
                return null;
            }

            // make sure the coach has a datamart entry
            var datamartPersonService = new DatamartPersonService( rockContext ).Queryable().AsNoTracking();
            var datamartPerson = datamartPersonService.Where( dp => dp.PersonId == personAlias.PersonId ).SingleOrDefault();
            if ( datamartPerson == null )
            {
                return null;
            }

            // now get the associate pastor for the coach--if we can't, there's no way to get APBoard content
            Person associatePastor = null;
            if ( datamartPerson.NeighborhoodPastorId.HasValue )
            {
                associatePastor = new PersonService( rockContext ).Queryable().AsNoTracking()
                                                                    .Where( p => p.Id == datamartPerson.NeighborhoodPastorId.Value )
                                                                    .SingleOrDefault();
            }

            if ( associatePastor == null )
            {
                return null;
            }

            
            // the APBoard is tied to a Group Region of which the Associate Pastor is a member of.
            // so first, get all the APBoard content channel items
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );
            const int ContentChannelId_ToolboxAPBoard = 295; //dev value
            ContentChannel apBoard = contentChannelService.Get( ContentChannelId_ToolboxAPBoard );

            // now go through each APBoard Item (there's one per Region per Campus)
            ContentChannelItem apBoardItem = null;
            foreach ( var item in apBoard.Items )
            {
                item.LoadAttributes();

                // Get the Group Region this item is tied to, which will contain the Associate Pastor
                string regionVal = item.AttributeValues["Region"].Value.ToString();
                var group = new GroupService( rockContext ).Get( regionVal.AsGuid() );

                // now see if this Region contains the Coach's associate pastor
                if ( group.Members.Where( m => m.PersonId == associatePastor.Id ).Count() != 0 )
                {
                    // It did, so this is the appropriate AP Board item
                    apBoardItem = item;
                    break;
                }
            }

            // package it up
            APBoardModel apBoardModel = new APBoardModel();
            apBoardModel.AssociatePastorName = associatePastor.NickName + " " + associatePastor.LastName;
            apBoardModel.AssociatePastorImageURL = publicAppRoot + "GetImage.ashx?Id=" + associatePastor.PhotoId;

            apBoardModel.Summary = apBoardItem.Content;
            apBoardModel.Date = apBoardItem.StartDateTime.Date;
            apBoardModel.TipOfTheWeek = apBoardItem.AttributeValues["TipOfTheWeek"].ToString();

            return apBoardModel;
        }
    }
}
