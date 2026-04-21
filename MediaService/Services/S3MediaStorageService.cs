using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using MediaService.Models;
using MediaService.Options;
using Microsoft.Extensions.Options;

namespace MediaService.Services;

public class S3MediaStorageService : IMediaStorageService
{
    private const string ImagesPrefix = "images";
    private const string AudioPrefix = "audio";
    private const string DocumentsPrefix = "documents";
    private const string ReaderLibraryPrefix = "reader-library";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;
    private readonly ILogger<S3MediaStorageService> _logger;
    private bool _bucketEnsured;

    public S3MediaStorageService(
        IAmazonS3 s3,
        IOptions<StorageOptions> options,
        ILogger<S3MediaStorageService> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Guid> UploadImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);
        var id = Guid.NewGuid();
        await UploadAsync($"{ImagesPrefix}/{id}", data, contentType, cancellationToken).ConfigureAwait(false);
        return id;
    }

    public async Task<Guid> UploadDocumentAsync(Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);
        var id = Guid.NewGuid();
        await UploadAsync($"{DocumentsPrefix}/{id}", data, contentType, cancellationToken).ConfigureAwait(false);
        return id;
    }

    public async Task<Guid> UploadAudioAsync(Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);
        var id = Guid.NewGuid();
        await UploadAsync($"{AudioPrefix}/{id}", data, contentType, cancellationToken).ConfigureAwait(false);
        return id;
    }

    public Task<string> GetMediaUrlAsync(Guid mediaId, string prefix, CancellationToken cancellationToken = default)
    {
        var key = $"{prefix}/{mediaId}";
        if (!string.IsNullOrEmpty(_options.PublicBaseUrl))
        {
            return Task.FromResult($"{_options.PublicBaseUrl.TrimEnd('/')}/{key}");
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlExpirationMinutes)
        };

        return Task.FromResult(_s3.GetPreSignedURL(request));
    }

    public Task<string> GetMediaUrlForServerFetchAsync(Guid mediaId, string prefix, CancellationToken cancellationToken = default)
    {
        var key = $"{prefix}/{mediaId}";
        var baseUrl = !string.IsNullOrEmpty(_options.ServerFetchBaseUrl)
            ? _options.ServerFetchBaseUrl.TrimEnd('/')
            : !string.IsNullOrEmpty(_options.PublicBaseUrl)
                ? _options.PublicBaseUrl.TrimEnd('/')
                : null;

        if (baseUrl != null)
        {
            return Task.FromResult($"{baseUrl}/{key}");
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlExpirationMinutes)
        };

        return Task.FromResult(_s3.GetPreSignedURL(request));
    }

    public async Task<IReadOnlyList<ReaderLibraryBookRecord>> ListReaderLibraryBooksAsync(Guid userId, string projectId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);
        return await LoadReaderLibraryBooksAsync(userId, projectId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReaderLibraryBookRecord> SaveReaderLibraryBookAsync(Guid userId, string projectId, ReaderLibraryBookRecord book, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        var books = (await LoadReaderLibraryBooksAsync(userId, projectId, cancellationToken).ConfigureAwait(false)).ToList();
        books.RemoveAll(existing => existing.Id == book.Id);
        books.Add(book);
        var ordered = SortBooks(books);

        await SaveReaderLibraryBooksAsync(userId, projectId, ordered, cancellationToken).ConfigureAwait(false);
        return ordered.First(item => item.Id == book.Id);
    }

    public async Task DeleteReaderLibraryBookAsync(Guid userId, string projectId, Guid bookId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        var books = (await LoadReaderLibraryBooksAsync(userId, projectId, cancellationToken).ConfigureAwait(false))
            .Where(item => item.Id != bookId)
            .ToList();

        await SaveReaderLibraryBooksAsync(userId, projectId, books, cancellationToken).ConfigureAwait(false);
    }

    private async Task UploadAsync(string key, Stream data, string contentType, CancellationToken cancellationToken)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = data,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Uploaded media object to {Key}", key);
    }

    private async Task<IReadOnlyList<ReaderLibraryBookRecord>> LoadReaderLibraryBooksAsync(Guid userId, string projectId, CancellationToken cancellationToken)
    {
        var key = GetReaderLibraryKey(userId, projectId);

        try
        {
            using var response = await _s3.GetObjectAsync(_options.Bucket, key, cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var books = JsonSerializer.Deserialize<List<ReaderLibraryBookRecord>>(json, JsonOptions) ?? [];
            return SortBooks(books);
        }
        catch (AmazonS3Exception ex) when (IsMissingObject(ex))
        {
            return [];
        }
    }

    private async Task SaveReaderLibraryBooksAsync(Guid userId, string projectId, IReadOnlyList<ReaderLibraryBookRecord> books, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(books, JsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await UploadAsync(GetReaderLibraryKey(userId, projectId), stream, "application/json", cancellationToken).ConfigureAwait(false);
    }

    private static List<ReaderLibraryBookRecord> SortBooks(IEnumerable<ReaderLibraryBookRecord> books)
    {
        return books
            .OrderByDescending(book => ParseDate(book.LastOpenedAt) ?? ParseDate(book.UploadedAt) ?? DateTimeOffset.MinValue)
            .ThenBy(book => book.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool IsMissingObject(AmazonS3Exception ex)
    {
        return ex.StatusCode == HttpStatusCode.NotFound
            || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetReaderLibraryKey(Guid userId, string projectId)
    {
        return $"{ReaderLibraryPrefix}/{userId:D}/{SanitizeKeySegment(projectId)}/index.json";
    }

    private static string SanitizeKeySegment(string value)
    {
        return value.Trim().Replace("/", "_", StringComparison.Ordinal).Replace("\\", "_", StringComparison.Ordinal);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured) return;

        try
        {
            try
            {
                await _s3.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _options.Bucket
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception ex) when (BucketAlreadyExists(ex))
            {
            }

            _bucketEnsured = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure bucket {Bucket}", _options.Bucket);
            throw;
        }
    }

    private static bool BucketAlreadyExists(AmazonS3Exception ex)
    {
        if (ex.StatusCode == HttpStatusCode.Conflict) return true;
        if (string.Equals(ex.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(ex.ErrorCode, "BucketAlreadyExists", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
