using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace YouView.Services
{
    public class BlobService
    {
        private readonly string _connectionString;
        private readonly string _profileContainerName;

        public BlobService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"];
            _profileContainerName = configuration["AzureStorage:ProfileContainer"];

            // Fallback safety check
            if (string.IsNullOrEmpty(_profileContainerName))
            {
                _profileContainerName = "profiles"; 
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            // Rename for safety (GUID + Extension)
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            // Connect
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_profileContainerName);
            
            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync();
            await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);

            // Upload
            var blobClient = containerClient.GetBlobClient(fileName);
            await using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            try 
            {
                var uri = new Uri(fileUrl);
                var fileName = Path.GetFileName(uri.LocalPath);

                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_profileContainerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.DeleteIfExistsAsync();
            }
            catch
            {
                // Ignore errors if file is already gone
            }
        }
    }
}