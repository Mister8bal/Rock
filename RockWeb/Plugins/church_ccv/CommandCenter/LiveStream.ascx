<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LiveStream.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.CommandCenter.LiveStream" %>

<div class="container">
    <Rock:NotificationBox id="ntbAlert" runat="server" NotificationBoxType="Danger" Visible="false" Text="There are currently no streams available." />
</div>

<asp:Repeater ID="rptvideostreams" runat="server" >
    <ItemTemplate>
        <div class="<%# Eval("BoostrapColumn") %>">
            <div class="panel panel-default">                
                <div class="panel-body">   
                    <h3><%# Eval("Campus") %></h3>
                    <video id='<%# Eval("VideoId") %>' class="video-js vjs-default-skin videocontent" data-setup='{ "controls": true, "autoplay": true }'>
                        <source src='<%# Eval("Url") %>' type="application/x-mpegURL">
                    </video>
                    <br />
                    <div style="text-align: center;">
                        <a id='<%# Eval("VideoId") %>' class="btn btn-default audio-toggle muted" >
                            <i class="fa fa-volume-off"></i>
                            <span></span>
                        </a>   
                    </div>
                    
                    <script>
                        // this will mute each video as soon as the player is ready.  This ensures
                        //  that we are compliant with html5 video autoplay rules.
                        videojs("<%# Eval("VideoId") %>").ready(function () {
                            var myPlayer = this;
                            myPlayer.muted(true);

                            <%--if ( <%# Eval("Order") %> === 1) {
                                myPlayer.muted(false);
                            }
                            else {
                                myPlayer.muted(true);
                            }--%>
                        });
                    </script>
                </div>
            </div>
        </div>
    </ItemTemplate>
</asp:Repeater>


<script type="text/javascript">   

    function pageLoad() {
        // untoggle first button
        //$('.audio-toggle').first().each(function () {
        //    $(this).addClass('enabled');
        //    $(this).removeClass('muted');
        //    $(this).addClass('btn-primary');
        //    $(this).removeClass('btn-default');
        //});
    }

    $('.audio-toggle').click(function (event) {
        // set flag if user clicked on current item this will note that they wish to mute current channel
        var currentItem = false;
        if ($(this).is('.enabled')) {
            currentItem = true;
        }

        // set all buttons to mute
        $('.audio-toggle').each(function (index) {
            $(this).removeClass('enabled');
            $(this).addClass('muted');
            $(this).removeClass('btn-primary');
            $(this).addClass('btn-default');
        });

        // mute all videos
        $('video').each(function () {
            var videoId = $(this).attr('id');
            videojs(videoId).muted(true);
        });

        // get id of video player from button id (need to append _html5_api because videojs changes the video id)
        var playerId = $(this).attr('id');
        videoPlayerId = playerId.concat("_html5_api");
        console.log( videoPlayerId );

        // enabled selected video unless it is the active one, then mute
        if (currentItem) {
            videojs(videoPlayerId).muted(true);
        } else {
            $(this).addClass('enabled');
            $(this).addClass('btn-primary');
            $(this).removeClass('btn-default');
            videojs(videoPlayerId).muted(false);
        }
    });
</script>