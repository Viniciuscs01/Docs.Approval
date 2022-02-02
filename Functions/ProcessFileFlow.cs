using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Docs.Approval.Helpers;
using Docs.Approval.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using OcrResult = Docs.Approval.Models.OcrResult;

namespace Docs.Approval.Functions
{
  public static class ProcessFileFlow
  {
    public static object OcrType { get; private set; }

    [FunctionName("ProcessFileFlow")]
    public static async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
      var input = context.GetInput<ApprovalWorkflowData>();

      var uploadApprovedEvent = context.WaitForExternalEvent<bool>("UploadApproved");
      await Task.WhenAny(uploadApprovedEvent);

      var ocrProcessTask = context.CallActivityAsync<bool>(nameof(ProcessFileFlow.ProcessFile), input.TargetId);
      await Task.WhenAny(ocrProcessTask);
    }

    [FunctionName("ProcessFile")]
    public static async Task ProcessFile(
        [ActivityTrigger] string fileId,
        [Blob("files/{fileId}", FileAccess.Read, Connection = "StorageAccountConnectionString")] CloudBlockBlob fileBlob,
        [Table("ocrdata", Connection = "TableConnectionString")] CloudTable ocrDataTable,
        ILogger log)
    {
      try
      {

        var compVisionClient = Authenticate(Environment.GetEnvironmentVariable("CognitiveServicesEndpoint"), Environment.GetEnvironmentVariable("CognitiveServicesKey"));
        var results = await AnalyzeImageUrl(compVisionClient, BlobHelper.GenerateSasUrlForFileDownload(fileBlob, fileId));

        var batchOperation = new TableBatchOperation();

        foreach (var caption in results.Description.Captions)
          batchOperation.Insert(new OcrResult(fileId) { KeyName = nameof(results.Description.Captions), OcrValue = caption.Text });

        foreach (var category in results.Categories)
          batchOperation.Insert(new OcrResult(fileId) { KeyName = category.Name, OcrValue = category.Score.ToString() });

        foreach (var obj in results.Objects)
          batchOperation.Insert(new OcrResult(fileId) { KeyName = obj.ObjectProperty, OcrValue = obj.Confidence.ToString() });

        foreach (var brand in results.Brands)
          batchOperation.Insert(new OcrResult(fileId) { KeyName = brand.Name, OcrValue = brand.Confidence.ToString() });

        foreach (var face in results.Faces)
          batchOperation.Insert(new OcrResult(fileId) { KeyName = face.Gender.ToString(), OcrValue = face.Age.ToString() });

        ocrDataTable.ExecuteBatch(batchOperation);
      }
      catch (Exception ex)
      {
        log.LogError(ex.Message);
      }
    }

    public static ComputerVisionClient Authenticate(string endpoint, string key)
    {
      ComputerVisionClient client =
        new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
        { Endpoint = endpoint };
      return client;
    }

    public static async Task<ImageAnalysis> AnalyzeImageUrl(ComputerVisionClient client, string imageUrl)
    {
      List<VisualFeatureTypes> features = new List<VisualFeatureTypes>()
      {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
            VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
            VisualFeatureTypes.Objects
      };

      return await client.AnalyzeImageAsync(imageUrl, visualFeatures: features);
    }
  }
}