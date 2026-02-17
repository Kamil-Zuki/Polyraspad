namespace VocabularyService.Tests.Skeleton;

/// <summary>
/// Колода с карточками для TDD-скелета (результат провайдера данных).
/// </summary>
public class DeckWithCards
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDeckId { get; set; }
    public IReadOnlyList<CardSrsState> Cards { get; set; } = Array.Empty<CardSrsState>();
}
