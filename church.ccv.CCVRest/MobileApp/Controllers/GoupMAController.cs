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
        public HttpResponseMessage SearchGroups( string nameKeyword = "", 
                                                 string descriptionKeyword = "", 
                                                 string street = "", 
                                                 string city = "", 
                                                 string state = "", 
                                                 string zip = "", 
                                                 bool? requiresChildcare = false, 
                                                 int? skip = null, 
                                                 int top = 10 )
        {
            // see if there's a location to use for sorting groups by distance
            Location locationForDistance = null;

            if ( string.IsNullOrWhiteSpace( street ) == false &&
                 string.IsNullOrWhiteSpace( city ) == false &&
                 string.IsNullOrWhiteSpace( state ) == false &&
                 string.IsNullOrWhiteSpace( zip ) == false )
            {
                // take the address provided and get a location object from it
                RockContext rockContext = new RockContext( );
                locationForDistance = new LocationService( rockContext ).Get( street, string.Empty, city, state, zip, GlobalAttributesCache.Read().OrganizationCountry );
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

            List<MobileAppGroupModel> groupResults = MobileAppService.GetMobileAppGroups( nameKeyword, descriptionKeyword, locationForDistance, requiresChildcare, skip, top );

            return Common.Util.GenerateResponse( true, SearchGroupsResponse.Success.ToString(), groupResults );
        }

        [Serializable]
        public enum GroupResponse
        {
            NotSet = -1,

            Success,

            NotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Group" )]
        [Authenticate, Secured]
        public HttpResponseMessage Group( int groupId )
        {
            // find the Rock group, and then we'll get a mobile app group from that
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            Group group = groupService.Get( groupId );

            if ( group != null )
            {
                MobileAppGroupModel mobileAppGroup = MobileAppService.GetMobileAppGroup( group );

                return Common.Util.GenerateResponse( true, GroupResponse.Success.ToString(), mobileAppGroup );
            }
            else
            {
                return Common.Util.GenerateResponse( false, GroupResponse.NotFound.ToString(), null );
            }
        }
    }
}
