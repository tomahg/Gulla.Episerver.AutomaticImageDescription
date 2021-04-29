using EPiServer.Shell.ObjectEditing;

namespace Gulla.Episerver.AutomaticImageDescription.Core.PropertyDefinitions
{
    public class LocalizedString
    {
        [SelectOne(SelectionFactoryType  = typeof(LanguageSelectionFactory))]
        public virtual string Language { get; set; }

        public virtual string Value { get; set; }
    }
}