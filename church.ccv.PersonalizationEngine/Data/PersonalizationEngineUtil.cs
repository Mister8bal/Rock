using church.ccv.PersonalizationEngine.Model;
using Rock.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using static church.ccv.PersonalizationEngine.Model.Campaign;

namespace church.ccv.PersonalizationEngine.Data
{
    public static class PersonalizationEngineUtil
    {
        #region PERSONA
        public static bool PersonaFits( Persona persona, int personId )
        {
            // determine if the given personId fits the given persona

            // execute the sql defining that persona
            using ( RockContext rockContext = new RockContext( ) )
            {
                var fitsPersona = rockContext.Database.SqlQuery<int>
                (
                    persona.RockSQL,
                    new SqlParameter( "@PersonId", personId ) { SqlDbType = SqlDbType.Int, IsNullable = false }
                ).SingleOrDefault( );

                // if the resulting value returned is non zero, then the person matched the persona definition
                return fitsPersona == 1 ? true : false;
            }
        }

        public static Persona GetPersona( int id )
        {
            // return a particular persona

            using ( RockContext rockContext = new RockContext( ) )
            {
                PersonalizationEngineService<Persona> peService = new PersonalizationEngineService<Persona>( rockContext );

                Persona persona = peService.Get( id );

                return persona;
            }
        }
        
        public static List<Persona> GetPersonas( int personId )
        {
            // return all personas that the personId fits

            using ( RockContext rockContext = new RockContext( ) )
            {
                PersonalizationEngineService<Persona> peService = new PersonalizationEngineService<Persona>( rockContext );
                var personaQuery = peService.Queryable( ).AsNoTracking( );
             
                // start with an empty list of personas
                List<Persona> personaList = new List<Persona>( );
                foreach( var persona in personaQuery )
                {
                    // for each persona, execute the sql defining that persona
                    if ( PersonaFits( persona, personId ) )
                    {
                        // if it returned true, then the persona fit, so add it to the list
                        personaList.Add( persona );
                    }
                }

                return personaList;
            }
        }

        public static void DeletePersona( int personaId )
        {
            // remove the persona and all linkages
            using ( RockContext rockContext = new RockContext( ) )
            {
                PersonalizationEngineService<Persona> peService = new PersonalizationEngineService<Persona>( rockContext );

                var personaObj = peService.Get( personaId );
                if( personaObj != null )
                {
                    // get any linkages attached to it
                    Service<Linkage> linkageService = new Service<Linkage>( rockContext );
                    var personaLinkages = linkageService.Queryable( ).Where( l => l.PersonaId == personaObj.Id );

                    if ( personaLinkages != null )
                    {
                        // remove the linkages
                        linkageService.DeleteRange( personaLinkages );
                    }

                    // and delete the persona
                    peService.Delete( personaObj );

                    rockContext.SaveChanges( );
                }
            }
        }
        #endregion

        #region CAMPAIGN
        public static Campaign GetCampaign( int id )
        {
            // get a particular campaign

            using ( RockContext rockContext = new RockContext( ) )
            { 
                PersonalizationEngineService<Campaign> peService = new PersonalizationEngineService<Campaign>( rockContext );

                var campaign = peService.Get( id );
                return campaign;
            }
        }

        public static List<Campaign> GetCampaigns( string campaignTypeList, DateTime? startDate = null, DateTime? endDate = null, bool isDefault = false )
        {
            // get all campaigns of the provided types, that fall within the requested date range
            
            // Default campaigns are those that are NOT tied to a persona.
            // if isDefault is FALSE, then we get campaigns that ARE tied to personas
            // if isDefault is TRUE, then we get campaigns that are NOT tied to personas

            //note: the dates are treated as inclusive, meaning the campaign must have a startDate BEFORE the provided startDate
            // and an end date that lands AFTER the provided endDate

            using ( RockContext rockContext = new RockContext( ) )
            {
                PersonalizationEngineService<Campaign> peService = new PersonalizationEngineService<Campaign>( rockContext );

                // the types column on the campaign is a comma seperated value of all Types the campaign should display on.
                
                // We'll take the provided typeList and convert it into a string array of the types.
                // Then we'll use linq to take each element in the typeListArray, and see if any of those elements are contained
                // in the Campaign's Type CSV. If they are, then it's a match!
                
                // convert the string list into an array
                string[] campaignTypes = campaignTypeList.Split( ',' );
                
                var campaigns = peService.Queryable( ).AsNoTracking( )
                                         //The campaign start date must be valid, but if the user passed in null OR the campaign start date is earlier than the date provided
                                         .Where( c => startDate.HasValue == false || c.StartDate <= startDate.Value )

                                         // A campaign does NOT need to have an end date--so if it doesn't have one just take it
                                         .Where( c => c.EndDate.HasValue == false || endDate.HasValue == false || c.EndDate >= endDate.Value )

                                         // for each Campaign, see if any element of campaignTypeIds is contained in c.Type (the CSV)
                                         .Where( c => campaignTypes.Any( t => c.Type.Contains( t ) ) )
                                         
                                         // default campaigns are campaigns appropriate for anyone, and that are not tied to a persona
                                         .Where( c => c.IsDefault == isDefault ) 

                                         .OrderByDescending( c => c.Priority )
                                         .ToList( ); //take those
                return campaigns;
            }
        }

