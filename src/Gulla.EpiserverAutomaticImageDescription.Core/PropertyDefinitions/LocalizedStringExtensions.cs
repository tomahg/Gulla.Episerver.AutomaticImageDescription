using System.Collections.Generic;
using System.Linq;
using EPiServer.Globalization;

namespace Gulla.Episerver.AutomaticImageDescription.Core.PropertyDefinitions
{
    public static class LocalizedStringExtensions
    {
        public static string GetPreferredCulture(this IList<LocalizedString> localizedStrings)
        {
            if (localizedStrings == null || localizedStrings.Count == 0)
            {
                return "";
            }
            return localizedStrings.FirstOrDefault(x => x.Language == ContentLanguage.PreferredCulture.Name)?.Value;
        }

        public static string GetPreferredCulture(this IList<LocalizedString> localizedStrings, string fallbackLanguage)
        {
            var valueInPreferredLanguage = localizedStrings.GetPreferredCulture();
            return !string.IsNullOrEmpty(valueInPreferredLanguage) ? valueInPreferredLanguage : localizedStrings.FirstOrDefault(x => x.Language == fallbackLanguage)?.Value;
        }

        public static IList<string> GetPreferredCulture(this IList<LocalizedStringList> localizedStrings)
        {
            if (localizedStrings == null || localizedStrings.Count == 0)
            {
                return new List<string>();
            }
            return localizedStrings.FirstOrDefault(x => x.Language == ContentLanguage.PreferredCulture.Name)?.Value;
        }

        public static IList<string> GetPreferredCulture(this IList<LocalizedStringList> localizedStrings, string fallbackLanguage)
        {
            var valueInPreferredLanguage = localizedStrings.GetPreferredCulture();
            return valueInPreferredLanguage?.Any() == true ? valueInPreferredLanguage : localizedStrings.FirstOrDefault(x => x.Language == fallbackLanguage)?.Value;
        }
    }
}