using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace DokterAbdurrahman.Services
{
    /// <summary>
    /// Service untuk memproses dan menyimpan media ke database (bytea).
    /// Gambar dikompres sebelum disimpan.
    /// </summary>
    public class LocalMediaService
    {
        private readonly ILogger<LocalMediaService> _logger;

        public LocalMediaService(ILogger<LocalMediaService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Kompres foto dan kembalikan byte[] siap simpan ke DB
        /// </summary>
        public async Task<(byte[] Data, string ContentType, string FileName)> ProcessFoto(IFormFile file)
        {
            using var inputStream = file.OpenReadStream();
            using var compressed = CompressImage(inputStream);
            var data = compressed.ToArray();
            _logger.LogInformation("Processed foto: {Name} -> {Size} bytes", file.FileName, data.Length);
            return (data, "image/jpeg", Path.GetFileNameWithoutExtension(file.FileName) + ".jpg");
        }

        /// <summary>
        /// Baca video dan kembalikan byte[] siap simpan ke DB
        /// </summary>
        public async Task<(byte[] Data, string ContentType, string FileName)> ProcessVideo(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();
            var contentType = file.ContentType ?? "video/mp4";
            _logger.LogInformation("Processed video: {Name} -> {Size} bytes", file.FileName, data.Length);
            return (data, contentType, file.FileName);
        }

        private static MemoryStream CompressImage(Stream input)
        {
            var output = new MemoryStream();
            using var image = Image.Load(input);
            if (image.Width > 1920)
            {
                image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(1920, 0),
                    Mode = ResizeMode.Max
                }));
            }
            image.Save(output, new JpegEncoder { Quality = 78 });
            output.Position = 0;
            return output;
        }
    }
}
