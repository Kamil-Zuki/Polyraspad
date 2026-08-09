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

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/pdf" : request.ContentType.Trim();
        var fileName = request.FileName ?? string.Empty;
        if (!TryNormalizeReaderDocumentContentType(contentType, fileName, out var normalizedContentType))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported document type (use PDF, EPUB, or plain text)"));
        }

        try
        {
            await using var stream = new MemoryStream(request.DocumentData.ToByteArray());
            var documentId = await _mediaStorage.UploadDocumentAsync(stream, normalizedContentType, context.CancellationToken).ConfigureAwait(false);
            var url = await _mediaStorage.GetMediaUrlAsync(documentId, "documents", context.CancellationToken).ConfigureAwait(false);
            return new UploadDocumentResponse { Url = url, DocumentId = documentId.ToString() };
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading document");
            throw new RpcException(new Status(StatusCode.Unavailable, $"Media storage error: {ex.Message}"));
        }
    }

    private static bool TryNormalizeReaderDocumentContentType(string contentType, string fileName, out string normalized)
    {
        normalized = "";

        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "application/pdf";
            return true;
        }

        if (string.Equals(contentType, "application/epub+zip", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contentType, "application/x-epub+zip", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "application/epub+zip";
            return true;
        }

        if (string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "text/plain";
            return true;
        }

        return false;
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

    public override async Task<PutDocumentExtractResponse> PutDocumentExtract(PutDocumentExtractRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentId) || !Guid.TryParse(request.DocumentId, out var documentId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid document_id (UUID) is required"));
        }

        if (request.ExtractJson == null || request.ExtractJson.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "extract_json is required"));
        }

        if (request.ExtractJson.Length > MaxDocumentSizeBytes)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Extract payload must not exceed 50 MB"));
        }

        try
        {
            await using var stream = new MemoryStream(request.ExtractJson.ToByteArray());
            await _mediaStorage.PutDocumentExtractAsync(documentId, stream, context.CancellationToken).ConfigureAwait(false);
            return new PutDocumentExtractResponse();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error putting document extract {DocumentId}", documentId);
            throw new RpcException(new Status(StatusCode.Unavailable, $"Media storage error: {ex.Message}"));
        }
    }

    public override async Task<GetDocumentExtractResponse> GetDocumentExtract(GetDocumentExtractRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentId) || !Guid.TryParse(request.DocumentId, out var documentId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid document_id (UUID) is required"));
        }

        try
        {
            var bytes = await _mediaStorage.GetDocumentExtractAsync(documentId, context.CancellationToken).ConfigureAwait(false);
            if (bytes == null || bytes.Length == 0)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Document extract not found"));
            }

            return new GetDocumentExtractResponse
            {
                ExtractJson = Google.Protobuf.ByteString.CopyFrom(bytes)
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error getting document extract {DocumentId}", documentId);
            throw new RpcException(new Status(StatusCode.Unavailable, $"Media storage error: {ex.Message}"));
        }
    }

    public override async Task<DeleteDocumentExtractResponse> DeleteDocumentExtract(DeleteDocumentExtractRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentId) || !Guid.TryParse(request.DocumentId, out var documentId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid document_id (UUID) is required"));
        }

        try
        {
            await _mediaStorage.DeleteDocumentExtractAsync(documentId, context.CancellationToken).ConfigureAwait(false);
            return new DeleteDocumentExtractResponse();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error deleting document extract {DocumentId}", documentId);
            throw new RpcException(new Status(StatusCode.Unavailable, $"Media storage error: {ex.Message}"));
        }
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

    public override async Task<ListReaderCollectionsResponse> ListReaderCollections(ListReaderCollectionsRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var projectId = GetRequiredProjectId(request.ProjectId);
        var collections = await _mediaStorage.ListReaderCollectionsAsync(userId, projectId, context.CancellationToken).ConfigureAwait(false);
        var books = await _mediaStorage.ListReaderLibraryBooksAsync(userId, projectId, context.CancellationToken).ConfigureAwait(false);

        var response = new ListReaderCollectionsResponse();
        response.Collections.AddRange(await MapCollectionsAsync(collections, books, null, context.CancellationToken).ConfigureAwait(false));
        return response;
    }

    public override async Task<SaveReaderCollectionResponse> SaveReaderCollection(SaveReaderCollectionRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        if (request.Collection == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "collection is required"));
        }

        var collection = MapCollectionRecord(request.Collection, userId);
        var saved = await _mediaStorage.SaveReaderCollectionAsync(userId, collection, context.CancellationToken).ConfigureAwait(false);
        var books = await _mediaStorage.ListReaderLibraryBooksAsync(userId, saved.ProjectId, context.CancellationToken).ConfigureAwait(false);

        return new SaveReaderCollectionResponse
        {
            Collection = await MapCollectionAsync(saved, books, null, context.CancellationToken).ConfigureAwait(false)
        };
    }

    public override async Task<DeleteReaderCollectionResponse> DeleteReaderCollection(DeleteReaderCollectionRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var projectId = GetRequiredProjectId(request.ProjectId);
        if (string.IsNullOrWhiteSpace(request.CollectionId) || !Guid.TryParse(request.CollectionId, out var collectionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid collection_id (UUID) is required"));
        }

        await _mediaStorage.DeleteReaderCollectionAsync(userId, projectId, collectionId, context.CancellationToken).ConfigureAwait(false);
        return new DeleteReaderCollectionResponse();
    }

    public override async Task<ShareReaderCollectionResponse> ShareReaderCollection(ShareReaderCollectionRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var projectId = GetRequiredProjectId(request.ProjectId);
        if (string.IsNullOrWhiteSpace(request.CollectionId) || !Guid.TryParse(request.CollectionId, out var collectionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid collection_id (UUID) is required"));
        }

        if (request.Collaborator == null || string.IsNullOrWhiteSpace(request.Collaborator.UserId) || !Guid.TryParse(request.Collaborator.UserId, out var collaboratorUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid collaborator.user_id is required"));
        }

        var saved = await _mediaStorage.ShareReaderCollectionAsync(
            userId,
            projectId,
            collectionId,
            new ReaderCollectionCollaboratorRecord
            {
                UserId = collaboratorUserId,
                UserName = request.Collaborator.UserName ?? string.Empty,
                Email = request.Collaborator.Email ?? string.Empty,
                CanEdit = request.Collaborator.CanEdit,
                SharedAt = string.IsNullOrWhiteSpace(request.Collaborator.SharedAt) ? DateTimeOffset.UtcNow.ToString("O") : request.Collaborator.SharedAt
            },
            context.CancellationToken).ConfigureAwait(false);

        var books = await _mediaStorage.ListReaderLibraryBooksAsync(userId, projectId, context.CancellationToken).ConfigureAwait(false);
        return new ShareReaderCollectionResponse
        {
            Collection = await MapCollectionAsync(saved, books, null, context.CancellationToken).ConfigureAwait(false)
        };
    }

    public override async Task<UnshareReaderCollectionResponse> UnshareReaderCollection(UnshareReaderCollectionRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var projectId = GetRequiredProjectId(request.ProjectId);
        if (string.IsNullOrWhiteSpace(request.CollectionId) || !Guid.TryParse(request.CollectionId, out var collectionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid collection_id (UUID) is required"));
        }

        if (string.IsNullOrWhiteSpace(request.CollaboratorUserId) || !Guid.TryParse(request.CollaboratorUserId, out var collaboratorUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid collaborator_user_id is required"));
        }

        var saved = await _mediaStorage.UnshareReaderCollectionAsync(userId, projectId, collectionId, collaboratorUserId, context.CancellationToken).ConfigureAwait(false);
        var books = await _mediaStorage.ListReaderLibraryBooksAsync(userId, projectId, context.CancellationToken).ConfigureAwait(false);
        return new UnshareReaderCollectionResponse
        {
            Collection = await MapCollectionAsync(saved, books, null, context.CancellationToken).ConfigureAwait(false)
        };
    }

    public override async Task<ListSharedReaderCollectionsResponse> ListSharedReaderCollections(ListSharedReaderCollectionsRequest request, ServerCallContext context)
    {
        var userId = GetRequiredUserId(context);
        var sharedCollections = await _mediaStorage.ListSharedReaderCollectionsAsync(userId, context.CancellationToken).ConfigureAwait(false);

        var response = new ListSharedReaderCollectionsResponse();
        foreach (var item in sharedCollections)
        {
            response.Collections.Add(await MapCollectionAsync(item.Collection, item.Books, item.Access, context.CancellationToken).ConfigureAwait(false));
        }

        return response;
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

        var readingMode = string.IsNullOrWhiteSpace(book.ReadingMode)
            ? "pdf"
            : book.ReadingMode.Trim().ToLowerInvariant();
        if (readingMode is not ("pdf" or "extracted" or "text" or "epub"))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "book.reading_mode must be 'pdf', 'extracted', 'text', or 'epub'"));
        }

        return new ReaderLibraryBookRecord
        {
            Id = bookId,
            DocumentId = documentId,
            Title = book.Title.Trim(),
            FileName = book.FileName.Trim(),
            PageCount = book.PageCount > 0 ? book.PageCount : null,
            LastPageNumber = book.LastPageNumber > 0 ? book.LastPageNumber : null,
            UploadedAt = string.IsNullOrWhiteSpace(book.UploadedAt) ? DateTimeOffset.UtcNow.ToString("O") : book.UploadedAt,
            LastOpenedAt = string.IsNullOrWhiteSpace(book.LastOpenedAt) ? null : book.LastOpenedAt,
            CollectionId = string.IsNullOrWhiteSpace(book.CollectionId) ? null : book.CollectionId.Trim(),
            CollectionName = string.IsNullOrWhiteSpace(book.CollectionName) ? null : book.CollectionName.Trim(),
            OwnerUserId = Guid.TryParse(book.OwnerUserId, out var ownerUserId) ? ownerUserId : Guid.Empty,
            OwnerUserName = book.OwnerUserName ?? string.Empty,
            OwnerEmail = book.OwnerEmail ?? string.Empty,
            ReadingMode = readingMode,
            HasExtractedText = book.HasExtractedText,
            CoverImageUrl = book.CoverImageUrl ?? string.Empty,
            AudioUrl = book.AudioUrl ?? string.Empty,
            CefrLevel = book.CefrLevel ?? string.Empty,
            Summary = book.Summary ?? string.Empty
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
            LastPageNumber = book.LastPageNumber ?? 0,
            UploadedAt = book.UploadedAt,
            LastOpenedAt = book.LastOpenedAt ?? string.Empty,
            CollectionId = book.CollectionId ?? string.Empty,
            CollectionName = book.CollectionName ?? string.Empty,
            IsShared = book.IsShared,
            OwnerUserId = book.OwnerUserId == Guid.Empty ? string.Empty : book.OwnerUserId.ToString(),
            OwnerUserName = book.OwnerUserName,
            OwnerEmail = book.OwnerEmail,
            ReadingMode = string.IsNullOrWhiteSpace(book.ReadingMode) ? "pdf" : book.ReadingMode,
            HasExtractedText = book.HasExtractedText,
            CoverImageUrl = book.CoverImageUrl ?? string.Empty,
            AudioUrl = book.AudioUrl ?? string.Empty,
            CefrLevel = book.CefrLevel ?? string.Empty,
            Summary = book.Summary ?? string.Empty
        };
    }

    private static ReaderCollectionRecord MapCollectionRecord(ReaderCollection collection, Guid ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(collection.Id) || !Guid.TryParse(collection.Id, out var collectionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid collection.id (UUID) is required"));
        }

        if (string.IsNullOrWhiteSpace(collection.ProjectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "collection.project_id is required"));
        }

        if (string.IsNullOrWhiteSpace(collection.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "collection.name is required"));
        }

        return new ReaderCollectionRecord
        {
            Id = collectionId,
            ProjectId = collection.ProjectId.Trim(),
            Name = collection.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(collection.Description) ? null : collection.Description.Trim(),
            CreatedAt = string.IsNullOrWhiteSpace(collection.CreatedAt) ? DateTimeOffset.UtcNow.ToString("O") : collection.CreatedAt,
            UpdatedAt = string.IsNullOrWhiteSpace(collection.UpdatedAt) ? DateTimeOffset.UtcNow.ToString("O") : collection.UpdatedAt,
            OwnerUserId = ownerUserId,
            OwnerUserName = collection.OwnerUserName ?? string.Empty,
            OwnerEmail = collection.OwnerEmail ?? string.Empty,
            Collaborators = collection.Collaborators.Select(item => new ReaderCollectionCollaboratorRecord
            {
                UserId = Guid.TryParse(item.UserId, out var userId) ? userId : Guid.Empty,
                UserName = item.UserName ?? string.Empty,
                Email = item.Email ?? string.Empty,
                CanEdit = item.CanEdit,
                SharedAt = string.IsNullOrWhiteSpace(item.SharedAt) ? DateTimeOffset.UtcNow.ToString("O") : item.SharedAt
            }).Where(item => item.UserId != Guid.Empty).ToList()
        };
    }

    private async Task<IReadOnlyList<ReaderCollection>> MapCollectionsAsync(
        IEnumerable<ReaderCollectionRecord> collections,
        IReadOnlyList<ReaderLibraryBookRecord> books,
        ReaderCollectionCollaboratorRecord? access,
        CancellationToken cancellationToken)
    {
        var list = new List<ReaderCollection>();
        foreach (var collection in collections)
        {
            list.Add(await MapCollectionAsync(collection, books, access, cancellationToken).ConfigureAwait(false));
        }

        return list;
    }

    private async Task<ReaderCollection> MapCollectionAsync(
        ReaderCollectionRecord collection,
        IEnumerable<ReaderLibraryBookRecord> books,
        ReaderCollectionCollaboratorRecord? access,
        CancellationToken cancellationToken)
    {
        var collectionBooks = books
            .Where(book => string.Equals(book.CollectionId, collection.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        var mappedBooks = await MapBooksAsync(collectionBooks, cancellationToken).ConfigureAwait(false);
        var response = new ReaderCollection
        {
            Id = collection.Id.ToString(),
            ProjectId = collection.ProjectId,
            Name = collection.Name,
            Description = collection.Description ?? string.Empty,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            OwnerUserId = collection.OwnerUserId.ToString(),
            OwnerUserName = collection.OwnerUserName,
            OwnerEmail = collection.OwnerEmail,
            BookCount = mappedBooks.Count,
            IsSharedWithMe = access != null,
            CanEdit = access?.CanEdit ?? true
        };

        response.Collaborators.AddRange(collection.Collaborators.Select(item => new ReaderCollectionCollaborator
        {
            UserId = item.UserId.ToString(),
            UserName = item.UserName,
            Email = item.Email,
            CanEdit = item.CanEdit,
            SharedAt = item.SharedAt
        }));
        response.Books.AddRange(mappedBooks);
        return response;
    }
}
