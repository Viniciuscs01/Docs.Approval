using Microsoft.Azure.Cosmos.Table;

namespace Docs.Approval.Models
{
  public class FileMetadata : TableEntity
  {
    public string WorkflowId { get; set; }
    public bool ApprovedForAnalysis { get; set; } = false;
    public bool ApprovedForDownload { get; set; } = false;
  }
}