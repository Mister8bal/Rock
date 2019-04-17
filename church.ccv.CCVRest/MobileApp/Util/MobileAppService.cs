using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Linq;
using church.ccv.Actions;
using church.ccv.CCVRest.Common;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Datamart.Model;
using church.ccv.Podcast;
using Rock;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class MobileAppService
    {
        const int GroupRoleId_ChildInFamily = 4;

        const int GroupRoleId_NeighborhoodGroupCoach = 50;
        const int GroupTypeId_NeighborhoodGroup = 49;
        
        const string GroupDescription_Key = "GroupDescription";
        const string ChildcareDescription_Key = "Childcare";
        const string FamilyPicture_Key = "FamilyPicture";

        const string GroupFilters_Key = "GroupFilters";
        const string ChildcareProvided_FilterKey = "Childcare Provided";

        public static MobileAppPersonModel GetMobileAppPerson( int personId )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );

            // start by getting the person. if we can't do that, we should fail
            Person person = personService.Queryable().Include( a => a.PhoneNumbers ).Include( a => a.Aliases )
                .FirstOrDefault( p => p.Id == personId );

            if ( person == null )
            {
                return null;
            }

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            MobileAppPersonModel personModel = new MobileAppPersonModel();

            // first get their basic info
            personModel.PrimaryAliasId = person.PrimaryAliasId.Value; //See definition of PersonModel - person.PrimaryAliasId can never actually be null.
            personModel.FirstName = person.NickName;
            personModel.LastName = person.LastName;
            personModel.Email = person.Email;
            personModel.Birthdate = person.BirthDate;
            personModel.Age = person.Age;

            if ( person.PhotoId.HasValue )
            {
                personModel.PhotoURL = publicAppRoot + "GetImage.ashx?Id=" + person.PhotoId;
            }
            else
            {
                personModel.PhotoURL = string.Empty;
            }

            var mobilePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            if ( mobilePhoneType != null )
            {
                PhoneNumber phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == mobilePhoneType.Id );
                if ( phoneNumber != null )
                {
                    personModel.PhoneNumberDigits = phoneNumber.Number;
                }
            }

            // now get info about their family
            Group family = person.GetFamily();
            personModel.FamilyId = family.Id;

            // set their campus
            personModel.CampusId = family.CampusId;

            // set info about each family member
            personModel.FamilyMembers = new List<FamilyMemberModel>();
            foreach ( GroupMember groupMember in family.Members )
            {
                // don't add the person we're getting info about into their list of family members
                if ( groupMember.Person.Id != personId )
                {
                    FamilyMemberModel familyMember = new FamilyMemberModel
                    {
                        PrimaryAliasId = groupMember.Person.PrimaryAliasId ?? groupMember.Person.Id,
                        FirstName = groupMember.Person.FirstName,
                        LastName = groupMember.Person.LastName,
                        Age = groupMember.Person.Age
                    };

                    if ( groupMember.Person.PhotoId.HasValue )
                    {
                        familyMember.PhotoURL = publicAppRoot + "GetImage.ashx?Id=" + groupMember.Person.PhotoId;
                    }
                    else
                    {
                        familyMember.PhotoURL = string.Empty;
                    }

                    // if they're a child, set that, and also flag that this person HAS children
                    if ( groupMember.GroupRoleId == GroupRoleId_ChildInFamily )
                    {
                        familyMember.IsChild = true;
                        personModel.FamilyHasChildren = true;
                    }

                    personModel.FamilyMembers.Add( familyMember );
                }
                // however, since this IS the group member for that person, we can see if they're a child / adult
                else
                {
                    // if they're a child, set that, and also flag that this person HAS children
                    if ( groupMember.GroupRoleId == GroupRoleId_ChildInFamily )
                    {
                        personModel.IsChild = true;
                        personModel.FamilyHasChildren = true;
                    }
                }
            }

            // now get the neighborhood groups (and classes?) they're in
            personModel.Groups = new List<MobileAppGroupModel>();

            // lazily load each group member
            foreach ( GroupMember member in person.Members )
            {
                // and group; to see if it's a neighborhood group
                if ( member.Group.GroupTypeId == GroupTypeId_NeighborhoodGroup )
                {
                    // it is, so add it
                    MobileAppGroupModel groupResult = GetMobileAppGroup( member.Group );

                    if ( groupResult != null )
                    {
                        personModel.Groups.Add( groupResult );
                    }
                }
            }

            // now try to get ther home address for this family
            Guid? homeAddressGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuidOrNull();
            if ( homeAddressGuid.HasValue )
            {
                var homeAddressDv = DefinedValueCache.Read( homeAddressGuid.Value );
                if ( homeAddressDv != null )
                {
                    // take the group location flagged as a home address and mapped
                    GroupLocation familyAddress = family.GroupLocations
                        .Where( l =>
                            l.GroupLocationTypeValueId == homeAddressDv.Id &&
                            l.IsMappedLocation )
                        .FirstOrDefault();

                    if ( familyAddress != null )
                    {
                        personModel.Street1 = familyAddress.Location.Street1;
                        personModel.Street2 = familyAddress.Location.Street2;
                        personModel.City = familyAddress.Location.City;
                        personModel.State = familyAddress.Location.State;
                        personModel.Zip = familyAddress.Location.PostalCode;
                    }
                }
            }

            // their age determines whether we use adult vs student actions. If they have no age, or are >= 18, they're an adult
            if ( person.Age.HasValue == false || person.Age >= 18 )
            {
                DateTime? baptismDate;
                personModel.IsBaptised = Actions_Adult.Baptised.IsBaptised( person.Id, out baptismDate );
                personModel.IsWorshipping = Actions_Adult.ERA.IsERA( person.Id );
                personModel.IsGiving = Actions_Adult.Give.IsGiving( person.Id );
                
                Actions_Adult.PeerLearning.Result peerLearningResult;
                Actions_Adult.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                personModel.IsConnected = peerLearningResult.IsPeerLearning;

                Actions_Adult.Serving.Result servingResult;
                Actions_Adult.Serving.IsServing( person.Id, out servingResult );
                personModel.IsServing = servingResult.IsServing;

                Actions_Adult.Teaching.Result teachingResult;
                Actions_Adult.Teaching.IsTeaching( person.Id, out teachingResult );
                personModel.IsCoaching = teachingResult.IsTeaching;

                List<int> storyIds;
                personModel.SharedStory = Actions_Adult.ShareStory.SharedStory( person.Id, out storyIds );
            }
            // get the students version
            else
            {
                // for students, they become eligible for more badges as their age / grade goes up
                
                // note - this is intentionally NOT wrapped by one big age if-statement, even tho most of them are 9+ / 4th grade

                // worship - always eligible
                personModel.IsWorshipping = Actions_Student.ERA.IsERA( person.Id );

                // baptism is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    DateTime? baptismDate;
                    personModel.IsBaptised = Actions_Student.Baptised.IsBaptised( person.Id, out baptismDate );
                }
                else
                {
                    personModel.IsBaptised = null;
                }

                // giving is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    personModel.IsGiving = Actions_Student.Give.IsGiving( person.Id );
                }
                else
                {
                    personModel.IsGiving = null;
                }

                // connected is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    Actions_Student.PeerLearning.Result peerLearningResult;
                    Actions_Student.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                    personModel.IsConnected = peerLearningResult.IsPeerLearning;
                }
                else
                {
                    personModel.IsConnected = null;
                }

                // serving is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    Actions_Student.Serving.Result servingResult;
                    Actions_Student.Serving.IsServing( person.Id, out servingResult );
                    personModel.IsServing = servingResult.IsServing;
                }
                else
                {
                    personModel.IsServing = null;
                }

                // shared story is 14+ / 9th grade
                if ( person.Age >= 14 )
                {
                    List<int> storyIds;
                    personModel.SharedStory = Actions_Student.ShareStory.SharedStory( person.Id, out storyIds );
                }
                else
                {
                    personModel.SharedStory = null;
                }

                // Students are never eligible to be coaches
                personModel.IsCoaching = null;
            }

            return personModel;
        }

        public static UserLogin CreateNewLogin( NewUserModel newUserModel, Person person, bool autoConfirm )
        {
            RockContext rockContext = new RockContext();

            // and now create the login for this person
            try
            {
                UserLogin login = UserLoginService.Create(
                                rockContext,
                                person,
                                Rock.Model.AuthenticationServiceType.Internal,
                                EntityTypeCache.Read( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                                newUserModel.Username,
                                newUserModel.Password,
                                autoConfirm,
                                false );

                rockContext.SaveChanges();

                return login;
            }
            catch
            {
                // fail on exception
                return null;
            }
        }

        public static bool RegisterNewPerson( NewUserModel newUserModel )
        {
            RockContext rockContext = new RockContext();

            // get all required values and make sure they exist
            DefinedValueCache connectionStatusWebProspect = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT );
            DefinedValueCache recordStatusPending = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING );
            DefinedValueCache recordTypePerson = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );

            if ( connectionStatusWebProspect == null || recordStatusPending == null || recordTypePerson == null )
            {
                return false;
            }

            // create a new person, which will give us a new Id
            Person person = new Person();

            // for new people, copy the stuff sent by the Mobile App
            person.FirstName = newUserModel.FirstName.Trim();
            person.LastName = newUserModel.LastName.Trim();

            person.Email = newUserModel.Email.Trim();
            person.IsEmailActive = string.IsNullOrWhiteSpace( person.Email ) == false ? true : false;
            person.EmailPreference = EmailPreference.EmailAllowed;

            // now set values so it's a Person Record Type, and pending web prospect.
            person.ConnectionStatusValueId = connectionStatusWebProspect.Id;
            person.RecordStatusValueId = recordStatusPending.Id;
            person.RecordTypeValueId = recordTypePerson.Id;

            // now, save the person so that all the extra stuff (known relationship groups) gets created.
            Group newFamily = PersonService.SaveNewPerson( person, rockContext );

            // save all changes
            person.SaveAttributeValues( rockContext );
            rockContext.SaveChanges();

            // and now create the login for this person
            UserLogin newLogin = CreateNewLogin( newUserModel, person, true );
            return newLogin != null ? true : false;
        }

        public static void SendConfirmAccountEmail( Person person, UserLogin userLogin )
        {
            const string ConfirmAccountUrlRoute = "ConfirmAccount";

            // send an email to the person found asking them to confirm their account
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, person );

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            mergeFields.Add( "ConfirmAccountUrl", publicAppRoot + ConfirmAccountUrlRoute );

            mergeFields.Add( "Person", person );
            mergeFields.Add( "User", userLogin );

            var emailMessage = new RockEmailMessage( Rock.SystemGuid.SystemEmail.SECURITY_CONFIRM_ACCOUNT.AsGuid() );
            emailMessage.AddRecipient( new RecipientData( person.Email, mergeFields ) );
            emailMessage.CreateCommunicationRecord = false;
            emailMessage.Send();
        }

        public static bool SendForgotPasswordEmail( string personEmail )
        {
            // Define our constant values here in the function to keep them organized
            // Note that even tho this function is "ForgotPassword", technically it sends them a list of Usernames in an email,
            // which is why we use a Username Email Template
            const string ConfirmAccountUrlRoute = "ConfirmAccount";

            // setup merge fields
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            mergeFields.Add( "ConfirmAccountUrl", publicAppRoot + ConfirmAccountUrlRoute );
            var results = new List<IDictionary<string, object>>();

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var userLoginService = new UserLoginService( rockContext );

            // get all the accounts associated with the person(s) using this email address
            bool hasAccountWithPasswordResetAbility = false;
            List<string> accountTypes = new List<string>();

            foreach ( Person person in personService.GetByEmail( personEmail )
                .Where( p => p.Users.Any() ) )
            {
                var users = new List<UserLogin>();
                foreach ( UserLogin user in userLoginService.GetByPersonId( person.Id ) )
                {
                    if ( user.EntityType != null )
                    {
                        var component = AuthenticationContainer.GetComponent( user.EntityType.Name );
                        if ( component.SupportsChangePassword )
                        {
                            users.Add( user );
                            hasAccountWithPasswordResetAbility = true;
                        }

                        accountTypes.Add( user.EntityType.FriendlyName );
                    }
                }

                var resultsDictionary = new Dictionary<string, object>();
                resultsDictionary.Add( "Person", person );
                resultsDictionary.Add( "Users", users );
                results.Add( resultsDictionary );
            }

            // if we found user accounts that were valid, send the email
            if ( results.Count > 0 && hasAccountWithPasswordResetAbility )
            {
                mergeFields.Add( "Results", results.ToArray() );

                var emailMessage = new RockEmailMessage( Rock.SystemGuid.SystemEmail.SECURITY_FORGOT_USERNAME.AsGuid() );
                emailMessage.AddRecipient( new RecipientData( personEmail, mergeFields ) );
                emailMessage.CreateCommunicationRecord = false;
                emailMessage.Send();

                return true;
            }

            return false;
        }

        public static int? GetNearestCampus( double longitude, double latitude, double maxDistanceMeters )
        {
            List<CampusCache> campusCacheList = CampusCache.All( false );

            // assume we're too far away from any campuses
            double closestDistance = maxDistanceMeters;
            int? closestCampusId = null;

            // take the provided long/lat and get a geoPoint out of it
            DbGeography geoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", longitude, latitude ) );

            // now go thru each campus
            foreach ( CampusCache campusCache in campusCacheList )
            {
                // if the campus has a geopoint defined
                if ( campusCache.Location.Longitude.HasValue && campusCache.Location.Latitude.HasValue )
                {
                    // put it in a geoPoint
                    DbGeography campusGeoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", campusCache.Location.Longitude, campusCache.Location.Latitude ) );

                    // take the distance between the the provided point and this campus
                    double? distanceFromCampus = campusGeoPoint.Distance( geoPoint );

                    // if a calcluation could be performed and it's closer than what we've already found, take it.
                    if ( distanceFromCampus.HasValue && distanceFromCampus < closestDistance )
                    {
                        closestDistance = distanceFromCampus.Value;
                        closestCampusId = campusCache.Id;
                    }
                }
            }

            // return whatever the closest campus was (or null if none were close enough)
            return closestCampusId;
        }

        // This is the Id for the attendance group created in the Check-In system within Rock. It should never change.
        //const int Attendance_GroupId_CCVMobileAttendance = 2595385; //Production Value - Can't use in MA3 until we refresh the server.
        const int Attendance_GroupId_CCVMobileAttendance = 2588359; //Temp MA3 value - Remove once we refresh MA3.

        public static bool HasAttendanceRecord( PersonAlias personAlias )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );

            var dt = DateTime.Now;
            dt = TimeZoneInfo.ConvertTime( dt, RockDateTime.OrgTimeZoneInfo );

            return Common.Util.HasAttendanceRecord( personAlias.PersonId, Attendance_GroupId_CCVMobileAttendance, dt, attendanceService, rockContext );
        }

        public static bool SaveAttendanceRecord( PersonAlias personAlias, int? campusId, string host, string userAgent )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            PersonAliasService paService = new PersonAliasService( rockContext );

            var dt = DateTime.Now;
            dt = TimeZoneInfo.ConvertTime( dt, RockDateTime.OrgTimeZoneInfo );

            // attempt to save the record. if we did, this will return true
            if ( Util.CreateAttendanceRecord( personAlias.PersonId, campusId, Attendance_GroupId_CCVMobileAttendance, dt, attendanceService, paService, rockContext ) )
            {
                // log the interaction
                AddMobileAppAttendanceInteraction( personAlias, dt, host, userAgent, rockContext );

                rockContext.SaveChanges();

                return true;
            }
            // if a record already existed, false will be returned
            else
            {
                return false;
            }
        }

        private static void AddMobileAppAttendanceInteraction( PersonAlias personAlias, DateTime interactionDateTime, string host, string userAgent, RockContext rockContext )
        {
            // Interactions work as follows:
            // 1. Create an Interaction Medium (or pick an existing one) in the Defined Type here: https://rock.ccv.church/page/119?definedTypeId=510
            // 2. In SQL, create an Interaction Channel (ensure you set UsesSession to 1) that is bound to the Medium in Step 1.
            // 3. In SQL, create an Interaction Component that is bound to the Interaction Channel in Step 2.
            // 4. Make note of the InteractionComponent Id in Step 3.

            // To use:
            // The relationship is as follows:
            // Interaction points to an InteractionComponent (created above) and an InteractionSession that's created with each Interaction.
            // The InteractionSession points to an InteractionDeviceType, which can either be an existing one, or new, depending on whether an appropriate DeviceType can be found.
            // Typically each new Interaction use case (like this Mobile App Attendance Interaction) defines its own format for the InteractionDeviceType, so they'll be unique within the scope of
            // the interaction use case.
            //
            // What follows is a typical example of recording one.
            // 1. Check for a useable InteractionDeviceType. If one isn't found, create it.
            // 2. Create the InteractionSession and give it the InteractionDeviceType. This will always happen.
            // 3. Create the Interaction and give it the InteractionSession and InteractionComponent. (The InteractionComponent Id should be hardcoded)


            // Hardcode constant values we use for Mobile App attendance interaction. 
            // There isn't a place in Rock that centralizes this stuff, so this is as close to centralized as is possible.
            const string InteractionDeviceType_Name = "Mobile App";
            const string InteractionDeviceType_ClientType = "Mobile";
            const string InteractionDeviceType_Application = "CCV Mobile App";

            const string InteractionSession_SessionData = "Mobile App Attendance";

            const string Interaction_Operation = "Attend";

            //const int InteractionComponent_Id = 156245; //Production Value - Can't use in MA3 until we refresh the server
            const int InteractionComponent_Id = 151588; //Temp MA3 value - Remove once we refresh MA3.

            // first, see if there's already a device type matching this requester - We store device types 
            // with our defined InteractionDeviceTypeData above, and the userAgent string. That's it.
            InteractionDeviceTypeService interactionDeviceTypeService = new InteractionDeviceTypeService( rockContext );
            var interactionDeviceType = interactionDeviceTypeService.Queryable()
                                                                    .Where( a =>
                                                                            a.Name == InteractionDeviceType_Name &&
                                                                            a.ClientType == InteractionDeviceType_ClientType &&
                                                                            a.DeviceTypeData == userAgent &&
                                                                            a.Application == InteractionDeviceType_Application &&
                                                                            a.OperatingSystem == null )
                                                                    .FirstOrDefault();

            // if we didn't get one back, create one.
            if ( interactionDeviceType == null )
            {
                interactionDeviceType = new InteractionDeviceType
                {
                    Name = InteractionDeviceType_Name,
                    ClientType = InteractionDeviceType_ClientType,
                    DeviceTypeData = userAgent,
                    Application = InteractionDeviceType_Application,
                    CreatedByPersonAliasId = personAlias.Id
                };

                interactionDeviceTypeService.Add( interactionDeviceType );
            }


            // now create the interaction session
            InteractionSessionService interactionSessionService = new InteractionSessionService( rockContext );
            InteractionSession interactionSession = new InteractionSession
            {
                SessionData = InteractionSession_SessionData,
                DeviceTypeId = interactionDeviceType.Id,
                IpAddress = host,
                CreatedByPersonAliasId = personAlias.Id
            };
            interactionSessionService.Add( interactionSession );


            // finally, add the interaction itself
            InteractionService interactionService = new InteractionService( rockContext );
            Interaction thisInteraction = new Interaction()
            {
                Operation = Interaction_Operation,
                InteractionDateTime = interactionDateTime,
                PersonAliasId = personAlias.Id,
                InteractionComponentId = InteractionComponent_Id,
                InteractionSessionId = interactionSession.Id
            };
            interactionService.Add( thisInteraction );
        }

        public static List<MobileAppGroupModel> GetMobileAppGroups( string nameKeyword,
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
            List<MobileAppGroupModel> groupResultList = new List<MobileAppGroupModel>();

            // now take only what we need from each group (drops our return package to about 2kb, from 40kb)
            foreach ( Group group in groupList )
            {
                MobileAppGroupModel groupResult = GetMobileAppGroup( group );

                if ( groupResult != null )
                {
                    groupResultList.Add( groupResult );
                }
            }

            return groupResultList;
        }

        public static MobileAppGroupModel GetMobileAppGroup( Group group )
        {
            RockContext rockContext = new RockContext();

            var datamartPersonService = new DatamartPersonService( rockContext ).Queryable().AsNoTracking();
            var personService = new PersonService( rockContext ).Queryable().AsNoTracking();
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // now get the group leader. If there isn't one, we'll fail, because we don't want a group with no leader
            GroupMember leader = group.Members.Where( gm => GroupRoleId_NeighborhoodGroupCoach == gm.GroupRole.Id ).SingleOrDefault();
            if ( leader != null )
            {
                // we are guaranteed that there will be a location object due to our initial query
                Location locationObj = group.GroupLocations.First().Location;

                MobileAppGroupModel groupResult = new MobileAppGroupModel()
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

                // now find the leader in our datamart so that we can see who their Associate Pastor / Neighborhood Leader is
                var datamartPerson = datamartPersonService.Where( dp => dp.PersonId == leader.Person.Id ).SingleOrDefault();
                if ( datamartPerson != null )
                {
                    groupResult.CoachName = leader.Person.NickName + " " + leader.Person.LastName;

                    if ( leader.Person.PhotoId.HasValue )
                    {
                        groupResult.CoachPhotoURL = publicAppRoot + "GetImage.ashx?Id=" + leader.Person.PhotoId.Value;
                    }
                    else
                    {
                        groupResult.CoachPhotoURL = string.Empty;
                    }


                    // if the leader has a neighborhood pastor (now called associate pastor) defined, take their values.
                    if ( datamartPerson.NeighborhoodPastorId.HasValue )
                    {
                        // get the AP, but guard against a null value (could happen if the current ID is merged and the datamart hasn't re-run)
                        Person associatePastor = personService.Where( p => p.Id == datamartPerson.NeighborhoodPastorId.Value ).SingleOrDefault();
                        if ( associatePastor != null )
                        {
                            groupResult.AssociatePastorName = associatePastor.NickName + " " + associatePastor.LastName;

                            if ( associatePastor.PhotoId.HasValue )
                            {
                                groupResult.AssociatePastorPhotoURL = publicAppRoot + "GetImage.ashx?Id=" + associatePastor.PhotoId.Value;
                            }
                            else
                            {
                                groupResult.AssociatePastorPhotoURL = string.Empty;
                            }
                        }
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
                    groupResult.PhotoURL = publicAppRoot + "GetImage.ashx?Guid=" + photoGuid;
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

            return null;
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

        public static MobileAppSeriesModel PodcastSeriesToMobileAppSeries( PodcastUtil.PodcastSeries series )
        {
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            MobileAppSeriesModel maSeriesModel = new MobileAppSeriesModel();

            maSeriesModel.Name = series.Name;
            maSeriesModel.Description = series.Description;

            // parse and setup the date range for the series
            string dateRangeStr = string.Empty;
            series.Attributes.TryGetValue( "DateRange", out dateRangeStr );
            if ( string.IsNullOrWhiteSpace( dateRangeStr ) == false )
            {
                string[] dateRanges = dateRangeStr.Split( ',' );
                string startDate = DateTime.Parse( dateRanges[0] ).ToShortDateString();
                string endDate = DateTime.Parse( dateRanges[1] ).ToShortDateString();

                maSeriesModel.DateRange = startDate + " - " + endDate;
            }

            // set the images
            if ( string.IsNullOrWhiteSpace( series.Attributes["Image_16_9"] ) == false )
            {
                maSeriesModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_16_9"];
            }

            if ( string.IsNullOrWhiteSpace( series.Attributes["Image_1_1"] ) == false )
            {
                maSeriesModel.ThumbnailURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_1_1"];
            }

            // after we process messages, we'll use this value for deciding whether the Series is hidden.
            bool allMessagesHidden = true;

            // Now generate each message of the series
            maSeriesModel.Messages = new List<MobileAppMessageModel>();
            foreach ( PodcastUtil.PodcastMessage message in series.Messages )
            {
                MobileAppMessageModel maMessageModel = new MobileAppMessageModel();

                // (because this is a new attrib, and not all messages have it, check for null. default to TRUE since that's what thye'll want the majority of the time.)
                // This is totally confusing, but the KEY is called "Active", however, its name in Rock is "Approved". So confusing. My mistake. Ugh.
                bool messageActive = true;
                if ( message.Attributes.ContainsKey( "Active" ) )
                {
                    messageActive = bool.Parse( message.Attributes["Active"] );
                }

                // if the message doesn't start yet, or hasn't been approved, set it to private.
                if ( message.Date > RockDateTime.Now || messageActive == false )
                {
                    maMessageModel.Hidden = true;
                }
                else
                {
                    // this message is _NOT_ hidden, therefore we can set allMessagesHidden to false
                    allMessagesHidden = false;
                }

                maMessageModel.Name = message.Name;
                maMessageModel.Speaker = message.Attributes["Speaker"];
                maMessageModel.Date = message.Date.Value.ToShortDateString();
                maMessageModel.ImageURL = maSeriesModel.ImageURL;
                maMessageModel.ThumbnailURL = maSeriesModel.ThumbnailURL;

                string noteUrlValue = message.Attributes["NoteUrl"];
                if ( string.IsNullOrWhiteSpace( noteUrlValue ) == false )
                {
                    maMessageModel.NoteURL = noteUrlValue;
                }

                string watchUrlValue = message.Attributes["WatchUrl"];
                if ( string.IsNullOrWhiteSpace( watchUrlValue ) == false )
                {
                    maMessageModel.VideoURL = watchUrlValue;
                }

                string shareUrlValue = message.Attributes["ShareUrl"];
                if ( string.IsNullOrWhiteSpace( shareUrlValue ) == false )
                {
                    maMessageModel.ShareURL = shareUrlValue;
                }

                string discussionGuideUrlValue = null;
                message.Attributes.TryGetValue( "DiscussionGuideUrl", out discussionGuideUrlValue );
                if ( string.IsNullOrWhiteSpace( discussionGuideUrlValue ) == false )
                {
                    maMessageModel.DiscussionGuideURL = discussionGuideUrlValue;
                }

                maSeriesModel.Messages.Add( maMessageModel );
            }

            // Finally, let's see if the series should be flagged as Hidden.
            // It should be hidden if all messages are hidden OR the seriesActive flag is false
            bool isSeriesActive = series.Attributes["Active"] == "True" ? true : false;

            if ( allMessagesHidden || isSeriesActive == false )
            {
                maSeriesModel.Hidden = true;
            }

            return maSeriesModel;
        }

        internal static KidsContentModel BuildKidsContent( Person person )
        {
            // first, we need to know what grade range we'll be getting content for
            const string GradeRange_EK = "Early Kids";
            const string GradeRange_LK = "Later Kids";
            const string GradeRange_JH = "Junior High";
            const string GradeRange_HS = "High School";

            // this is technically cheating, but Rock abstracts grade and doesn't natively
            // know about the US standard. To simplify things, let's do the conversion here
            int realGrade = 1; //(assume 1st grade)
            if ( person.GradeOffset.HasValue )
            {
                realGrade = 12 - person.GradeOffset.Value;
            }
            else
            {
                // before we completely assume 1st grade, see if we can use their age
                if ( person.Age.HasValue )
                {
                    if ( person.Age >= 14 )
                    {
                        realGrade = 9;
                    }
                    else if ( person.Age >= 10 )
                    {
                        realGrade = 6;
                    }
                    else
                    {
                        realGrade = 1;
                    }
                }
            }

            // now see which grade level they're in
            string targetGradeRange = string.Empty;
            if ( realGrade >= 9 )
            {
                targetGradeRange = GradeRange_HS;
            }
            else if ( realGrade >= 7 )
            {
                targetGradeRange = GradeRange_JH;
            }
            else if ( realGrade >= 5 )
            {
                targetGradeRange = GradeRange_LK;
            }
            else
            {
                targetGradeRange = GradeRange_EK;
            }
            
            // now that we know the range, build the content channel queries
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            // first, get AtCCV
            const int ContentChannelId_AtCCV = 285;
            ContentChannel atCCV = contentChannelService.Get( ContentChannelId_AtCCV );

            // sort by date
            var atCCVItems = atCCV.Items.OrderByDescending( i => i.StartDateTime );

            // now take the first one that matches our grade offset.

            // while iterating over these in memory could become slow as the list grows, the business
            // requirements of CCV mean it won't. Because there will always be a new entry each week for each grade level,
            // it won't ever realistically go over the first 4 items
            ContentChannelItem atCCVItem = null;
            foreach ( var item in atCCVItems )
            {
                // this is the slow part. If it ever does become an issue, replace it with an AV table join.
                item.LoadAttributes();
                if ( item.AttributeValues["GradeLevel"].ToString() == targetGradeRange )
                {
                    atCCVItem = item;
                    break;
                }
            }


            // next, get Faith Building At Home
            const int ContentChannelId_FaithBuilding = 286;
            ContentChannel faithBuilding = contentChannelService.Get( ContentChannelId_FaithBuilding );

            // sort by date
            var faithBuildingItems = faithBuilding.Items.OrderByDescending( i => i.StartDateTime );

            // as above, we'll iterate over the whole list in memory, knowing we'll actually only load attributes for about 4 items.
            ContentChannelItem faithBuildingItem = null;
            foreach ( var item in faithBuildingItems )
            {
                item.LoadAttributes();
                if ( item.AttributeValues["GradeLevel"].ToString() == targetGradeRange )
                {
                    faithBuildingItem = item;
                    break;
                }
            }


            // finally, get the resources available for the grade level
            const int ContentChannelId_Resources = 287;
            ContentChannel resourceChannel = contentChannelService.Get( ContentChannelId_Resources );

            List<ContentChannelItem> resourceList = new List<ContentChannelItem>();
            foreach ( var item in resourceChannel.Items )
            {
                item.LoadAttributes();
                if ( item.AttributeValues["GradeLevel"].ToString().Contains( targetGradeRange ) )
                {
                    resourceList.Add( item );
                }
            }

            
            // prepare our model - we'll require both main category items 
            // and otherwise return failure (note that resources CAN be empty)
            if ( atCCVItem != null && faithBuildingItem != null )
            {
                string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

                KidsContentModel contentModel = new KidsContentModel();

                // At CCV
                contentModel.AtCCV_Title = atCCVItem.Title;
                contentModel.AtCCV_Date = atCCVItem.StartDateTime;
                contentModel.AtCCV_Content = atCCVItem.Content;
                contentModel.AtCCV_Date = atCCVItem.StartDateTime;
                contentModel.AtCCV_DiscussionTopic_One = atCCVItem.AttributeValues["DiscussionTopic1"].ToString();
                contentModel.AtCCV_DiscussionTopic_Two = atCCVItem.AttributeValues["DiscussionTopic2"].ToString();

                string seriesImageGuid = atCCVItem.AttributeValues["SeriesImage"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( seriesImageGuid ) == false )
                {
                    contentModel.AtCCV_ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + seriesImageGuid;
                }
                else
                {
                    contentModel.AtCCV_ImageURL = string.Empty;
                }

                // Faith building
                contentModel.FaithBuilding_Title = faithBuildingItem.Title;
                contentModel.FaithBuilding_Content = faithBuildingItem.Content;

                // resources CAN be empty, so just take whatever's available
                contentModel.Resources = new List<KidsResourceModel>();

                foreach ( var resourceItem in resourceList )
                {
                    KidsResourceModel resModel = new KidsResourceModel
                    {
                        Title = resourceItem.Title,
                        Subtitle = resourceItem.AttributeValues["Subtitle"].ToString(),
                        URL = resourceItem.AttributeValues["URL"].ToString()
                    };

                    contentModel.Resources.Add( resModel );
                }

                return contentModel;
            }
            else
            {
                return null;
            }
        }
    }
}
