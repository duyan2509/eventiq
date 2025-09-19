using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Eventiq.Application.Interfaces.Services;

namespace Eventiq.Infrastructure.Cloudinary;

public class CloudinaryStorageService:ICloudStorageService
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;

    public CloudinaryStorageService(CloudinaryDotNet.Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }


    public async Task<string?> UploadAsync(Stream fileStream, string fileName)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            PublicId = Path.GetFileNameWithoutExtension(fileName),
            Transformation = new Transformation()
                .Width(290)
                .Height(366)
                .Crop("fill")
                .Quality(80)
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        Console.WriteLine(result.SecureUri?.ToString()??"khong co");
        return result.SecureUri?.ToString();
    }

    public async Task DeleteAsync(string url)
    {
        var uri = new Uri(url);
        var segments = uri.Segments.Select(s => s.Trim('/')).ToArray();

        var uploadIndex = Array.FindIndex(segments, s => s == "upload");
        if (uploadIndex == -1 || uploadIndex + 2 > segments.Length)
            throw new ArgumentException("URL invalid: missing 'upload' or content after 'upload'");

        var publicParts = segments.Skip(uploadIndex + 2); 

        var publicPathWithExtension = string.Join("/", publicParts);

        var lastDotIndex = publicPathWithExtension.LastIndexOf('.');
        var publicId = (lastDotIndex > 0)
            ? publicPathWithExtension.Substring(0, lastDotIndex)
            : publicPathWithExtension;

        var deletionParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deletionParams);
    }
}