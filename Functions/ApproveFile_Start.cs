using System.IO;
using System.Threading.Tasks;
using Docs.Approval.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Docs.Approval.Functions
{
  public class ApproveFile_Start
  {
    [FunctionName("ApproveFile_Start")]
    public static async Task HttpStart(
        [BlobTrigger("files/{id}", Connection = "StorageAccountConnectionString")] Stream fileBlob,
        string id,
        [Table("metadata", "{id}", "{id}", Connection = "TableConnectionString")] FileMetadata metadata,
        [Table("metadata", Connection = "TableConnectionString")] CloudTable metadataTable,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
      string instanceId = await starter.StartNewAsync("ProcessFileFlow", new ApprovalWorkflowData { TargetId = id });
      metadata.WorkflowId = instanceId;
      var replaceOperation = TableOperation.Replace(metadata);
      var result = await metadataTable.ExecuteAsync(replaceOperation);
      log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
      log.LogInformation("Flow started");
    }
  }
}
