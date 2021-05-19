# [AnalyzeImageForLandmarks]
This attribute will try to identify and name [landmarks](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/concept-detecting-domain-content) present in the image.

May be added to the following property types:

- **String:** A comma separated list of landmarks.
- **IList&lt;string&gt;:** A list of landmarks.

**Example**
``` C#
public class LandmarkBlock : BlockData
{
    [AnalyzeImageForLandmarks]
    public virtual string String { get; set; }

    [AnalyzeImageForLandmarks]
    public virtual IList<string> StringList { get; set; }
}
```
![Brands](./img/Landmarks.jpg)