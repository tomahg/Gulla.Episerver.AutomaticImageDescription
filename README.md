# Gulla.Episerver.AutomaticImageDescription

## Automatic metadata for images in Episerver
Will use Microsoft Azure Cognitive Services, Computer Vision API in combination with Translator Text API, to generate various metadata for images.


## Prerequisites
* Create an Azure Cognitive Services, Computer Vision-resource using the Azure portal.
* Add the following keys to appsettings section in web.config. Get the values from the Azure portal.  
  - Gulla.Episerver.AutomaticImageDescription:ComputerVision.SubscriptionKey
  - Gulla.Episerver.AutomaticImageDescription:ComputerVision.Endpoint

If you want to use the translation capabilities, also create an Azure Cognitive Services, Translator Text-resource using the Azure portal. Add the following keys to appsettings section in web.config. Get the values from the Azure portal.  
  - Gulla.Episerver.AutomaticImageDescription:Translator.SubscriptionKey
  - Gulla.Episerver.AutomaticImageDescription:Translator.TokenService.Endpoint

 If you will be using English-only metadata, you do not need the Translator Text-resource. 

## More information
Check out [this blog post](https://blog.novacare.no/episerver-automatic-image-metadata/).

## Get it
Grab it from this repository or install the nuget available on the [Episerver NuGet feed](https://nuget.episerver.com/package/?id=Gulla.Episerver.AutomaticImageDescription).
