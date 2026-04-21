using MediaService.Models;

namespace MediaService.Services;

public interface IMediaStorageService
{
    Task<Guid> UploadImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<Guid> UploadAudioAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<Guid> UploadDocumentAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetMediaUrlAsync(Guid mediaId, string prefix, CancellationToken cancellationToken = default);
    Task<string> GetMediaUrlForServerFetchAsync(Guid mediaId, string prefix, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReaderLibraryBookRecord>> ListReaderLibraryBooksAsync(Guid userId, string projectId, CancellationToken cancellationToken = default);
    Task<ReaderLibraryBookRecord> SaveReaderLibraryBookAsync(Guid userId, string projectId, ReaderLibraryBookRecord book, CancellationToken cancellationToken = default);
    Task DeleteReaderLibraryBookAsync(Guid userId, string projectId, Guid bookId, CancellationToken cancellationToken = default);
}
