namespace Eventiq.Application.Interfaces.Services;
public interface ICloudStorageService
{
    Task<string?> UploadAsync(Stream fileStream, string fileName);
    Task<string?> UploadAsync(Stream fileStream, string fileName, int? width, int? height, string? crop);
    Task DeleteAsync(string publicId);
}