using System;

namespace church.ccv.CCVRest.STARS.Model
{
    [Serializable]
    public class STARSFieldStatusModel
    {
        public int EntityId;

        public string CampusName;

        public string ModifiedDateTime;

        public string Summary;

        public string FieldStatus;
    }
}