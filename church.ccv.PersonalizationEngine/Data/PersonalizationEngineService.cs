using Rock.Data;

namespace church.ccv.PersonalizationEngine.Data
{
    public class PersonalizationEngineService<T> : Rock.Data.Service<T> where T : Rock.Data.Entity<T>, new()
    {
        public PersonalizationEngineService( RockContext rockContext )
            : base( rockContext )
        {
        }
    }
}
