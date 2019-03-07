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
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Linq;

using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Data Access/Service class for <see cref="Rock.Model.Person"/> entity objects.
    /// </summary>
    public partial class PersonService
    {     
   
        #region CCV addition for bringing captive portal from v8 to v7

        /// <summary>
        /// The cut off (inclusive) score 
        /// </summary>
        private const int MATCH_SCORE_CUTOFF = 35;

        /// <summary>
        /// Gets an enumerable collection of <see cref="Rock.Model.Person"/> entities that have a matching email address, firstname and lastname.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="email">The email.</param>
        /// <param name="includeDeceased">if set to <c>true</c> [include deceased].</param>
        /// <param name="includeBusinesses">if set to <c>true</c> [include businesses].</param>
        /// <returns></returns>
        public IEnumerable<Person> FindPersons( string firstName, string lastName, string email, bool includeDeceased = false, bool includeBusinesses = false )
        {
            return FindPersons( new PersonMatchQuery( firstName, lastName, email, string.Empty ) );
        }


        /// <summary>
        /// Finds people who are considered to be good matches based on the query provided.
        /// </summary>
        /// <param name="searchParameters">The search parameters.</param>
        /// <param name="includeDeceased">if set to <c>true</c> [include deceased].</param>
        /// <param name="includeBusinesses">if set to <c>true</c> [include businesses].</param>
        /// <returns>A IEnumerable of person, ordered by the likelihood they are a good match for the query.</returns>
        public IEnumerable<Person> FindPersons( PersonMatchQuery searchParameters, bool includeDeceased = false, bool includeBusinesses = false )
        {
            // Because we are search different tables (PhoneNumber, PreviousName, etc.) we do multiple queries, store the results in a dictionary, and then find good matches by scoring the results of the dictionary.
            // The large query we're building is: (email matches AND suffix matches AND DoB loose matches AND gender matches) OR last name matches OR phone number matches OR previous name matches
            // The dictionary is PersonId => PersonMatchResult, a class that stores the items that match and calculates the score

            // Query by last name, suffix, dob, and gender
            var query = Queryable( includeDeceased, includeBusinesses )
                .AsNoTracking()
                .Where( p => p.LastName == searchParameters.LastName );


            if ( searchParameters.SuffixValueId.HasValue )
            {
                query = query.Where( a => a.SuffixValueId == searchParameters.SuffixValueId.Value || a.SuffixValueId == null );
            }

            // Check for a DOB match here ignoring year and we award higher points if the year *does* match later, this allows for two tiers of scoring for birth dates
            if ( searchParameters.BirthDate.HasValue )
            {
                query = query.Where( a => ( a.BirthMonth == searchParameters.BirthDate.Value.Month && a.BirthDay == searchParameters.BirthDate.Value.Day ) || a.BirthMonth == null || a.BirthDay == null );
            }

            if ( searchParameters.Gender.HasValue )
            {
                query = query.Where( a => a.Gender == searchParameters.Gender.Value || a.Gender == Gender.Unknown );
            }

            // Create dictionary
            var foundPeople = query
                .Select( p => new PersonSummary()
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    NickName = p.NickName,
                    Gender = p.Gender,
                    BirthDate = p.BirthDate,
                    SuffixValueId = p.SuffixValueId
                } )
                .ToList()
                .ToDictionary(
                    p => p.Id,
                    p =>
                    {
                        var result = new PersonMatchResult( searchParameters, p )
                        {
                            LastNameMatched = true
                        };
                        return result;
                    }
                );

            if ( searchParameters.Email.IsNotNullOrWhiteSpace() )
            {
                Queryable( includeDeceased, includeBusinesses )
                    .AsNoTracking()
                    .Where(
                        p => ( p.Email != String.Empty && p.Email != null && p.Email == searchParameters.Email ) 
                    )
                    .Select( p => new PersonSummary()
                    {
                        Id = p.Id,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        NickName = p.NickName,
                        Gender = p.Gender,
                        BirthDate = p.BirthDate,
                        SuffixValueId = p.SuffixValueId
                    } )
                    .ToList()
                    .ForEach( p =>
                    {
                        if ( foundPeople.ContainsKey( p.Id ) )
                        {
                            foundPeople[p.Id].EmailMatched = true;
                        }
                        else
                        {
                            foundPeople[p.Id] = new PersonMatchResult( searchParameters, p )
                            {
                                EmailMatched = true
                            };
                        }
                    } );
            }

            var rockContext = new RockContext();

            // OR query for previous name matches
            var previousNameService = new PersonPreviousNameService( rockContext );
            previousNameService.Queryable( "PersonAlias.Person" )
                .AsNoTracking()
                .Where( n => n.LastName == searchParameters.LastName )
                .Select( n => new PersonSummary()
                {
                    Id = n.PersonAlias.Person.Id,
                    FirstName = n.PersonAlias.Person.FirstName,
                    LastName = n.PersonAlias.Person.LastName,
                    NickName = n.PersonAlias.Person.NickName,
                    Gender = n.PersonAlias.Person.Gender,
                    BirthDate = n.PersonAlias.Person.BirthDate,
                    SuffixValueId = n.PersonAlias.Person.SuffixValueId
                } )
                .ToList()
                .ForEach( p =>
                {
                    if ( foundPeople.ContainsKey( p.Id ) )
                    {
                        foundPeople[p.Id].PreviousNameMatched = true;
                    }
                    else
                    {
                        foundPeople[p.Id] = new PersonMatchResult( searchParameters, p )
                        {
                            PreviousNameMatched = true
                        };
                    }
                } );

            // OR query for mobile phone numbers
            if ( searchParameters.MobilePhone.IsNotNullOrWhiteSpace() )
            {
                var mobilePhoneTypeId = DefinedValueCache.Read( SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;
                var phoneNumberService = new PhoneNumberService( rockContext );
                phoneNumberService.Queryable( "Person" )
                    .AsNoTracking()
                    .Where( n => n.Number == searchParameters.MobilePhone && n.NumberTypeValueId == mobilePhoneTypeId )
                    .Select( n => new PersonSummary()
                    {
                        Id = n.Person.Id,
                        FirstName = n.Person.FirstName,
                        LastName = n.Person.LastName,
                        NickName = n.Person.NickName,
                        Gender = n.Person.Gender,
                        BirthDate = n.Person.BirthDate,
                        SuffixValueId = n.Person.SuffixValueId
                    } )
                    .ToList()
                    .ForEach( p =>
                    {
                        if ( foundPeople.ContainsKey( p.Id ) )
                        {
                            foundPeople[p.Id].MobileMatched = true;
                        }
                        else
                        {
                            foundPeople[p.Id] = new PersonMatchResult( searchParameters, p )
                            {
                                MobileMatched = true
                            };
                        }
                    } );
            }

            // Find people who have a good confidence score
            var goodMatches = foundPeople.Values
                .Where( match => match.ConfidenceScore >= MATCH_SCORE_CUTOFF )
                .OrderByDescending( match => match.ConfidenceScore );

            return GetByIds( goodMatches.Select( a => a.PersonId ).ToList() );
        }


        #region FindPersonClasses

        /// <summary>
        /// Contains the properties that can be searched for when performing a GetBestMatch query
        /// </summary>
        public class PersonMatchQuery
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PersonMatchQuery"/> class.
            /// </summary>
            /// <param name="firstName">The first name.</param>
            /// <param name="lastName">The last name.</param>
            /// <param name="email">The email.</param>
            /// <param name="mobilePhone">The mobile phone.</param>
            public PersonMatchQuery( string firstName, string lastName, string email, string mobilePhone )
            {
                FirstName = firstName.IsNotNullOrWhiteSpace() ? firstName.Trim() : string.Empty;
                LastName = lastName.IsNotNullOrWhiteSpace() ? lastName.Trim() : string.Empty;
                Email = email.IsNotNullOrWhiteSpace() ? email.Trim() : string.Empty;
                MobilePhone = mobilePhone.IsNotNullOrWhiteSpace() ? PhoneNumber.CleanNumber( mobilePhone ) : string.Empty;
                Gender = null;
                BirthDate = null;
                SuffixValueId = null;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PersonMatchQuery" /> class. Use this constructor when the person may not have a birth year.
            /// </summary>
            /// <param name="firstName">The first name.</param>
            /// <param name="lastName">The last name.</param>
            /// <param name="email">The email.</param>
            /// <param name="mobilePhone">The mobile phone.</param>
            /// <param name="gender">The gender.</param>
            /// <param name="birthMonth">The birth month.</param>
            /// <param name="birthDay">The birth day.</param>
            /// <param name="birthYear">The birth year.</param>
            /// <param name="suffixValueId">The suffix value identifier.</param>
            public PersonMatchQuery( string firstName, string lastName, string email, string mobilePhone, Gender? gender = null, int? birthMonth = null, int? birthDay = null, int? birthYear = null, int? suffixValueId = null )
            {
                FirstName = firstName.IsNotNullOrWhiteSpace() ? firstName.Trim() : string.Empty;
                LastName = lastName.IsNotNullOrWhiteSpace() ? lastName.Trim() : string.Empty;
                Email = email.IsNotNullOrWhiteSpace() ? email.Trim() : string.Empty;
                MobilePhone = mobilePhone.IsNotNullOrWhiteSpace() ? PhoneNumber.CleanNumber( mobilePhone ) : string.Empty;
                Gender = gender;
                BirthDate = birthDay.HasValue && birthMonth.HasValue ? new DateTime( birthYear ?? DateTime.MinValue.Year, birthMonth.Value, birthDay.Value ) : ( DateTime? ) null;
                SuffixValueId = suffixValueId;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PersonMatchQuery"/> class.
            /// </summary>
            /// <param name="firstName">The first name.</param>
            /// <param name="lastName">The last name.</param>
            /// <param name="email">The email.</param>
            /// <param name="mobilePhone">The mobile phone.</param>
            /// <param name="gender">The gender.</param>
            /// <param name="birthDate">The birth date.</param>
            /// <param name="suffixValueId">The suffix value identifier.</param>
            public PersonMatchQuery( string firstName, string lastName, string email, string mobilePhone, Gender? gender = null, DateTime? birthDate = null, int? suffixValueId = null )
            {
                FirstName = firstName.IsNotNullOrWhiteSpace() ? firstName.Trim() : string.Empty;
                LastName = lastName.IsNotNullOrWhiteSpace() ? lastName.Trim() : string.Empty;
                Email = email.IsNotNullOrWhiteSpace() ? email.Trim() : string.Empty;
                MobilePhone = mobilePhone.IsNotNullOrWhiteSpace() ? PhoneNumber.CleanNumber( mobilePhone ) : string.Empty;
                Gender = gender;
                BirthDate = birthDate;
                SuffixValueId = suffixValueId;
            }

            /// <summary>
            /// Gets or sets the first name.
            /// </summary>
            /// <value>
            /// The first name.
            /// </value>
            public string FirstName { get; set; }

            /// <summary>
            /// Gets or sets the last name.
            /// </summary>
            /// <value>
            /// The last name.
            /// </value>
            public string LastName { get; set; }

            /// <summary>
            /// Gets or sets the email.
            /// </summary>
            /// <value>
            /// The email.
            /// </value>
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets the mobile phone.
            /// </summary>
            /// <value>
            /// The mobile phone.
            /// </value>
            public string MobilePhone { get; set; }

            /// <summary>
            /// Gets or sets the gender.
            /// </summary>
            /// <value>
            /// The gender.
            /// </value>
            public Gender? Gender { get; set; }

            /// <summary>
            /// Gets or sets the birth date.
            /// </summary>
            /// <value>
            /// The birth date.
            /// </value>
            public DateTime? BirthDate { get; set; }

            /// <summary>
            /// Gets or sets the suffix value identifier.
            /// </summary>
            /// <value>
            /// The suffix value identifier.
            /// </value>
            public int? SuffixValueId { get; set; }
        }

        /// <summary>
        /// A class to summarise the components of a Person which matched a PersonMatchQuery and produce a score representing the likelihood this match is the correct match.
        /// </summary>
        private class PersonMatchResult
        {

            /// <summary>
            /// Initializes a new instance of the <see cref="PersonMatchResult"/> class.
            /// </summary>
            /// <param name="query">The person match query.</param>
            /// <param name="person">The person summary.</param>
            public PersonMatchResult( PersonMatchQuery query, PersonSummary person )
            {
                PersonId = person.Id;
                FirstNameMatched = ( person.FirstName != null && person.FirstName != String.Empty && person.FirstName.Equals( query.FirstName, StringComparison.CurrentCultureIgnoreCase ) ) || ( person.NickName != null && person.NickName != String.Empty && person.NickName.Equals( query.FirstName, StringComparison.CurrentCultureIgnoreCase ) );
                LastNameMatched = person.LastName != null && person.LastName != String.Empty && person.LastName.Equals( query.LastName, StringComparison.CurrentCultureIgnoreCase );
                SuffixMatched = query.SuffixValueId.HasValue && person.SuffixValueId != null && query.SuffixValueId == person.SuffixValueId;
                GenderMatched = query.Gender.HasValue & query.Gender == person.Gender;

                if ( query.BirthDate.HasValue && person.BirthDate.HasValue )
                {
                    BirthDate = query.BirthDate.Value.Month == person.BirthDate.Value.Month && query.BirthDate.Value.Day == person.BirthDate.Value.Day;
                    BirthDateYearMatched = BirthDate && person.BirthDate.Value.Year == query.BirthDate.Value.Year;
                }
            }

            public int PersonId { get; set; }

            public bool FirstNameMatched { get; set; }

            public bool LastNameMatched { get; set; }

            public bool EmailMatched { get; set; }

            public bool MobileMatched { get; set; }

            public bool PreviousNameMatched { get; set; }

            public bool SuffixMatched { get; set; }

            public bool GenderMatched { get; set; }

            public bool BirthDate { get; set; }

            public bool BirthDateYearMatched { get; set; }


            /// <summary>
            /// Calculates a score representing the likelihood this match is the correct match. Higher is better.
            /// </summary>
            /// <returns></returns>
            public int ConfidenceScore
            {
                get
                {
                    int total = 0;

                    if ( FirstNameMatched )
                    {
                        total += 15;
                    }

                    if ( LastNameMatched )
                    {
                        total += 15;
                    }

                    if ( PreviousNameMatched && !LastNameMatched )
                    {
                        total += 12;
                    }

                    if ( MobileMatched || EmailMatched )
                    {
                        total += 15;
                    }

                    if ( BirthDate )
                    {
                        total += 10;
                    }

                    if ( BirthDateYearMatched )
                    {
                        total += 5;
                    }

                    if ( GenderMatched )
                    {
                        total += 3;
                    }

                    if ( SuffixMatched )
                    {
                        total += 10;
                    }

                    return total;
                }
            }
        }

        /// <summary>
        /// Used to avoid bringing a whole Person into memory
        /// </summary>
        private class PersonSummary
        {
            public int Id { get; set; }
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string NickName { get; set; }

            public Gender Gender { get; set; }

            public DateTime? BirthDate { get; set; }

            public int? SuffixValueId { get; set; }
        }

        /// <summary>
        /// Looks for a single exact match based on the critieria provided. If more than one person is found it will return null (consider using FindPersons).
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="email">The email.</param>
        /// <param name="updatePrimaryEmail">if set to <c>true</c> the person's primary email will be updated to the search value if it was found as a person search key (alternate lookup address).</param>
        /// <param name="includeDeceased">if set to <c>true</c> include deceased individuals.</param>
        /// <param name="includeBusinesses">if set to <c>true</c> include businesses records.</param>
        /// <returns></returns>
        public Person FindPerson( string firstName, string lastName, string email, bool updatePrimaryEmail, bool includeDeceased = false, bool includeBusinesses = false )
        {
            return FindPerson( new PersonMatchQuery( firstName, lastName, email, string.Empty ), updatePrimaryEmail, includeDeceased, includeBusinesses );
        }

        /// <summary>
        /// Finds the person.
        /// </summary>
        /// <param name="personMatchQuery">The person match query.</param>
        /// <param name="updatePrimaryEmail">if set to <c>true</c> [update primary email].</param>
        /// <param name="includeDeceased">if set to <c>true</c> [include deceased].</param>
        /// <param name="includeBusinesses">if set to <c>true</c> [include businesses].</param>
        /// <returns></returns>
        public Person FindPerson( PersonMatchQuery personMatchQuery, bool updatePrimaryEmail, bool includeDeceased = false, bool includeBusinesses = false )
        {
            var matches = this.FindPersons( personMatchQuery, includeDeceased, includeBusinesses ).ToList();

            var match = matches.FirstOrDefault();

            // Check if we care about updating the person's primary email
            if ( updatePrimaryEmail && match != null )
            {
                return UpdatePrimaryEmail( personMatchQuery.Email, match );
            }

            return match;
        }

        /// <summary>
        /// Updates the primary email address of a person if they were found using an alternate email address
        /// </summary>
        /// <param name="email">The email.</param>
        /// <param name="match">The person to update.</param>
        /// <returns></returns>
        private Person UpdatePrimaryEmail( string email, Person match )
        {
            // Emails are already the same
            if ( string.Equals( match.Email, email, StringComparison.CurrentCultureIgnoreCase ) )
            {
                return match;
            }

            // The emails don't match and we've been instructed to update them
            using ( var privateContext = new RockContext() )
            {
                var privatePersonService = new PersonService( privateContext );
                var updatePerson = privatePersonService.Get( match.Id );
                updatePerson.Email = email;
                privateContext.SaveChanges();
            }

            // Return a freshly queried person
            return this.Get( match.Id );
        }


        #endregion

        #endregion
   
      


      
    }
}
