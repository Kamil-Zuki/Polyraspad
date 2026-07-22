using MediaService.Models;

namespace MediaService.Services;

public interface IMediaStorageService
{
    Task<Guid> UploadImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<Guid> UploadAudioAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<Guid> UploadDocumentAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task PutDocumentExtractAsync(Guid documentId, Stream data, CancellationToken cancellationToken = default);
    Task<byte[]?> GetDocumentExtractAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteDocumentExtractAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<string> GetMediaUrlAsync(Guid mediaId, string prefix, CancellationToken cancellationToken = default);
    Task<string> GetMediaUrlForServerFetchAsync(Guid mediaId, string prefix, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReaderLibraryBookRecord>> ListReaderLibraryBooksAsync(Guid userId, string projectId, CancellationToken cancellationToken = default);
    Task<ReaderLibraryBookRecord> SaveReaderLibraryBookAsync(Guid userId, string projectId, ReaderLibraryBookRecord book, CancellationToken cancellationToken = default);
    Task DeleteReaderLibraryBookAsync(Guid userId, string projectId, Guid bookId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReaderCollectionRecord>> ListReaderCollectionsAsync(Guid userId, string projectId, CancellationToken cancellationToken = default);
    Task<ReaderCollectionRecord> SaveReaderCollectionAsync(Guid userId, ReaderCollectionRecord collection, CancellationToken cancellationToken = default);
    Task DeleteReaderCollectionAsync(Guid userId, string projectId, Guid collectionId, CancellationToken cancellationToken = default);
    Task<ReaderCollectionRecord> ShareReaderCollectionAsync(Guid userId, string projectId, Guid collectionId, ReaderCollectionCollaboratorRecord collaborator, CancellationToken cancellationToken = default);
    Task<ReaderCollectionRecord> UnshareReaderCollectionAsync(Guid userId, string projectId, Guid collectionId, Guid collaboratorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReaderSharedCollectionRecord>> ListSharedReaderCollectionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
