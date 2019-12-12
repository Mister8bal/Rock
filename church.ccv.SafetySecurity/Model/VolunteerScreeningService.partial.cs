using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Model;

namespace church.ccv.SafetySecurity.Model
{
    public partial class VolunteerScreeningService
    {
        /// <summary>
        /// Deletes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override bool Delete( VolunteerScreening item )
        {
            int sCharacterReference_WorkflowId = 203;
            var rockContext = ( Rock.Data.RockContext ) this.Context;
            rockContext.WrapTransaction( () =>
            {
                // remove attached workflows
                if ( this != null && item.Application_WorkflowId.HasValue )
                {
                    var workflowService = new WorkflowService( rockContext );
                    Workflow wf = workflowService.Get( item.Application_WorkflowId.Value );

                    // get character references
                    List<int?> attribIds = new AttributeValueService( rockContext ).Queryable()
                        .AsNoTracking()
                        .Where( av => av.Attribute.Key == "VolunteerScreeningInstanceId" && av.ValueAsNumeric == item.Id )
                        .Select( av => av.EntityId )
                        .ToList();

                    List<Workflow> charRefWorkflows = new List<Workflow>();
                    if ( attribIds.Count > 0 )
                    {
                        charRefWorkflows = workflowService.Queryable()
                            .Where( w => w.WorkflowTypeId == sCharacterReference_WorkflowId && attribIds.Contains( w.Id ) )
                            .ToList();

                        // remove character workflows
                        if ( charRefWorkflows.Any() )
                        {
                            workflowService.DeleteRange( charRefWorkflows );
                        }
                    }

                    // remove attached workflow
                    workflowService.Delete( wf );
                }

                rockContext.SaveChanges();
            } );

            return base.Delete( item );
        }
    }
}
