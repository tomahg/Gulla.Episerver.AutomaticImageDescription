# Localization

Localization can be handled in different ways, because Episerver's base class for images `ImageData` do not implement `ILocalizable` you will have to handle the localization yourselv.

## Option 1 - separate properties
Let's say you want a property for `Description` in three languages, you could add one property for each language that will store the Description in it's resprective language. Add a `[AnalyzeImageForDescription]`-attribute to each of the three properties. Add a fourth property that simply returns the content of one of the three other properties based on `ContentLanguage.PreferredCulture.Name`.

You can add all these properties to a block type, and use this block type as a local block on your image model, to group them together.

See [this blogpost](https://www.gulla.net/en/blog/culture-specific-image-properties-in-episerver/) for code examples.

[<< Back to readme](../README.md)