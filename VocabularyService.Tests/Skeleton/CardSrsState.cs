namespace VocabularyService.Tests.Skeleton;

/// <summary>
/// Модель карточки для подсчёта SRS-статистики (TDD skeleton).
/// Repetitions, Interval, NextReviewDate — для расчёта New / Learning / Due.
/// </summary>
public class CardSrsState
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public int Repetitions { get; set; }
    public double Interval { get; set; }
    public DateTime NextReviewDate { get; set; }
}