        public static void DeleteCampaign( int campaignId )
        {
            // Deletes a campaign and all associated linkages

            using ( RockContext rockContext = new RockContext( ) )
            {
                // first get the campaign selected
                Service<Campaign> campaignService = new Service<Campaign>( rockContext );

                Campaign campaignObj = campaignService.Get( campaignId );
                if ( campaignObj != null )
                {
                    // get any linkages attached to it
                    Service<Linkage> linkageService = new Service<Linkage>( rockContext );
                    var campaignLinkages = linkageService.Queryable( ).Where( l => l.CampaignId == campaignObj.Id );

                    if ( campaignLinkages != null )
                    {
                        // remove the linkages
                        linkageService.DeleteRange( campaignLinkages );
                    }

                    // and delete the campaign
                    campaignService.Delete( campaignObj );

                    rockContext.SaveChanges( );
                }
            }
        }
        #endregion

        #region Linkages
        public static List<Persona> GetPersonasForCampaign( int campaignId )
        {
            // get all the personas tied to the given campaign

            using ( RockContext rockContext = new RockContext( ) )
            {
                // get queries to the linkage and persona table
                var peLinkageQry = new PersonalizationEngineService<Linkage>( rockContext ).Queryable( ).AsNoTracking( );
                var pePersonaQry = new PersonalizationEngineService<Persona>( rockContext ).Queryable( ).AsNoTracking( );

                // join the persona table to the linkage table, and take all rows where the campaign id matches the provided campaign
                var personas = peLinkageQry.Join( pePersonaQry, l => l.PersonaId, p => p.Id, ( l, p ) => new { Linkage = l, Persona = p } )
                                           .Where( lp => lp.Linkage.CampaignId == campaignId )
                                           .Select( a => a.Persona )
                                           .ToList( );

                return personas;
            }
        }

        public static List<Campaign> GetCampaignsForPersona( int personaId )
        {
            // get all the campaigns tied to the given persona

            using ( RockContext rockContext = new RockContext( ) )
            {
                // get queries to the linkage and persona table
                var peLinkageQry = new PersonalizationEngineService<Linkage>( rockContext ).Queryable( ).AsNoTracking( );
                var peCampaignQry = new PersonalizationEngineService<Campaign>( rockContext ).Queryable( ).AsNoTracking( );

                // join the persona table to the linkage table, and take all rows where the campaign id matches the provided campaign
                var campaigns = peLinkageQry.Join( peCampaignQry, l => l.CampaignId, c => c.Id, ( l, c ) => new { Linkage = l, Campaign = c } )
                                            .Where( lp => lp.Linkage.PersonaId == personaId )
                                            .Select( a => a.Campaign )
                                            .OrderByDescending( c => c.Priority )
                                            .ToList( );

                return campaigns;
            }
        }

        public static void LinkCampaignToPersona( int campaignId, int personaId )
        {
            // ties a campaign and perona together by adding an entry in the Linkage table

            using ( RockContext rockContext = new RockContext( ) )
            {
                Linkage linkage = new Linkage( )
                {
                    CampaignId = campaignId,
                    PersonaId = personaId
                };

                Service<Linkage> linkageService = new Service<Linkage>( rockContext );
                linkageService.Add( linkage );

                rockContext.SaveChanges( );
            }
        }

        public static void UnlinkCampaignFromPersona( int campaignId, int personaId )
        {
            // deletes the linkage that ties the campaign to the persona

            using ( RockContext rockContext = new RockContext( ) )
            {
                Service<Linkage> linkageService = new Service<Linkage>( rockContext );

                // note that we expect there to be exactly ONE linkage with the matching campaign and persona ids
                Linkage linkage = linkageService.Queryable( ).Where( l => l.CampaignId == campaignId && 
                                                                          l.PersonaId == personaId )
                                                             .Single( );

                linkageService.Delete( linkage );

                rockContext.SaveChanges( );
            }
        }
        #endregion

        #region General
        public static List<Campaign> GetRelevantCampaign( string campaignTypeList, int personId, int numCampaigns = 1 )
        {
            //given a person id, get whatever is less - the number of relevant campaigns that exist, or numCampaigns.

            // guard against passing in <= 0 numbers
            numCampaigns = Math.Max( numCampaigns, 1 );

            // get all the campaigns that match the center card
            var campaignList = GetCampaigns( campaignTypeList, DateTime.Now, DateTime.Now, false );

            // now go thru their personas, and take the first campaign with a persona that fits
            List<Campaign> relevantCampaigns = new List<Campaign>( );
            foreach( Campaign campaign in campaignList )
            {
                var personas = GetPersonasForCampaign( campaign.Id );
                foreach( var persona in personas )
                {
                    // as soon as we find a matching persona, take this campaign and stop searching
                    if( PersonaFits( persona, personId ) )
                    {
                        relevantCampaigns.Add( campaign );

                        // subtract off each time we find a campaign, and when this is 0, we're done
                        numCampaigns--;
                        break;
                    }
                }

                // once we've found the requested number of relevant campaigns, stop
                if ( numCampaigns == 0 )
                {
                    break;
                }
            }

            return relevantCampaigns;
        }

        public static List<Campaign> GetDefaultCampaign( string campaignTypeList, int numCampaigns = 1 )
        {
            // guard against passing in <= 0 numbers
            numCampaigns = Math.Max( numCampaigns, 1 );

            // default campaigns are campaigns appropriate for anyone, and that are not tied to a persona
            using ( RockContext rockContext = new RockContext( ) )
            {
                // get all the default campaigns
                var defaultCampaigns = GetCampaigns( campaignTypeList, DateTime.Now, DateTime.Now, true );

                // now take only what they asked for (a little less efficient than doing this at the database level, but results in simpler code)
                
                // clamp the count to what's actually available
                numCampaigns = Math.Min( defaultCampaigns.Count, numCampaigns );
                defaultCampaigns = defaultCampaigns.GetRange( 0, numCampaigns );

                return defaultCampaigns;
            }
        }
        #endregion
    }
}
