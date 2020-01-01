using System.Reflection;
using System.Runtime.InteropServices;
using EPiServer.Core;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image
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

        public void SetPropertyValue(object value)
        {
            Property.SetValue(_content, value);
        }

        public ImageData Image { get; }

        public PropertyInfo Property { get; }
    }
}
