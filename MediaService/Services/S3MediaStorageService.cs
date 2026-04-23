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
    private const string ReaderCollectionsPrefix = "reader-collections";
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
        var existing = books.FirstOrDefault(item => item.Id == book.Id);
        if (existing != null)
        {
            book.OwnerUserId = book.OwnerUserId == Guid.Empty ? existing.OwnerUserId : book.OwnerUserId;
            book.OwnerUserName = string.IsNullOrWhiteSpace(book.OwnerUserName) ? existing.OwnerUserName : book.OwnerUserName;
            book.OwnerEmail = string.IsNullOrWhiteSpace(book.OwnerEmail) ? existing.OwnerEmail : book.OwnerEmail;
        }
        book.OwnerUserId = userId;
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

    public async Task<IReadOnlyList<ReaderCollectionRecord>> ListReaderCollectionsAsync(Guid userId, string projectId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);
        return await LoadReaderCollectionsAsync(userId, projectId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReaderCollectionRecord> SaveReaderCollectionAsync(Guid userId, ReaderCollectionRecord collection, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        if (!string.Equals(collection.ProjectId?.Trim(), collection.ProjectId, StringComparison.Ordinal))
        {
            collection.ProjectId = collection.ProjectId.Trim();
        }

        var collections = (await LoadReaderCollectionsAsync(userId, collection.ProjectId, cancellationToken).ConfigureAwait(false)).ToList();
        var existing = collections.FirstOrDefault(item => item.Id == collection.Id);
        if (existing != null)
        {
            collection.CreatedAt = string.IsNullOrWhiteSpace(collection.CreatedAt) ? existing.CreatedAt : collection.CreatedAt;
            collection.OwnerUserName = string.IsNullOrWhiteSpace(collection.OwnerUserName) ? existing.OwnerUserName : collection.OwnerUserName;
            collection.OwnerEmail = string.IsNullOrWhiteSpace(collection.OwnerEmail) ? existing.OwnerEmail : collection.OwnerEmail;
            collection.Collaborators = collection.Collaborators.Count == 0 ? existing.Collaborators : collection.Collaborators;
        }

        collections.RemoveAll(existing => existing.Id == collection.Id);
        collections.Add(collection);
        var ordered = SortCollections(collections);

        await SaveReaderCollectionsAsync(userId, collection.ProjectId, ordered, cancellationToken).ConfigureAwait(false);
        return ordered.First(item => item.Id == collection.Id);
    }

    public async Task DeleteReaderCollectionAsync(Guid userId, string projectId, Guid collectionId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        var collections = (await LoadReaderCollectionsAsync(userId, projectId, cancellationToken).ConfigureAwait(false))
            .Where(item => item.Id != collectionId)
            .ToList();
        await SaveReaderCollectionsAsync(userId, projectId, collections, cancellationToken).ConfigureAwait(false);

        var books = (await LoadReaderLibraryBooksAsync(userId, projectId, cancellationToken).ConfigureAwait(false)).ToList();
        var updatedBooks = books
            .Select(book => book.CollectionId == collectionId.ToString()
                ? new ReaderLibraryBookRecord
                {
                    Id = book.Id,
                    DocumentId = book.DocumentId,
                    Title = book.Title,
                    FileName = book.FileName,
                    PageCount = book.PageCount,
                    UploadedAt = book.UploadedAt,
                    LastOpenedAt = book.LastOpenedAt,
                    OwnerUserId = book.OwnerUserId,
                    OwnerUserName = book.OwnerUserName,
                    OwnerEmail = book.OwnerEmail,
                    IsShared = book.IsShared
                }
                : book)
            .ToList();

        await SaveReaderLibraryBooksAsync(userId, projectId, updatedBooks, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReaderCollectionRecord> ShareReaderCollectionAsync(Guid userId, string projectId, Guid collectionId, ReaderCollectionCollaboratorRecord collaborator, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        var collections = (await LoadReaderCollectionsAsync(userId, projectId, cancellationToken).ConfigureAwait(false)).ToList();
        var collection = collections.FirstOrDefault(item => item.Id == collectionId)
            ?? throw new InvalidOperationException("Collection not found");

        collection.Collaborators.RemoveAll(item => item.UserId == collaborator.UserId);
        collection.Collaborators.Add(collaborator);
        collection.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        await SaveReaderCollectionsAsync(userId, projectId, SortCollections(collections), cancellationToken).ConfigureAwait(false);
        return collection;
    }

    public async Task<ReaderCollectionRecord> UnshareReaderCollectionAsync(Guid userId, string projectId, Guid collectionId, Guid collaboratorUserId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        var collections = (await LoadReaderCollectionsAsync(userId, projectId, cancellationToken).ConfigureAwait(false)).ToList();
        var collection = collections.FirstOrDefault(item => item.Id == collectionId)
            ?? throw new InvalidOperationException("Collection not found");

        collection.Collaborators.RemoveAll(item => item.UserId == collaboratorUserId);
        collection.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        await SaveReaderCollectionsAsync(userId, projectId, SortCollections(collections), cancellationToken).ConfigureAwait(false);
        return collection;
    }

    public async Task<IReadOnlyList<ReaderSharedCollectionRecord>> ListSharedReaderCollectionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

        var keys = await ListObjectKeysAsync($"{ReaderCollectionsPrefix}/", cancellationToken).ConfigureAwait(false);
        var results = new List<ReaderSharedCollectionRecord>();

        foreach (var key in keys.Where(key => key.EndsWith("/index.json", StringComparison.Ordinal)))
        {
            var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || !Guid.TryParse(parts[1], out var ownerUserId))
            {
                continue;
            }

            var projectId = parts[2];
            var collections = await ReadJsonAsync<List<ReaderCollectionRecord>>(key, cancellationToken).ConfigureAwait(false) ?? [];
            foreach (var collection in collections)
            {
                var access = collection.Collaborators.FirstOrDefault(item => item.UserId == userId);
                if (access == null)
                {
                    continue;
                }

                var books = (await LoadReaderLibraryBooksAsync(ownerUserId, projectId, cancellationToken).ConfigureAwait(false))
                    .Where(book => string.Equals(book.CollectionId, collection.Id.ToString(), StringComparison.OrdinalIgnoreCase))
                    .Select(book => new ReaderLibraryBookRecord
                    {
                        Id = book.Id,
                        DocumentId = book.DocumentId,
                        Title = book.Title,
                        FileName = book.FileName,
                        PageCount = book.PageCount,
                        UploadedAt = book.UploadedAt,
                        LastOpenedAt = book.LastOpenedAt,
                        CollectionId = book.CollectionId,
                        CollectionName = book.CollectionName,
                        OwnerUserId = book.OwnerUserId,
                        OwnerUserName = book.OwnerUserName,
                        OwnerEmail = book.OwnerEmail,
                        IsShared = true
                    })
                    .ToList();

                results.Add(new ReaderSharedCollectionRecord
                {
                    Collection = collection,
                    Books = SortBooks(books),
                    Access = access
                });
            }
        }

        return results
            .OrderBy(item => item.Collection.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
        var books = await ReadJsonAsync<List<ReaderLibraryBookRecord>>(GetReaderLibraryKey(userId, projectId), cancellationToken).ConfigureAwait(false) ?? [];
        foreach (var book in books)
        {
            book.OwnerUserId = book.OwnerUserId == Guid.Empty ? userId : book.OwnerUserId;
        }

        return SortBooks(books);
    }

    private async Task SaveReaderLibraryBooksAsync(Guid userId, string projectId, IReadOnlyList<ReaderLibraryBookRecord> books, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(books, JsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await UploadAsync(GetReaderLibraryKey(userId, projectId), stream, "application/json", cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ReaderCollectionRecord>> LoadReaderCollectionsAsync(Guid userId, string projectId, CancellationToken cancellationToken)
    {
        var collections = await ReadJsonAsync<List<ReaderCollectionRecord>>(GetReaderCollectionsKey(userId, projectId), cancellationToken).ConfigureAwait(false) ?? [];
        return SortCollections(collections);
    }

    private async Task SaveReaderCollectionsAsync(Guid userId, string projectId, IReadOnlyList<ReaderCollectionRecord> collections, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(collections, JsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await UploadAsync(GetReaderCollectionsKey(userId, projectId), stream, "application/json", cancellationToken).ConfigureAwait(false);
    }

    private static List<ReaderLibraryBookRecord> SortBooks(IEnumerable<ReaderLibraryBookRecord> books)
    {
        return books
            .OrderByDescending(book => ParseDate(book.LastOpenedAt) ?? ParseDate(book.UploadedAt) ?? DateTimeOffset.MinValue)
            .ThenBy(book => book.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ReaderCollectionRecord> SortCollections(IEnumerable<ReaderCollectionRecord> collections)
    {
        return collections
            .OrderBy(collection => collection.Name, StringComparer.OrdinalIgnoreCase)
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

    private static string GetReaderCollectionsKey(Guid userId, string projectId)
    {
        return $"{ReaderCollectionsPrefix}/{userId:D}/{SanitizeKeySegment(projectId)}/index.json";
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

    private async Task<T?> ReadJsonAsync<T>(string key, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _s3.GetObjectAsync(_options.Bucket, key, cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (AmazonS3Exception ex) when (IsMissingObject(ex))
        {
            return default;
        }
    }

    private async Task<IReadOnlyList<string>> ListObjectKeysAsync(string prefix, CancellationToken cancellationToken)
    {
        var results = new List<string>();
        string? continuationToken = null;

        do
        {
            var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _options.Bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            }, cancellationToken).ConfigureAwait(false);

            results.AddRange(response.S3Objects.Select(item => item.Key));
            continuationToken = response.IsTruncated ? response.NextContinuationToken : null;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        return results;
    }

    private static bool BucketAlreadyExists(AmazonS3Exception ex)
    {
        if (ex.StatusCode == HttpStatusCode.Conflict) return true;
        if (string.Equals(ex.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(ex.ErrorCode, "BucketAlreadyExists", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
