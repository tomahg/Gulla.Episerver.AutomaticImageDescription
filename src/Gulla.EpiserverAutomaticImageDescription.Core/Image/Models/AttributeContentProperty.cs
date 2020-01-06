using System.Reflection;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Models
{
    public class AttributeContentProperty
    {
        public BaseImageDetailsAttribute Attribute { get; set; }
        public object Content { get; set; }
        public PropertyInfo Property { get; set; }
    }
}
