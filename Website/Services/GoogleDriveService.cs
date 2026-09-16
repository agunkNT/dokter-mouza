using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2.Flows;
using System.Text.Json;

namespace DokterAbdurrahman.Services
{
    public class GoogleDriveService
    {
        private const string ApplicationName = "DokterAbdurrahman";
        private static readonly string[] Scopes = { DriveService.Scope.Drive };

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GoogleDriveService> _logger;

        public GoogleDriveService(IWebHostEnvironment env, ILogger<GoogleDriveService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public DriveService GetService()
        {
            // Cari service-account.json di project root
            var serviceAccountPath = Path.Combine(_env.ContentRootPath, "service-account.json");

            if (File.Exists(serviceAccountPath))
            {
                // ✅ MODE: Service Account (untuk production/server, tidak perlu login browser)
                _logger.LogInformation("Using Service Account authentication for Google Drive.");
                using var stream = new FileStream(serviceAccountPath, FileMode.Open, FileAccess.Read);
                var json = JsonDocument.Parse(stream);
                var email = json.RootElement.GetProperty("client_email").GetString()!;
                var privateKey = json.RootElement.GetProperty("private_key").GetString()!;

                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(email)
                    {
                        Scopes = Scopes
                    }.FromPrivateKey(privateKey));

                return new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
            }

            // ⚠️ FALLBACK: OAuth2 interaktif (hanya untuk lokal/development)
            _logger.LogWarning("service-account.json not found. Falling back to OAuth2 (browser flow). This only works on local machine.");
            var credentialsPath = Path.Combine(_env.ContentRootPath, "credentials.json");
            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    "Tidak ditemukan 'service-account.json' maupun 'credentials.json'. " +
                    "Taruh salah satu di folder project untuk mengaktifkan Google Drive upload.");
            }

            using var credStream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            var userCredential = Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(
                Google.Apis.Auth.OAuth2.GoogleClientSecrets.FromStream(credStream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new Google.Apis.Util.Store.FileDataStore("token.json", true)).Result;

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = userCredential,
                ApplicationName = ApplicationName,
            });
        }

        public virtual Google.Apis.Drive.v3.Data.File UploadFile(IFormFile file, string? parentFolderId = null)
        {
            using var stream = file.OpenReadStream();
            return UploadFile(stream, file.FileName, file.ContentType, parentFolderId);
        }

        public virtual Google.Apis.Drive.v3.Data.File UploadFile(Stream stream, string fileName, string contentType, string? parentFolderId = null)
        {
            var service = GetService();

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName
            };

            if (!string.IsNullOrEmpty(parentFolderId))
            {
                fileMetadata.Parents = new List<string> { parentFolderId };
            }

            var request = service.Files.Create(fileMetadata, stream, contentType);
            request.Fields = "id, name, webViewLink, webContentLink";
            var uploadResult = request.Upload();

            if (uploadResult.Status == Google.Apis.Upload.UploadStatus.Failed)
            {
                _logger.LogError(uploadResult.Exception, "Google Drive upload failed for file: {FileName}", fileName);
                throw new Exception($"Upload gagal untuk file {fileName}: {uploadResult.Exception?.Message}");
            }

            return request.ResponseBody;
        }

        public virtual string CreateFolder(string folderName, string? parentFolderId = null)
        {
            var service = GetService();
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            if (!string.IsNullOrEmpty(parentFolderId))
            {
                fileMetadata.Parents = new List<string> { parentFolderId };
            }

            var request = service.Files.Create(fileMetadata);
            request.Fields = "id";
            var file = request.Execute();
            return file.Id;
        }

        public virtual string? FindFolder(string folderName, string? parentFolderId = null)
        {
            var service = GetService();
            var request = service.Files.List();

            var query = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and trashed = false";
            if (!string.IsNullOrEmpty(parentFolderId))
            {
                query += $" and '{parentFolderId}' in parents";
            }

            request.Q = query;
            request.Fields = "files(id, name)";
            var result = request.Execute();

            return result.Files.FirstOrDefault()?.Id;
        }

        public virtual IList<Google.Apis.Drive.v3.Data.File> ListFiles(string? folderId = null)
        {
            var service = GetService();
            var request = service.Files.List();

            var query = "(mimeType contains 'image/' or mimeType contains 'video/') and trashed = false";
            if (!string.IsNullOrEmpty(folderId))
            {
                query += $" and '{folderId}' in parents";
            }

            request.Q = query;
            request.Fields = "nextPageToken, files(id, name, mimeType, webContentLink, webViewLink)";

            return request.Execute().Files;
        }

        /// <summary>
        /// Membuat permission publik agar file bisa dilihat via webContentLink
        /// </summary>
        public virtual void MakeFilePublic(string fileId)
        {
            try
            {
                var service = GetService();
                var permission = new Google.Apis.Drive.v3.Data.Permission
                {
                    Type = "anyone",
                    Role = "reader"
                };
                service.Permissions.Create(permission, fileId).Execute();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal membuat file publik: {FileId}", fileId);
            }
        }

        /// <summary>
        /// Mendapatkan direct thumbnail/view URL dari Google Drive file ID
        /// </summary>
        public static string GetThumbnailUrl(string fileId)
            => $"https://drive.google.com/thumbnail?id={fileId}&sz=w400";

        public static string GetViewUrl(string fileId)
            => $"https://drive.google.com/file/d/{fileId}/view";
    }
}
