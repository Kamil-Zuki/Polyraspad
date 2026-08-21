namespace VocabularyService.Tests.Skeleton;

/// <summary>
/// Провайдер данных для получения колоды с карточками (для мока в тестах).
/// </summary>
public interface IDeckWithCardsProvider
{
    Task<DeckWithCards?> GetDeckWithCardsAsync(Guid deckId, CancellationToken cancellationToken = default);
}
