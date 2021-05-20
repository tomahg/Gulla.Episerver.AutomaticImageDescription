# Gulla.Episerver.AutomaticImageDescription

## Automatic metadata for images in Episerver/Optimizely
This addon will use Microsoft Azure Cognitive Services, Computer Vision API in combination with Translator Text API, to generate various metadata for images uploaded in Episerver/Optimizely CMS.

How you use the metadata on your site is completely up to you. Some suggestions:
- Alt text for images, either directly or as a suggestion for editors to concider and adapt.
- Searchable metadata makes it easier to find the image you are looking for in Episerver edit mode.
- Use metadata to dynamically select what images to show in specific locations.

## Getting started
Visit the [Getting Started section](doc/GettingStarted.md) to learn how to install and configure the addon.

## Generating metadata when images are uploaded
Visit the [Generating Metadata section](doc/GeneratingMetadata.md) to learn all about how to generate metadata when images are uploaded.

## Generate metadata for your existing images
If your not starting from scratch you are likely to have a bunch of images prevously uploaded to the CMS.
Visit the [Scheduled job section](doc/ScheduledJob.md) to learn how you can populate metadat for your existing images.

# Use the attributes
The key to generating metadata, is adding the correct attribute
Visit the [Attributes section](doc/Attributes.md) to learn about the different attributes, and how they are used.

## Localization
If your site have more than one language, you probably want metadata in more than one language too. This can be handled in a couple of different ways.
Visit the [Localization section](doc/Localization.md) to learn more.

## More information
Check [this blog post](https://www.gulla.net/en/blog/episerver-automatic-image-metadata/).

## TL;DR
- Add the [NuGet package](https://nuget.episerver.com/package/?id=Gulla.Episerver.AutomaticImageDescription).
- Create an Azure resource
- Add the key to config
- Add an attribute, like `[AnalyzeImageForDescription]`, to a property on your image model.
- Upload an image
- Enjoy your metadata