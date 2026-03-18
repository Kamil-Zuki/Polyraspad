using VocabularyService.Dtos;

namespace VocabularyService.Tests.Skeleton;

/// <summary>
/// Реализация подсчёта SRS-статистики для деталей колоды (TDD Green).
/// </summary>
public class DeckDetailsService : IDeckDetailsService
{
    private readonly IDeckWithCardsProvider _provider;

    public DeckDetailsService(IDeckWithCardsProvider provider)
    {
        _provider = provider;
    }

    public async Task<DeckDetailDto?> GetDeckDetailsAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        var deckWithCards = await _provider.GetDeckWithCardsAsync(deckId, cancellationToken);
        if (deckWithCards == null)
        {
            return null;
        }

        var cards = deckWithCards.Cards;
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(20);

        var newCount = cards.Count(c => c.Repetitions == 0);
        // Learning: карточки в процессе изучения, доступные в течение 20 минут
        var learningCount = cards.Count(c => c.Repetitions > 0 && c.Interval < 1 && c.NextReviewDate <= cutoff);
        var dueCount = cards.Count(c => c.NextReviewDate <= now);
        var totalCount = cards.Count;

        return new DeckDetailDto
        {
            Id = deckWithCards.Id,
            Title = deckWithCards.Title,
            Description = deckWithCards.Description,
            ParentDeckId = deckWithCards.ParentDeckId,
            Stats = new DeckDetailStatsDto
            {
                NewCardsCount = newCount,
                LearningCardsCount = learningCount,
                DueCardsCount = dueCount,
                TotalCardsCount = totalCount
            }
        };
    }
}
