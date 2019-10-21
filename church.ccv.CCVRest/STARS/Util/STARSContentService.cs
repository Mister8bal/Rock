using System;
using System.Collections.Generic;
using church.ccv.CCVRest.STARS.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.STARS.Util
{
    class STARSFieldStatusService
    {
        // attribute id for field status attribute, summary attribute and content channel id
        private static int _attributeId_FieldStatus = 94565;
        private static int _attributeId_Summary = 94566;
        private static int _contentChannelId_CampusFieldStatus = 314;

        /// <summary>
        /// Return Field Status
        /// </summary>
        /// <returns></returns>
        public static List<STARSFieldStatusModel> GetFieldStatus()
        {
            // list of objects that is going to be returned
            List<STARSFieldStatusModel> fieldStatusList = new List<STARSFieldStatusModel>();

            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );

            ContentChannel fieldStatusContent = contentChannelService.Get( _contentChannelId_CampusFieldStatus );

            // loop through the items in the field status content channel
            foreach ( ContentChannelItem item in fieldStatusContent.Items )
            {
                item.LoadAttributes();

                AttributeValueCache avCampus = item.AttributeValues["Campus"];

                // get field status and summary atrribute value by passing attribute Id and content item id
                AttributeValue fieldStatusAttributeValue = attributeValueService.GetByAttributeIdAndEntityId( _attributeId_FieldStatus, item.Id );
                AttributeValue summaryAttributeValue = attributeValueService.GetByAttributeIdAndEntityId( _attributeId_Summary, item.Id );

                // create new field status model object
                STARSFieldStatusModel fieldStatusModel = new STARSFieldStatusModel();

                fieldStatusModel.EntityId = item.Id;

                if ( fieldStatusAttributeValue != null && summaryAttributeValue != null )
                {
                    fieldStatusModel.FieldStatus = fieldStatusAttributeValue.Value;
                    fieldStatusModel.Summary = summaryAttributeValue.Value;

                    if ( fieldStatusAttributeValue.ModifiedDateTime != null && summaryAttributeValue.ModifiedDateTime != null )
                    {
                        // set the model modified datetime to latest modifiedDateTime
                        if ( fieldStatusAttributeValue.ModifiedDateTime > summaryAttributeValue.ModifiedDateTime )
                        {
                            fieldStatusModel.ModifiedDateTime = String.Format( "{0:g}", fieldStatusAttributeValue.ModifiedDateTime );
                        }
                        else
                        {
                            fieldStatusModel.ModifiedDateTime = String.Format( "{0:g}", summaryAttributeValue.ModifiedDateTime );
                        }
                    }
                }

                if ( avCampus != null )
                {
                    fieldStatusModel.CampusName = avCampus.Value;
                }

                fieldStatusList.Add( fieldStatusModel );
            }

            return fieldStatusList;
        }
    }
}
