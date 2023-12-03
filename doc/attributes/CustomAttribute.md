# Creating your own attributes
New attributes may be created inheriting from the abstract base class `BaseImageDetailsAttribute`. You must then override one or more of the following attributes, to indicate what kind of resources your attribute needs.

``` C#
/// <summary>
/// Flag if the Update method needs imageAnalyzerResult to be populated.
/// </summary>
public virtual bool AnalyzeImageContent => false;

/// <summary>
/// Flag if the Update method needs ocrResult to be populated.
/// </summary>
public virtual bool AnalyzeImageOcr => false;

/// <summary>
/// Flag if the Update method needs translationService to be populated.
/// </summary>
public virtual bool RequireTranslations => false;
```

If you rely on data from the Computer Vision API, override `AnalyzeImageContent` and return true. If you rely on OCR data, override `AnalyzeImageOcr` and return true. If you are using the Translation API, override `RequireTranslations` and return true.

Finally implement the `Update` method, where you inspect the image analysis result and update the property decorated with your attribute.

## An example implementation of a custom attribute
This attribute will analyze the image and check if it's a Clip Art. The attribute may be added to three different property types.

``` C#
/// <summary>
/// Analyze the content type of images, indicating whether an image is clip art.
/// Apply to bool properties for true/false.
/// Apply to int properties for the likelihood of the image being clip art on a scale of 0 to 3.
/// Apply to string propertyes for a textual representation of the score.
/// </summary>
public class AnalyzeImageForClipArtAttribute : BaseImageDetailsAttribute
{
    public override bool AnalyzeImageContent => true;

    public override void Update(
        PropertyAccess propertyAccess,
        ImageAnalysis imageAnalyzerResult,
        OcrResult ocrResult,
        TranslationService translationService)
    {
        if (imageAnalyzerResult.ImageType == null)
        {
            return;
        }

        if (IsBooleanProperty(propertyAccess.Property))
        {
            propertyAccess.SetValue(imageAnalyzerResult.ImageType.ClipArtType > 0);
        }
        else if (IsIntProperty(propertyAccess.Property))
        {
            propertyAccess.SetValue(imageAnalyzerResult.ImageType.ClipArtType);
        }
        else if (IsStringProperty(propertyAccess.Property))
        {
            var clipartType = imageAnalyzerResult.ImageType.ClipArtType switch
            {
                1 => "Ambiguous",
                2 => "Normal-clip-art",
                3 => "Good-clip-art",
                _ => "Non-clip-art",
            };

            propertyAccess.SetValue(clipartType);
        }
    }
}
```

**Example**
``` C#
public class ClipArtBlock : BlockData
{
    [AnalyzeImageForClipArt]
    public virtual bool IsClipArt { get; set; }

    [AnalyzeImageForClipArt]
    public virtual int ClipArtNumber { get; set; }

    [AnalyzeImageForClipArt]
    public virtual string ClipArtType { get; set; }
}
```
![ClipArt](./img/ClipArt.jpg)
[<< Back to list of attributes](../Attributes.md)