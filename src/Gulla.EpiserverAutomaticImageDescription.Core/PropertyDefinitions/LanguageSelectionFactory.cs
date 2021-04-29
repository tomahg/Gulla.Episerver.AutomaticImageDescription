using System.Collections.Generic;
using System.Linq;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;

namespace Gulla.Episerver.AutomaticImageDescription.Core.PropertyDefinitions
{
    public class LanguageSelectionFactory : ISelectionFactory
    {
        private Injected<ILanguageBranchRepository> LanguageBranchRepository { get; set; }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            return new List<SelectItem>(LanguageBranchRepository.Service.ListEnabled().Select(c => new SelectItem { Value = c.LanguageID, Text = c.Name }));
        }
    }
}