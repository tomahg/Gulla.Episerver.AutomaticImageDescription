using System.Collections.Generic;
using EPiServer.Shell.ObjectEditing;

namespace Gulla.Episerver.AutomaticImageDescription.Core.PropertyDefinitions
{
    public class LocalizedStringList
    {
        [SelectOne(SelectionFactoryType  = typeof(LanguageSelectionFactory))]
        public virtual string Language { get; set; }

        public virtual IList<string> Value { get; set; }
    }
}