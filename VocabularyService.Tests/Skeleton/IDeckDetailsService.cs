using VocabularyService.Dtos;

namespace VocabularyService.Tests.Skeleton;

/// <summary>
/// Сервис деталей колоды со статистикой SRS (TDD skeleton).
/// Сигнатура: GetDeckDetailsAsync(Guid deckId) — без userId для упрощения сценария.
/// </summary>
public interface IDeckDetailsService
{
    Task<DeckDetailDto?> GetDeckDetailsAsync(Guid deckId, CancellationToken cancellationToken = default);
}
