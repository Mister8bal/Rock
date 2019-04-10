using System;
using System.Globalization;
using System.Linq;
using System.Text;

using Rock;

namespace rocks.pillars.LavaFilters.Lava
{
    /// <summary>
    /// Custom Lava Filters
    /// </summary>
    public static class RockFilters
    {
        /// <summary>
        /// Replaces any accent characters with the base equivalent.
        /// </summary>
        /// <param name="input">A string.</param>
        /// <returns></returns>
        public static string ReplaceAccentCharacters( object input )
        {
            if ( input == null )
            {
                return string.Empty;
            }

            string inputString = input.ToString();

            var decomposed = inputString.Normalize( NormalizationForm.FormD );
            var filtered = decomposed.Where( c => char.GetUnicodeCategory( c ) != UnicodeCategory.NonSpacingMark );

            return new String( filtered.ToArray() );
        }
    }
}