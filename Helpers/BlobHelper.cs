using System;
using Microsoft.Azure.Storage.Blob;

namespace Docs.Approval.Helpers
{
  internal class BlobHelper
  {
    public static string GenerateSasUrlForFileDownload(CloudBlockBlob blob, string fileId)
    {
      var policy = new SharedAccessBlobPolicy()
      {
        SharedAccessExpiryTime = DateTime.Now.AddHours(1),
        Permissions = SharedAccessBlobPermissions.Read
      };

      return blob.Uri + blob.GetSharedAccessSignature(policy);
    }
  }
}