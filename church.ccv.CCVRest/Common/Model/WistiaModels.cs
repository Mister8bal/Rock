using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.Common.Model
{
    [Serializable]
    public class WistiaMedia
    {
        public int Id;
        public string Name;
        public string Type;
        public DateTime Created;
        public DateTime Updated;
        public float Duration;
        public string Hashed_Id;
        public string Description;
        public float Progress;
        public string Status;
        public WistiaThumbnail Thumbnail;
        public WistiaProject Project;
        public List<WistiaAsset> Assets;
        public string EmbedCode;
    }

    [Serializable]
    public class WistiaAsset
    {
        // define the asset types Wistia supports
        public const string OriginalFile = "originalfile";
        public const string FlashVideoFile = "flashvideofile";
        public const string MdFlashVideoFile = "mdflashvideofile";
        public const string HdFlashVideoFile = "hdflashvideofile";
        public const string Mp4VideoFile = "mp4videofile";
        public const string MdMp4VideoFile = "mdmp4videofile";
        public const string HdMp4VideoFile = "hdmp4videofile";
        public const string IPhoneVideoFile = "iphonevideofile";
        public const string StillImageFile = "stillimagefile";
        public const string SwfFile = "swffile";
        public const string Mp3AudioFile = "mp3audiofile";
        public const string LargeImageFile = "largeimagefile";

        public string URL;
        public int Width;
        public int Height;
        public UInt64 FileSize;
        public string ContentType;
        public string Type;
    }

    [Serializable]
    public class WistiaThumbnail
    {
        public string URL;
        public int Width;
        public int Height;
    }

    [Serializable]
    public class WistiaProject
    {
        public int Id;
        public string Name;
        public int MediaCount;
        public DateTime Created;
        public DateTime Updated;
        public string Hashed_Id;
        public bool AnonymousCanUpload;
        public bool AnonymousCanDownload;
        public bool Public;
        public string PublicId; //Intentionally a string
        public string Description;
        public List<WistiaMedia> Medias;
    }
}
