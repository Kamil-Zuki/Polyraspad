using Amazon.S3;
using Grpc.Core;
using MediaService.Models;
using MediaService.Services;
using Pvs.Media.Grpc;
using static Pvs.Media.Grpc.MediaService;

namespace MediaService.Grpc;

public class MediaGrpcService : MediaServiceBase
{
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;
    private const int MaxDocumentSizeBytes = 50 * 1024 * 1024;

    private readonly IMediaStorageService _mediaStorage;
    private readonly ILogger<MediaGrpcService> _logger;

    public MediaGrpcService(IMediaStorageService mediaStorage, ILogger<MediaGrpcService> logger)
    {
        _mediaStorage = mediaStorage;
        _logger = logger;
    }

    public override async Task<UploadImageResponse> UploadImage(UploadImageRequest request, ServerCallContext context)
    {
        if (request.ImageData == null || request.ImageData.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Image data is required"));
        }

        if (request.ImageData.Length > MaxImageSizeBytes)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Image size must not exceed 5 MB"));
        }

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "image/png" : request.ContentType;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content type must be image/*"));
        }

        try
        {
            await using var stream = new MemoryStream(request.ImageData.ToByteArray());
            var imageId = await _mediaStorage.UploadImageAsync(stream, contentType, context.CancellationToken).ConfigureAwait(false);
            var url = await _mediaStorage.GetMediaUrlAsync(imageId, "images", context.CancellationToken).ConfigureAwait(false);
            return new UploadImageResponse { Url = url, ImageId = imageId.ToString() };
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading image");
            throw new RpcException(new Status(StatusCode.Unavailable, $"Media storage error: {ex.Message}"));
        }
    }

    public override async Task<UploadAudioResponse> UploadAudio(UploadAudioRequest request, ServerCallContext context)
    {
        if (request.AudioData == null || request.AudioData.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Audio data is required"));
        }

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "audio/mpeg" : request.ContentType;
        if (!contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content type must be audio/*"));
        }

        try
        {
            await using var stream = new MemoryStream(request.AudioData.ToByteArray());
            var audioId = await _mediaStorage.UploadAudioAsync(stream, contentType, context.CancellationToken).ConfigureAwait(false);
            var url = await _mediaStorage.GetMediaUrlAsync(audioId, "audio", context.CancellationToken).ConfigureAwait(false);
            return new UploadAudioResponse { Url = url, AudioId = audioId.ToString() };
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading audio");
            throw new RpcException(new Status(StatusCode.Unavailable, $"Media storage error: {ex.Message}"));
        }
    }

    public override async Task<UploadDocumentResponse> UploadDocument(UploadDocumentRequest request, ServerCallContext context)
    {
        if (request.DocumentData == null || request.DocumentData.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Document data is required"));
        }

        if (request.DocumentData.Length > MaxDocumentSizeBytes)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Document size must not exceed 50 MB"));
        }

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/pdf" : request.ContentType;
        var fileName = request.FileName ?? string.Empty;
        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Only PDF documents are supported"));
        }

        try
        {
            await using var stream = new MemoryStream(request.DocumentData.ToByteArray());
            var documentId = await _mediaStorage.UploadDocumentAsync(stream, "application/pdf", context.CancellationToken).ConfigureAwait(false);
            var url = await _mediaStorage.GetMediaUrlAsync(documentId, "documents", context.CancellationToken).ConfigureAwait(false);
            return new UploadDocumentResponse { Url = url, DocumentId = documentId.ToString() };
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading document");
            throw new RpcException(new Status(StatusCode.Unavailable, $"Media storage error: {ex.Message}"));
        }
    }

    public override async Task<GetImageUrlResponse> GetImageUrl(GetImageUrlRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.ImageId) || !Guid.TryParse(request.ImageId, out var imageId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid image_id (UUID) is required"));
        }

        var url = await _mediaStorage.GetMediaUrlForServerFetchAsync(imageId, "images", context.CancellationToken).ConfigureAwait(false);
        return new GetImageUrlResponse { Url = url };
    }

    public override async Task<GetDocumentUrlResponse> GetDocumentUrl(GetDocumentUrlRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentId) || !Guid.TryParse(request.DocumentId, out var documentId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid document_id (UUID) is required"));
        }

        var url = await _mediaStorage.GetMediaUrlForServerFetchAsync(documentId, "documents", context.CancellationToken).ConfigureAwait(false);
        return new GetDocumentUrlResponse { Url = url };
    }

    public override async Task<GetAudioUrlResponse> GetAudioUrl(GetAudioUrlRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.AudioId) || !Guid.TryParse(request.AudioId, out var audioId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid audio_id (UUID) is required"));
        }

        var url = await _mediaStorage.GetMediaUrlForServerFetchAsync(audioId, "audio", context.CancellationToken).ConfigureAwait(false);
        return new GetAudioUrlResponse { Url = url };
    }

    public override async Task<ListReaderLibraryBooksResponse> ListReaderLibraryBooks(ListReaderLibraryBooksRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var projectId = GetRequiredProjectId(request.ProjectId);
        var books = await _mediaStorage.ListReaderLibraryBooksAsync(userId, projectId, context.CancellationToken).ConfigureAwait(false);

        var response = new ListReaderLibraryBooksResponse();
        response.Books.AddRange(await MapBooksAsync(books, context.CancellationToken).ConfigureAwait(false));
        return response;
    }

    public override async Task<SaveReaderLibraryBookResponse> SaveReaderLibraryBook(SaveReaderLibraryBookRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var projectId = GetRequiredProjectId(request.ProjectId);

        if (request.Book == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Book payload is required"));
        }

        var saved = await _mediaStorage.SaveReaderLibraryBookAsync(
            userId,
            projectId,
            MapBookRecord(request.Book),
            context.CancellationToken).ConfigureAwait(false);

        return new SaveReaderLibraryBookResponse
        {
            Book = await MapBookAsync(saved, context.CancellationToken).ConfigureAwait(false)
        };
    }

    public override async Task<DeleteReaderLibraryBookResponse> DeleteReaderLibraryBook(DeleteReaderLibraryBookRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var projectId = GetRequiredProjectId(request.ProjectId);

        if (string.IsNullOrWhiteSpace(request.BookId) || !Guid.TryParse(request.BookId, out var bookId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid book_id (UUID) is required"));
        }

        await _mediaStorage.DeleteReaderLibraryBookAsync(userId, projectId, bookId, context.CancellationToken).ConfigureAwait(false);
        return new DeleteReaderLibraryBookResponse();
    }

    private Guid GetRequiredUserId(ServerCallContext context)
    {
        var rawUserId = context.RequestHeaders.GetValue("user_id");
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Valid user_id header is required"));
        }

        return userId;
    }

    private static string GetRequiredProjectId(string? projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "project_id is required"));
        }

        return projectId.Trim();
    }

    private static ReaderLibraryBookRecord MapBookRecord(ReaderLibraryBook book)
    {
        if (string.IsNullOrWhiteSpace(book.Id) || !Guid.TryParse(book.Id, out var bookId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid book.id (UUID) is required"));
        }

        Guid? documentId = null;
        if (!string.IsNullOrWhiteSpace(book.DocumentId))
        {
            if (!Guid.TryParse(book.DocumentId, out var parsedDocumentId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "book.document_id must be a UUID when provided"));
            }

            documentId = parsedDocumentId;
        }

        if (string.IsNullOrWhiteSpace(book.Title))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "book.title is required"));
        }

        if (string.IsNullOrWhiteSpace(book.FileName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "book.file_name is required"));
        }

        return new ReaderLibraryBookRecord
        {
            Id = bookId,
            DocumentId = documentId,
            Title = book.Title.Trim(),
            FileName = book.FileName.Trim(),
            PageCount = book.PageCount > 0 ? book.PageCount : null,
            UploadedAt = string.IsNullOrWhiteSpace(book.UploadedAt) ? DateTimeOffset.UtcNow.ToString("O") : book.UploadedAt,
            LastOpenedAt = string.IsNullOrWhiteSpace(book.LastOpenedAt) ? null : book.LastOpenedAt,
            CollectionId = string.IsNullOrWhiteSpace(book.CollectionId) ? null : book.CollectionId.Trim(),
            CollectionName = string.IsNullOrWhiteSpace(book.CollectionName) ? null : book.CollectionName.Trim()
        };
    }

    private async Task<IReadOnlyList<ReaderLibraryBook>> MapBooksAsync(IEnumerable<ReaderLibraryBookRecord> books, CancellationToken cancellationToken)
    {
        var list = new List<ReaderLibraryBook>();
        foreach (var book in books)
        {
            list.Add(await MapBookAsync(book, cancellationToken).ConfigureAwait(false));
        }

        return list;
    }

    private async Task<ReaderLibraryBook> MapBookAsync(ReaderLibraryBookRecord book, CancellationToken cancellationToken)
    {
        string url = string.Empty;
        if (book.DocumentId.HasValue)
        {
            url = await _mediaStorage.GetMediaUrlForServerFetchAsync(book.DocumentId.Value, "documents", cancellationToken).ConfigureAwait(false);
        }

        return new ReaderLibraryBook
        {
            Id = book.Id.ToString(),
            Title = book.Title,
            FileName = book.FileName,
            Url = url,
            DocumentId = book.DocumentId?.ToString() ?? string.Empty,
            PageCount = book.PageCount ?? 0,
            UploadedAt = book.UploadedAt,
            LastOpenedAt = book.LastOpenedAt ?? string.Empty,
            CollectionId = book.CollectionId ?? string.Empty,
            CollectionName = book.CollectionName ?? string.Empty
        };
    }
}
