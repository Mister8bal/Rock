using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using church.ccv.PersonalizationEngine.Model;
using church.ccv.PersonalizationEngine.Data;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;
using Newtonsoft.Json.Linq;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Datamart.Model;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum SearchGroupsResponse
        {
            NotSet = -1,
            Success,
            
            AddressLookupFailed, //The address couldn't be resolved to a location object within Rock

            LocationNotGeocoded, //Means a location was found with the address provided, but it isn't geocoded (and needs to be)
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/SearchGroups" )]
        [Authenticate, Secured]
        public HttpResponseMessage SearchGroups( string nameKeyword = "", string descriptionKeyword = "", string street = "", string city = "", string state = "", string zip = "", int? skip = null, int top = 10 )
        {
            const int GroupTypeId_NeighborhoodGroup = 49;
            const int GroupRoleId_NeighborhoodGroupCoach = 50;

            // The id for the group description on Neighborhood groups. Used for joining the attributeValue if a descriptionKeyword is provided.
            const int AttributeId_GroupDescription = 13055;

            const string GroupDescription_Key = "GroupDescription";
            const string ChildcareDescription_Key = "Childcare";
            const string FamilyPicture_Key = "FamilyPicture";

            const string GroupFilters_Key = "GroupFilters";
            const string ChildcareProvided_FilterKey = "Childcare Provided";

            RockContext rockContext = new RockContext();

            // this location is the one derived from the provided address, and if valid, will simply be used
            // to sort groups by distance from this location
            Location locationForDistance = null;

            // first see if there's a location to use
            if ( string.IsNullOrWhiteSpace( street ) == false &&
                 string.IsNullOrWhiteSpace( city ) == false &&
                 string.IsNullOrWhiteSpace( state ) == false &&
                 string.IsNullOrWhiteSpace( zip ) == false )
            {
                string street2 = string.Empty;
                string country = GlobalAttributesCache.Read().OrganizationCountry;

                // take the address provided and get a location object from it
                locationForDistance = new LocationService( rockContext ).Get( street, street2, city, state, zip, country );
                if ( locationForDistance == null )
                {
                    return Common.Util.GenerateResponse( false, SearchGroupsResponse.AddressLookupFailed.ToString(), null );
                }

                // use the location to find the groups nearby
                if ( locationForDistance.GeoPoint == null )
                {
                    return Common.Util.GenerateResponse( false, SearchGroupsResponse.LocationNotGeocoded.ToString(), null );
                }
            }

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
                var avQuery = new AttributeValueService( rockContext ).Queryable().AsNoTracking().Where( av => av.AttributeId == AttributeId_GroupDescription );
                var joinedQuery = groupList.Join( avQuery, g => g.Id, av => av.EntityId, ( g, av ) => new { Group = g, Desc = av.Value } );

                groupList = joinedQuery.Where( g => g.Desc.ToLower().Contains( descriptionKeyword ) ).Select( g => g.Group );
            }

            // pull it into memory, because we have to in order to store distances on the location and sort by that
            // (This could be optimized by creating a lookup table and sending that to sql)
            groupList = groupList.ToList();

            // calculate the distance of each of the groups locations from the specified geoFence
            if ( locationForDistance != null )
            {
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
            }

            // sort by distance
            groupList = groupList.OrderBy( a => a.GroupLocations.FirstOrDefault() != null ? a.GroupLocations.FirstOrDefault().Location.Distance : int.MaxValue );

            // grab the nth set
            if ( skip.HasValue )
            {
                groupList = groupList.Skip( skip.Value );
            }

            groupList = groupList.Take( top ).ToList();

            var datamartPersonService = new DatamartPersonService( rockContext ).Queryable().AsNoTracking();
            var personService = new PersonService( rockContext ).Queryable().AsNoTracking();
            var binaryFileService = new BinaryFileService( rockContext ).Queryable().AsNoTracking();

            List<GroupResult> groupResultList = new List<GroupResult>();

            // now take only what we need from each group (drops our return package to about 2kb, from 40kb)
            foreach ( Group group in groupList )
            {
                // we are guaranteed that there will be a location object due to our initial query
                Location locationObj = group.GroupLocations.First().Location;

                // now get the group leader. If there isn't one, we'll fail, because we don't want a group with no leader
                GroupMember leader = group.Members.Where( gm => GroupRoleId_NeighborhoodGroupCoach == gm.GroupRole.Id ).SingleOrDefault();
                if ( leader != null )
                {
                    GroupResult groupResult = new GroupResult()
                    {
                        Id = group.Id,

                        Name = group.Name,
                        Description = group.Description,

                        Longitude = locationObj.Longitude.Value,
                        Latitude = locationObj.Latitude.Value,
                        DistanceFromSource = locationObj.Distance,

                        MeetingTime = group.Schedule != null ? group.Schedule.FriendlyScheduleText : "",

                        Street = locationObj.Street1,
                        City = locationObj.City,
                        State = locationObj.State,
                        Zip = locationObj.PostalCode.Substring( 0, locationObj.PostalCode.IndexOf( '-' ) ) //strip off the stupid postal code
                    };

                    // now find the leader in our datamart so that we can see who their Associate Pastor / Neighborhood Leader is
                    var datamartPerson = datamartPersonService.Where( dp => dp.PersonId == leader.Person.Id ).SingleOrDefault();
                    if ( datamartPerson != null )
                    {
                        groupResult.CoachName = leader.Person.NickName + " " + leader.Person.LastName;
                        groupResult.CoachPhotoId = leader.Person.PhotoId;

                        // if the leader has a neighborhood pastor (now called associate pastor) defined, take their values.
                        if ( datamartPerson.NeighborhoodPastorId.HasValue )
                        {
                            // get the AP, but guard against a null value (could happen if the current ID is merged and the datamart hasn't re-run)
                            Person associatePastor = personService.Where( p => p.Id == datamartPerson.NeighborhoodPastorId.Value ).SingleOrDefault();
                            if ( associatePastor != null )
                            {
                                groupResult.AssociatePastorName = associatePastor.NickName + " " + associatePastor.LastName;
                                groupResult.AssociatePastorPhotoId = associatePastor.PhotoId;
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
                        Guid photoGuid = group.AttributeValues[FamilyPicture_Key].Value.AsGuid();

                        // get the id so that we send consistent data down to the mobile app
                        var photoObj = binaryFileService.Where( f => f.Guid == photoGuid ).SingleOrDefault();
                        if ( photoObj != null )
                        {
                            groupResult.PhotoId = photoObj.Id;
                        }
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

                    // get the childcare description whether the Childcare filter is set or NOT. This is
                    // because some groups (like mine!) explain that they'd be willing to start childcare if the group grew.
                    if ( group.AttributeValues.ContainsKey( ChildcareDescription_Key ) )
                    {
                        groupResult.ChildcareDesc = group.AttributeValues[ChildcareDescription_Key].Value;
                    }

                    groupResultList.Add( groupResult );
                }
            }

            return Common.Util.GenerateResponse( true, SearchGroupsResponse.Success.ToString(), groupResultList );
        }
    }
}
