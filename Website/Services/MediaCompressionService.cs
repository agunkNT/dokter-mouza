using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace DokterAbdurrahman.Services
{
    public class MediaCompressionService
    {
        public virtual Stream CompressImage(Stream inputStream)
        {
            var outputStream = new MemoryStream();
            
            using (var image = Image.Load(inputStream))
            {
                // Resize if too large (e.g., width > 1920)
                if (image.Width > 1920)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(1920, 0), // Maintain aspect ratio
                        Mode = ResizeMode.Max
                    }));
                }

                // Compress (Quality 75 is a good balance)
                var encoder = new JpegEncoder
                {
                    Quality = 75
                };

                image.Save(outputStream, encoder);
            }
            
            outputStream.Position = 0; // Reset position for reading
            return outputStream;
        }
    }
}
