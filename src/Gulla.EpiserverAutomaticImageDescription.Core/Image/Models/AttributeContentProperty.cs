using System.Reflection;
using Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Models
{
    public class AttributeContentProperty
    {
        public BaseImageDetailsAttribute Attribute { get; set; }
        public object Content { get; set; }
        public PropertyInfo Property { get; set; }
    }
}
