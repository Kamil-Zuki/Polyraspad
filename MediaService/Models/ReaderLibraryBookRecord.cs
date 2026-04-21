namespace MediaService.Models;

public sealed class ReaderLibraryBookRecord
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int? PageCount { get; set; }
    public string UploadedAt { get; set; } = string.Empty;
    public string? LastOpenedAt { get; set; }
    public string? CollectionId { get; set; }
    public string? CollectionName { get; set; }
}
