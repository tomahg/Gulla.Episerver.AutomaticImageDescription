using System.Reflection;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Models
{
    public class ContentProperty
    {
        public object Content { get; set; }
        public PropertyInfo Property { get; set; }
    }
}
