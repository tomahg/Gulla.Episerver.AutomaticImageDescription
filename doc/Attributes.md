# Attributes

In order to generate metadata, you decorate properties on your image model with various attributes. Different types of metadata, requires different attributes. Follow the links below to read more about the different attributes, the metadata they produce, and how they are used.

- [Adult content](./attributes/AnalyzeImageForAdultContent.md)
- [Brands](./attributes/AnalyzeImageForBrands.md)
- [Celebrities](./attributes/AnalyzeImageForCelebrities.md)
- [Description, suitable for alt-text](./attributes/AnalyzeImageForDescription.md)
- [Faces](./attributes/AnalyzeImageForFaces.md)
- [Gory content](./attributes/AnalyzeImageForGoryContent.md)
- [Landmarks](./attributes/AnalyzeImageForLandmarks.md)
- [OCR](./attributes/AnalyzeImageForOcr.md)
- [Racy content](./attributes/AnalyzeImageForRacyContent.md)
- [Tags](./attributes/AnalyzeImageForTags.md)

If none of these attributes suits your needs, you may [create your own attribute](./attributes/CustomAttribute.md).

## Add attributes to image model

You can add properties, and attributes, direcly to the image model like this.
``` C#
namespace Alloy.Models.Media
{
    [ContentType]
    [MediaDescriptor(ExtensionString = "jpg,jpeg,jpe,ico,gif,bmp,png")]
    public class ImageFile : ImageData
    {
      [AnalyzeImageForDescription]
      public virtual string Description { get; set; }
    }
}
```

## Add attributes to local block

If you want to group multiple properties together, you can use local block. The attributes will work for those too.

First create your local block like this.

``` C#
namespace Alloy.Models.Blocks
{
    [ContentType(AvailableInEditMode = false)]
    public class DescriptionBlock : BlockData
    {
        [AnalyzeImageForDescription]
        public virtual string English { get; set; }

        [AnalyzeImageForDescription(TranslationLanguage.Norwegian)]
        public virtual string Norwegian { get; set; }
    }
}
```

Then add the block type to your image model as a local block, like this.

``` C#
namespace Alloy.Models.Media
{
    [ContentType]
    [MediaDescriptor(ExtensionString = "jpg,jpeg,jpe,ico,gif,bmp,png")]
    public class ImageFile : ImageData
    {
      public virtual DescriptionBlock Description { get; set; }
    }
}
```