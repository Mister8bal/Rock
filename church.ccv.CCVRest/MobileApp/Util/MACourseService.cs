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
    class MACourseService
    {
        // The "Courses" as the Mobile App calls them, are Life Training Groups in Rock.
        const int GroupTypeId_LifeTrainingGroup = 117;
        const int GroupRoleId_LifeTrainingCourseLeader = 170;

        const string Course_Description_Key = "CourseDescription";
        const string Course_RegStartDate_Key = "RegistrationStartDate";
        const string Course_RegEndDate_Key = "RegistrationEndDate";
        const string Course_Topic_Key = "CourseTopic";
       
        public static List<MACourseModel> GetMobileAppCourses( string nameKeyword,
                                                              string descriptionKeyword,
                                                              Location locationForDistance,
                                                              int? skip,
                                                              int top )
        {
            // Gets Life Training Groups, searches by the provided arguments, and returns matching values as MobileAppCourseModels

            // First get all life training groups, filtered by name and description if the caller provided those keywords
            RockContext rockContext = new RockContext();

            // get all groups of this group type that are public, and have a long/lat we can use
            GroupService groupService = new GroupService( rockContext );
            IEnumerable<Group> groupList = groupService.Queryable( "Schedule,GroupLocations.Location" ).AsNoTracking()
                                                       .Where( a => a.GroupTypeId == GroupTypeId_LifeTrainingGroup && a.IsPublic == true )
                                                       .Include( a => a.GroupLocations ).Where( a => a.GroupLocations.Any( x => x.Location.GeoPoint != null ) );
            
            // if they provided name keywords, filter by those
            if ( string.IsNullOrWhiteSpace( nameKeyword ) == false )
            {
                groupList = groupList.Where( a => a.Name.ToLower().Contains( nameKeyword.ToLower() ) );
            }

            // if they provided description, we need to join the attribute value table
            if ( string.IsNullOrWhiteSpace( descriptionKeyword ) == false )
            {
                // The id for the course description on Life Training groups. Used for joining the attributeValue if a descriptionKeyword is provided.
                const int AttributeId_Description = 69073;

                // Join the attribute value that defines the CourseDescription with the group
                var avQuery = new AttributeValueService( rockContext ).Queryable().AsNoTracking().Where( av => av.AttributeId == AttributeId_Description );
                var joinedQuery = groupList.Join( avQuery, g => g.Id, av => av.EntityId, ( g, av ) => new { Group = g, GroupDesc = av.Value } );

                // see if the GroupDescription attribute value has the description keyword in it
                groupList = joinedQuery.Where( g => g.GroupDesc.ToLower().Contains( descriptionKeyword.ToLower() ) ).Select( g => g.Group );
            }
            
            // now we need to keep only those with open registration. Unfortunately we have to pull to memory for that.
            List<Group> filteredGroupList = new List<Group>();

            groupList = groupList.ToList();

            foreach ( var group in groupList )
            {
                group.LoadAttributes();
                
                if ( group.AttributeValues.ContainsKey( Course_RegStartDate_Key ) && group.AttributeValues.ContainsKey( Course_RegEndDate_Key ) )
                {
                    // grab the values
                    string registrationStart = group.GetAttributeValue( Course_RegStartDate_Key );
                    string registrationEnd = group.GetAttributeValue( Course_RegEndDate_Key );

                    if ( !string.IsNullOrWhiteSpace( registrationStart ) && !string.IsNullOrWhiteSpace( registrationEnd ) )
                    {
                        // convert them
                        DateTime registrationStartDate = DateTime.Parse( registrationStart );
                        DateTime registrationEndDate = DateTime.Parse( registrationEnd );

                        var today = RockDateTime.Now.Date;

                        // If RegistrationStartDate is before today's date & RegistrationEndDate is before today's date
                        if ( registrationStartDate.Date <= today && registrationEndDate.Date >= today )
                        {
                            filteredGroupList.Add( group );
                        }
                    }
                }
            }

            // calculate the distance of each of the group's locations from the specified geoFence
            if ( locationForDistance != null )
            {
                foreach ( var group in filteredGroupList )
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
                filteredGroupList = filteredGroupList.OrderBy( a => a.GroupLocations.First().Location.Distance ).ToList();
            }


            // grab the nth set
            if ( skip.HasValue )
            {
                filteredGroupList = filteredGroupList.Skip( skip.Value ).ToList();
            }

            // and take the top amount
            filteredGroupList = filteredGroupList.Take( top ).ToList();


            // Now package the groups into GroupResult objects that store what the Mobile App cares about
            List<MACourseModel> maCourseResultList = new List<MACourseModel>();

            // now take only what we need from each group
            foreach ( Group group in filteredGroupList )
            {
                MACourseModel maCourseResult = GetMobileAppCourse( group );

                if ( maCourseResult != null )
                {
                    maCourseResultList.Add( maCourseResult );
                }
            }

            return maCourseResultList;
        }
        
        public static MACourseModel GetMobileAppCourse( Group group )
        {
            RockContext rockContext = new RockContext();
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // now get the group leader. If there isn't one, we'll fail, because we don't want a group with no leader
            GroupMember leader = group.Members.Where( gm => GroupRoleId_LifeTrainingCourseLeader == gm.GroupRole.Id ).FirstOrDefault();
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

            // build the mobile app model
            MACourseModel courseResult = new MACourseModel()
            {
                Id = group.Id,
                SubTitle = group.Name,
                MeetingTime = group.Schedule != null ? group.Schedule.FriendlyScheduleText : ""
            };

            // try to set the location into
            var groupLoc = group.GroupLocations.FirstOrDefault();
            if ( groupLoc != null )
            {
                courseResult.Longitude = groupLoc.Location.Longitude.Value;
                courseResult.Latitude = groupLoc.Location.Latitude.Value;
                courseResult.DistanceFromSource = groupLoc.Location.Distance;

                courseResult.Street = groupLoc.Location.Street1;
                courseResult.City = groupLoc.Location.City;
                courseResult.State = groupLoc.Location.State;
                courseResult.Zip = groupLoc.Location.PostalCode;
            }

            // set the leader info
            courseResult.CourseLeader = new MACourseMemberModel
            {
                Name = leader.Person.NickName + " " + leader.Person.LastName,
                ThumbnailPhotoURL = leader.Person.PhotoId.HasValue ? publicAppRoot + "GetImage.ashx?Id=" + leader.Person.PhotoId.Value + "&width=180" : ""
            };

            // if the leader has a neighborhood pastor (now called associate pastor) defined, grab their person object. (This is allowed to be null)
            if ( datamartPerson.NeighborhoodPastorId.HasValue )
            {
                // get the AP, but guard against a null value (could happen if the current ID is merged and the datamart hasn't re-run)
                var associatePastor = new PersonService( rockContext ).Queryable().AsNoTracking()
                                                                      .Where( p => p.Id == datamartPerson.NeighborhoodPastorId.Value )
                                                                      .SingleOrDefault();


                courseResult.AssociatePastor = new MACourseMemberModel
                {
                    Name = associatePastor.NickName + " " + associatePastor.LastName,
                    ThumbnailPhotoURL = associatePastor.PhotoId.HasValue ? publicAppRoot + "GetImage.ashx?Id=" + associatePastor.PhotoId.Value + "&width=180" : ""
                };
            };
            
            // Finally, load attributes so we can set additional group info
            group.LoadAttributes();

            // Check Group Capacity
            courseResult.IsFull = IsCourseFull( group );

            if ( group.AttributeValues.ContainsKey( Course_Description_Key ) )
            {
                courseResult.Description = group.AttributeValues[Course_Description_Key].Value;
            }

            // the title of the course will be its course topic
            if ( group.AttributeValues.ContainsKey( Course_Topic_Key ) )
            {
                string courseTopic = group.AttributeValues[Course_Topic_Key].Value;
                courseResult.Title = courseTopic + " Course";
                
                // Get the picture based on the life training topic.
                // Not great, but we have no other way of associating a picture with a topic.
                // Alternatives would be a defined type with an image attribute, but then we'd have to make a custom block
                // to get the website to read it (or a custom API)
                string topicImageURL = string.Empty;
                switch ( courseTopic )
                {
                    case "Marriage":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-marriage.jpg";
                        break;
                    case "Divorce":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-marriage.jpg";
                        break;
                    case "Finance":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-finance.jpg";
                        break;
                    case "Parenting":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-parenting.jpg";
                        break;
                    case "Blended Families":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-parenting.jpg";
                        break;
                    case "Addiction":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-addiction.jpg";
                        break;
                    case "Grief":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-pain-and-grief.jpg";
                        break;
                    case "Abuse":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-trauma-and-abuse.jpg";
                        break;
                    case "Pornography":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-sex-and-sexuality.jpg";
                        break;
                    case "Pornography Spouse Support":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-sex-and-sexuality.jpg";
                        break;
                    case "Pain & Suffering":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-pain-and-grief.jpg";
                        break;
                    case "Premarital":
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-premarital.jpg";
                        break;

                    default:
                        topicImageURL = "/Themes/church_ccv_External_v8/Assets/Images/home/get-involved/feature-life-training-marriage.jpg";
                        break;
                }

                courseResult.PhotoURL = publicAppRoot + topicImageURL;
                courseResult.ThumbnailPhotoURL = publicAppRoot + topicImageURL;
            }

            return courseResult;
        }

        private static bool IsCourseFull( Group g )
    {
        if ( g.GroupCapacity == null || g.Members.Count() < g.GroupCapacity )
        {
            return false;
        }

        return true;
    }
    }
}
