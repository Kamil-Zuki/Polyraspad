namespace MediaService.Models;

public sealed class ReaderCollectionCollaboratorRecord
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public string SharedAt { get; set; } = string.Empty;
}
