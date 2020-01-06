using System.Reflection;
using EPiServer.Core;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image
{
    public class PropertyAccess
    {
        private readonly object _content;

        public PropertyAccess(ImageData image, object content, PropertyInfo propertyInfo)
        {
            _content = content;
            Image = image;
            Property = propertyInfo;
        }

        public void SetValue(object value)
        {
            Property.SetValue(_content, value);
        }

        public object GetValue()
        {
            return Property.GetValue(_content);
        }

        public ImageData Image { get; }

        public PropertyInfo Property { get; }
    }
}
