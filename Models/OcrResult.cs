using System;
using Microsoft.Azure.Cosmos.Table;

namespace Docs.Approval.Models
{
  public class OcrResult : TableEntity
  {
    public OcrResult(string fileId)
    {
      PartitionKey = fileId;
      RowKey = Guid.NewGuid().ToString();
    }

    public OcrResult()
    {

    }

    public string KeyName { get; set; }
    public string OcrValue { get; set; }
  }
}