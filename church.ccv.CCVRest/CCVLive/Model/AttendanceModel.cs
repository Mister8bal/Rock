// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.CCVLive.Model
{
    /**
     * <summary>Represents attendance interaction data</summary> 
     */ 
    [Serializable]
    public class AttendanceModel
    {
        /**
         * <summary>
         * The interactions component Id for the associated Interaction Component.  
         * <see cref="Rock.Model.InteractionComponent"/> 
         * </summary>
         * <type>
         * The Interaction Component id for this attendance interaction.
         * </type>
         */ 
        public int InteractionComponentId;

        /**
        * <summary>
        * The string representation of the operation completed.  
        * </summary>
        * <type>
        * String: the operation completed. 
        * </type>
        */
        public string Operation;

        /**
        * <summary>
        * The Id for the PersonAlias for which to associate the interaction.  
        * <see cref="Rock.Model.PersonAlias"/> 
        * </summary>
        * <type>
        * Int: PersonAliasId: the id of the person alias associate with the interaction.
        * </type>
        */
        public int PersonAliasId;

        /**
        * <summary>
        * The interaction session id for the InteractionSession to be associated with the interaction.  
        * Note: UsesSession should be set to true in the InteractionChannel for this to 
        * have any real meaning.
        * <see cref="Rock.Model.InteractionSession"/> 
        * </summary>
        * <type>
        * The Interaction Session Id for this attendance interaction.
        * </type>
        */
        public int InteractionSessionId;

        /**
        * <summary>
        * A Time string used to set the CreatedDateTime value.  If none is provided,
        * the value of Interaction.CreatedDateTime will be set based on 
        * execution.
        * </summary>
        * <type>
        * String: RequestTime: A time string representing the time at which the interaction occured.
        * </type>
        */
        public string RequestTime;

        public string Email;

        public string Name;

        public AttendanceModel()
        {

        }

    }

    
}
