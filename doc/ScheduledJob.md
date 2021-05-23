# Add metadata to existing images

When adding attributes to properties, those properties are populated with metadata when the image is uploaded. So what about images that were uploaded before you started using this module?

You may add the NuGet package [Gulla.Episerver.AutomaticImageDescription.ScheduledJob](https://nuget.episerver.com/package/?id=Gulla.Episerver.AutomaticImageDescription.ScheduledJob) that (surprise!) will give you a scheduled job that can generate metadata, based on the attributes, for your existing images. When the job is done, simply remove the NuGet package, to get rid of the job from admin mode.

Per default, the job will only send 20 requests to the Computer Vision API, so it's possible to [stay inside the free tier](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/computer-vision/). To adjust the throttling, add the following line to the appSettings-region of your web.config.

``` XML
<add key="Gulla.Episerver.AutomaticImageDescription:ScheduledJob.MaxRequestsPerMinute" value="20" />
```

20 is default, add value="0" to get maximum throughput.

If you have a lot of images, and think maybe the job will not finish in one go, you may add the interface `IAnalyzableImage` (included in the NuGet) to your image model. It will add a bool property `ImageAnalysisCompleted` that will be used to keep track of which images are processed, and which is not.

If you plan using the approach with the interface, add the interface to your Image content type before uploading new images. In that way, the new images will get the correct status, and changes will not be overwritten by the scheduled job.

[<< Back to readme](../README.md)