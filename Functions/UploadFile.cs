using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Cosmos.Table;
using Docs.Approval.Models;

namespace Docs.Approval.Functions
{
  public static class UploadFile
  {
    [FunctionName("UploadFile")]
    public static async Task<IActionResult> Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "file/add")] HttpRequest uploadFile,
      [Blob("files", FileAccess.Write, Connection = "StorageAccountConnectionString")] CloudBlobContainer blobContainer,
      [Table("metadata", Connection = "TableConnectionString")] CloudTable metadataTable,
      ILogger log)
    {
      log.LogInformation("Iniciando Upload");
      var fileName = Guid.NewGuid().ToString();
      await blobContainer.CreateIfNotExistsAsync();
      var cloudBlockBlob = blobContainer.GetBlockBlobReference(fileName);
      await cloudBlockBlob.UploadFromStreamAsync(uploadFile.Body);
      await metadataTable.CreateIfNotExistsAsync();
      var addOperation = TableOperation.Insert(new FileMetadata
      {
        RowKey = fileName,
        PartitionKey = fileName,
      });
      await metadataTable.ExecuteAsync(addOperation);
      log.LogInformation("Upload Finalizado");
      return new CreatedResult(string.Empty, fileName);
    }
  }
}
