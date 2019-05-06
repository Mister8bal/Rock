using System;
using System.ComponentModel;
using System.ComponentModel.Composition;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace church.ccv.Badges.Info
{
    [Description( "Displays the Mission Status for the specific person." )]
    [Export( typeof( Rock.PersonProfile.BadgeComponent ) )]
    [ExportMetadata( "ComponentName", "Missions" )]

    [TextField( "Badge Color", "The color of the badge (#ffffff).", true, "#8b9064", "",  0)]
    class Missions : Rock.PersonProfile.BadgeComponent
    {
        public override void Render( PersonBadgeCache badge, System.Web.UI.HtmlTextWriter writer, Person person, PersonBlock parentPersonBlock )
        {
            string badgeColor = GetAttributeValue( badge, "BadgeColor" );

            if ( !string.IsNullOrEmpty( badgeColor ) )
            {
                writer.Write( string.Format(
                    @"<div class='badge badge-id-{0}' style='color: {1}' data-toggle='tooltip' data-container='body'> 
                        <i class='badge-icon badge-icon-id-{0} fa fa-globe badge-disabled'></i>
                    </div>",
                    badge.Id,
                    badgeColor.EscapeQuotes() ) );

                writer.Write( string.Format(
                    @"
                    <script>
                    Sys.Application.add_load(function () {{
                                                
                        $.ajax({{
                                type: 'GET',
                                url: Rock.settings.get('baseUrl') + 'api/CCV/Badges/TakenMissionTrip/{0}' ,
                                statusCode: {{
                                    200: function (data, status, xhr) {{
                                        var $badge = $('.badge.badge-id-{2}');
                                        
                                        if ( Array.isArray( data.MissionSummaries ) && data.MissionSummaries.length ) {{
                                            
                                            $badge.find( '.badge-icon-id-{2}' ).attr('style', 'opacity: .5' );
                                            $badge.find( '.badge-icon-id-{2}' ).removeClass( 'badge-disabled' );

                                            var popoverContent = '{1} has been on these Mission trips:<br/><br/><p>';

                                            $.each( data.MissionSummaries, function (index, summary) {{
                                                
                                                popoverContent = popoverContent + summary.GroupName + ' (' + summary.TripDate + ')<br/>';

                                            }});

                                            $badge.attr('data-original-title', popoverContent + '</p>' );

                                        }}
                                        else {{
                                            $badge.attr('data-original-title', '{1} has not been on a recent Missions trip.' );
                                        }}

                                        $badge.show();
                                    }}
                                }},
                        }});
                    }});
                    </script>
                    ",
                     person.Id.ToString(),
                     person.NickName.EncodeHtml(),
                     badge.Id ) );
            }
        }
    }
}
