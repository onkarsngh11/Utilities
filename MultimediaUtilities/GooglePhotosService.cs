using System.Net;
using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MultimediaUtilities;

/// <summary>
/// Wrapper around CasCap.Apis.GooglePhotos.
/// Reads credentials from environment variables:
///   GOOGLE_PHOTOS_USER, GOOGLE_PHOTOS_CLIENT_ID, GOOGLE_PHOTOS_CLIENT_SECRET
/// Note: Google removed the photoslibrary.readonly scope for full library listing in March 2025
/// (replaced by the Google Photos Picker API), so listing all library items may require migrating APIs.
/// </summary>
public class GooglePhotosClientService
{
    public async Task ListMediaItemsAsync(CancellationToken cancellationToken = default)
    {
        string user = GetRequiredEnvVar("GOOGLE_PHOTOS_USER");
        string clientId = GetRequiredEnvVar("GOOGLE_PHOTOS_CLIENT_ID");
        string clientSecret = GetRequiredEnvVar("GOOGLE_PHOTOS_CLIENT_SECRET");

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<GooglePhotosService>();

        var options = new GooglePhotosOptions
        {
            User = user,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = [GooglePhotosScope.ReadOnly],
        };

        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseAddress)
        };

        var googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), client);

        if (!await googlePhotosSvc.LoginAsync())
            throw new InvalidOperationException("Google Photos login failed.");

        var mediaItems = googlePhotosSvc.GetMediaItemsAsync(1, 1, false, cancellationToken);
        await foreach (var item in mediaItems.WithCancellation(cancellationToken))
        {
            logger.LogInformation("{Id}\t{Filename}", item.id, item.filename);
        }
    }

    private static string GetRequiredEnvVar(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Required environment variable '{name}' is not set.");

        return value;
    }
}
