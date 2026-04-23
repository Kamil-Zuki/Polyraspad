namespace MediaService.Models;

public sealed class ReaderCollectionRecord
{
    public Guid Id { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public string OwnerUserName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public List<ReaderCollectionCollaboratorRecord> Collaborators { get; set; } = [];
}
