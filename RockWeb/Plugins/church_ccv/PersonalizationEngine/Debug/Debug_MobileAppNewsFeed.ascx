<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Debug_MobileAppNewsFeed.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.PersonalizationEngine.Debug_MobileAppNewsFeed" %>

<asp:Panel ID="upnlSettings" runat="server">
    <asp:Literal ID="lContent" runat="server"></asp:Literal>
</asp:Panel>

<style>
    @font-face {
        font-family: 'DINWebCondensed';
        src: url('../Plugins/church_ccv/PersonalizationEngine/Debug/Assets/Fonts/din-black.eot');
        src: url('../Plugins/church_ccv/PersonalizationEngine/Debug/Assets/Fonts/din-black.eot?#iefix') format('embedded-opentype'),url('../Plugins/church_ccv/PersonalizationEngine/Debug/Assets/Fonts/din-black.woff2') format('woff2'),url('../Plugins/church_ccv/PersonalizationEngine/Debug/Assets/Fonts/din-black.woff') format('woff'),url('../Plugins/church_ccv/PersonalizationEngine/Debug/Assets/Fonts/din-black.ttf') format('truetype');
        font-weight: 900;
        font-style: normal;
    }

    .mobile-app-wrapper {
            display: flex;
            flex-direction: column;
        }

    @media screen and (min-width: 1000px) {
        .mobile-app-wrapper {
            flex-direction: row;
        }
    }

    .mobile-app-wrapper img {
        max-width: 320px;
        max-height: 568px;
    }

    .mobile-app-header {
        min-height: 45px;
        max-height: 45px;
        background-color: #191919;
    }

    .mobile-app-header i {
        font-size: 36px;
    }

    .mobile-app-news-feed {
        max-width: 320px;
        min-height: 568px;
        max-height: 568px;

        background-color: #212121;   
    }

    .spacer {
        min-width: 100px;
        min-height: 100px;
    }

    .mobile-app-news-detail {
        max-width: 320px;
        min-height: 568px;
        max-height: 568px;

        background-color: #212121;   

        display: flex;
        flex-direction: column;
    }

    .mobile-app-news-detail h4 {
        padding-left: 20px;
        padding-right: 20px;
                
        font-size: 24px;
        color: #CCCCCC;
        font-family: DinWebCondensed;
        font-weight: 900;
    }

    .mobile-app-news-detail p {
        padding-left: 20px;
        padding-right: 20px;

        font-family: OpenSans;
        color: #CCCCCC;
        font-weight: 100;

        height: 100%;
    }

    .mobile-app-learn-more {
        align-self: flex-end;

        margin: 0 auto 10px auto;

        min-width: 144px;
        max-width: 144px;

        min-height: 34px;
        max-height: 34px;

        border-radius: 4px;
        background-color: #7e7e7e;

        display: flex;
        align-items: center;
        justify-content: center;
    }

    .mobile-app-learn-more a {
        font-family: OpenSans;
        font-weight: 100;
        color: #CCCCCC;
    }

    .mobile-app-learn-more a:hover {
        text-decoration: none;
    }

    .mobile-app-footer {
        align-self: flex-end;
    }

</style>