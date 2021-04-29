using System.Collections.Generic;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Gulla.Episerver.AutomaticImageDescription.Core.PropertyDefinitions
{
    [EditorDescriptorRegistration(TargetType = typeof(IList<LocalizedStringList>))]
    public class PropertyLocalizedStringListEditorDescriptor : CollectionEditorDescriptor<LocalizedStringList>
    {

    }
}