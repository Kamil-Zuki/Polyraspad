namespace MediaService.Models;

public sealed class ReaderSharedCollectionRecord
{
    public ReaderCollectionRecord Collection { get; set; } = new();
    public IReadOnlyList<ReaderLibraryBookRecord> Books { get; set; } = [];
    public ReaderCollectionCollaboratorRecord? Access { get; set; }
}
