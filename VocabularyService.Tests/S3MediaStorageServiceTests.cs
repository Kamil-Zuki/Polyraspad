using Amazon.S3;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Options;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class S3MediaStorageServiceTests
{
    [Fact]
    public async Task FillCardMediaUrlsAsync_WhenImageIdExists_ShouldOverrideStaleImageUrl()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        var options = Microsoft.Extensions.Options.Options.Create(new StorageOptions
        {
            Bucket = "polyraspad-media",
            PublicBaseUrl = "http://localhost:9000/polyraspad-media"
        });
        var sut = new S3MediaStorageService(s3.Object, options, NullLogger<S3MediaStorageService>.Instance);
        var imageId = Guid.NewGuid();
        var media = new CardMedia
        {
            ImageId = imageId,
            ImageUrl = "http://minio:9000/polyraspad-media/images/old"
        };

        await sut.FillCardMediaUrlsAsync(media);

        media.ImageUrl.Should().Be($"http://localhost:9000/polyraspad-media/images/{imageId}");
    }

    [Fact]
    public async Task FillCardMediaUrlsAsync_WhenAudioIdExists_ShouldOverrideStaleAudioUrl()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        var options = Microsoft.Extensions.Options.Options.Create(new StorageOptions
        {
            Bucket = "polyraspad-media",
            PublicBaseUrl = "http://localhost:9000/polyraspad-media"
        });
        var sut = new S3MediaStorageService(s3.Object, options, NullLogger<S3MediaStorageService>.Instance);
        var audioId = Guid.NewGuid();
        var media = new CardMedia
        {
            AudioId = audioId,
            AudioUrl = "http://minio:9000/polyraspad-media/audio/old"
        };

        await sut.FillCardMediaUrlsAsync(media);

        media.AudioUrl.Should().Be($"http://localhost:9000/polyraspad-media/audio/{audioId}");
    }
}
