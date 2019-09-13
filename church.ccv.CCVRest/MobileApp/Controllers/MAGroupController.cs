using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Podcast;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum SearchGroupsResponse
        {
            NotSet = -1,

            Success
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
                Location foundLocation = new LocationService( rockContext ).Get( street, string.Empty, city, state, zip, GlobalAttributesCache.Read().OrganizationCountry );

                // if we found a location and it's geo-coded, we'll use it to sort groups by distance from it
                if ( foundLocation != null && foundLocation.GeoPoint != null )
                {
                    locationForDistance = foundLocation;
                }
            }

            List<MAGroupModel> groupResults = MAGroupService.GetMobileAppGroups( nameKeyword, descriptionKeyword, locationForDistance, requiresChildcare, skip, top );

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
            return InternalGroup( groupId, MAGroupService.MAGroupMemberView.NonMemberView);
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Group/MemberView" )]
        [Authenticate, Secured]
        public HttpResponseMessage GroupMemberView( int groupId )
        {
            return InternalGroup( groupId, MAGroupService.MAGroupMemberView.MemberView );
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Group/CoachView" )]
        [Authenticate, Secured]
        public HttpResponseMessage GroupCoachView( int groupId )
        {
            return InternalGroup( groupId, MAGroupService.MAGroupMemberView.CoachView );
        }

        private HttpResponseMessage InternalGroup( int groupId, MAGroupService.MAGroupMemberView memberView )
        {
            // find the Rock group, and then we'll get a mobile app group from that
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            Group group = groupService.Get( groupId );

            if ( group != null )
            {
                MAGroupModel mobileAppGroup = MAGroupService.GetMobileAppGroup( group, memberView );

                return Common.Util.GenerateResponse( true, GroupResponse.Success.ToString(), mobileAppGroup );
            }
            else
            {
                return Common.Util.GenerateResponse( false, GroupResponse.NotFound.ToString(), null );
            }
        }

        [Serializable]
        public enum JoinGroupResponse
        {
            NotSet = -1,

            Success,

            Success_SecurityIssue, //The user was processed, but needs a review by security before they can join the group.

            AlreadyInGroup, //The user is already in this group

            GroupNotFound, //A group with the provided group id wasn't found

            InvalidModel,

            Failed
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/JoinGroup" )]
        [Authenticate, Secured]
        public HttpResponseMessage JoinGroup( [FromBody] JoinGroupModel joinGroupModel )
        {
            // make sure the model is valid
            if ( joinGroupModel == null ||
                string.IsNullOrWhiteSpace( joinGroupModel.FirstName ) == true ||
                string.IsNullOrWhiteSpace( joinGroupModel.LastName ) == true ||
                string.IsNullOrWhiteSpace( joinGroupModel.Email ) == true )
            {
                return Common.Util.GenerateResponse( false, JoinGroupResponse.InvalidModel.ToString(), null );
            }

            // try letting them join the group
            MAGroupService.RegisterPersonResult result = MAGroupService.RegisterPersonInGroup( joinGroupModel );
            switch ( result )
            {
                case MAGroupService.RegisterPersonResult.Success:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.Success.ToString(), null );

                case MAGroupService.RegisterPersonResult.SecurityIssue:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.Success_SecurityIssue.ToString(), null );

                case MAGroupService.RegisterPersonResult.GroupNotFound:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.GroupNotFound.ToString(), null );

                case MAGroupService.RegisterPersonResult.AlreadyInGroup:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.AlreadyInGroup.ToString(), null );

                default:
                    return Common.Util.GenerateResponse( false, JoinGroupResponse.Failed.ToString(), null );
            }
        }

        [Serializable]
        public enum ToolboxContentResponse
        {
            NotSet = -1,

            Success,

            ContentError
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/ToolboxContent" )]
        [Authenticate, Secured]
        public async Task<HttpResponseMessage> ToolboxContent( int primaryAliasId )
        {
            try
            {
                ToolboxContentModel toolboxContent = new ToolboxContentModel();
                toolboxContent.ResourceList = new List<ToolboxResourceModel>();

                // track the resources we need to get, so we don't go over the limit
                int resourcesRemaining = 4;

                // ultimately, we want 4 resources/messages. However, because some could be private, and we also need to know the index
                // of the messsage within its series (For the Week Number: N feature), we have to take whole series. 
                // Grab the most recent six, which should be more than enough to cover 4 messages.
                PodcastUtil.PodcastCategory rootCategory = PodcastUtil.GetPodcastsByCategory( 470, false, 6, int.MaxValue, primaryAliasId );
                if ( rootCategory == null )
                {
                    // if this failed something really bad happened
                    return Common.Util.GenerateResponse( false, ToolboxContentResponse.ContentError.ToString(), null );
                }

                // iterate over each series
                foreach ( PodcastUtil.IPodcastNode podcastNode in rootCategory.Children )
                {
                    // this is safe to cast to a series, because we ask for only Series by passing false to GetPodcastsByCategory                        
                    PodcastUtil.PodcastSeries series = podcastNode as PodcastUtil.PodcastSeries;

                    // use the resourcesRemaining int to track when we've hit our total number
                    List<ToolboxResourceModel> resourceList = await MAPodcastService.PodcastSeriesToToolboxResources( series, resourcesRemaining );
                    toolboxContent.ResourceList.AddRange( resourceList );

                    resourcesRemaining -= resourceList.Count;

                    // if parsing this latest series caused us to hit our goal, break.
                    if ( resourcesRemaining == 0 )
                    {
                        break;
                    }
                }

                // Grab the content for the Associate Pastor board (if there was an error and null was returned, that's fine)
                toolboxContent.APBoardContent = await MAGroupService.GetAPBoardContent( primaryAliasId );

                return Common.Util.GenerateResponse( true, ToolboxContentResponse.Success.ToString(), toolboxContent );
            }
            catch
            {
                return Common.Util.GenerateResponse( true, ToolboxContentResponse.ContentError.ToString(), null );
            }
        }
    }
}
