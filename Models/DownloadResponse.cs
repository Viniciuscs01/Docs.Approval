namespace Docs.Approval.Models
{
  internal class DownloadResponse
  {
    public object Metadata { get; set; }
    public string FileId { get; set; }
    public string DownloadUrl { get; set; }
  }
}