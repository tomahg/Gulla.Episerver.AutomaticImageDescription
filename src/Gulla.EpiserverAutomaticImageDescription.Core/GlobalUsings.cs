// Resolve the ReadResult name clash between the new Azure.AI.Vision.ImageAnalysis SDK
// and the legacy Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models type.
// The OCR pipeline uses the new SDK's ReadResult.
global using ReadResult = Azure.AI.Vision.ImageAnalysis.ReadResult;
