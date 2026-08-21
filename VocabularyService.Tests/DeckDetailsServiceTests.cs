using FluentAssertions;
using Moq;
using VocabularyService.Dtos;
using VocabularyService.Tests.Skeleton;
using Xunit;

namespace VocabularyService.Tests;

/// <summary>
/// Unit-тесты для логики получения деталей колоды с подсчётом SRS-статистики (TDD).
/// </summary>
public class DeckDetailsServiceTests
{
    private readonly Mock<IDeckWithCardsProvider> _providerMock;
    private readonly DeckDetailsService _sut;

    public DeckDetailsServiceTests()
    {
        _providerMock = new Mock<IDeckWithCardsProvider>();
        _sut = new DeckDetailsService(_providerMock.Object);
    }

    [Fact]
    public async Task GetDeckDetails_ShouldCalculateStatsCorrectly()
    {
        // Arrange: колода с 5 карточками
        var deckId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var yesterday = now.AddDays(-1);
        var tomorrow = now.AddDays(1);
        var in5Minutes = now.AddMinutes(5);

        var deckWithCards = new DeckWithCards
        {
            Id = deckId,
            Title = "Test Deck",
            Description = "Description",
            ParentDeckId = null,
            Cards = new List<CardSrsState>
            {
                // Карточка A: New (Repetitions == 0)
                new()
                {
                    Id = Guid.NewGuid(),
                    DeckId = deckId,
                    Repetitions = 0,
                    Interval = 0,
                    NextReviewDate = tomorrow
                },
                // Карточка B: Learning (Repetitions > 0 и Interval < 1 день), но не Due скоро (> 20 мин)
                new()
                {
                    Id = Guid.NewGuid(),
                    DeckId = deckId,
                    Repetitions = 1,
                    Interval = 0.5,
                    NextReviewDate = tomorrow
                },
                // Карточка C: Due (NextReviewDate <= UtcNow)
                new()
                {
                    Id = Guid.NewGuid(),
                    DeckId = deckId,
                    Repetitions = 5,
                    Interval = 7,
                    NextReviewDate = yesterday
                },
                // Карточка D: ReviewFuture (NextReviewDate = завтра, не Due)
                new()
                {
                    Id = Guid.NewGuid(),
                    DeckId = deckId,
                    Repetitions = 10,
                    Interval = 14,
                    NextReviewDate = tomorrow
                },
                // Карточка E: Learning, Due скоро (через 5 минут)
                new()
                {
                    Id = Guid.NewGuid(),
                    DeckId = deckId,
                    Repetitions = 1,
                    Interval = 0.1,
                    NextReviewDate = in5Minutes
                }
            }
        };

        _providerMock
            .Setup(p => p.GetDeckWithCardsAsync(deckId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deckWithCards);

        // Act
        var result = await _sut.GetDeckDetailsAsync(deckId);

        // Assert: New=1 (A), Learning=1 (E, B — отфильтрована по времени), Due=1 (C), Total=5
        result.Should().NotBeNull();
        result!.Id.Should().Be(deckId);
        result.Stats.NewCardsCount.Should().Be(1, "карточка A: Repetitions == 0");
        result.Stats.LearningCardsCount.Should().Be(1, "карточка E: Repetitions > 0, Interval < 1 и Due в течение 20 мин (B не попадает)");
        result.Stats.DueCardsCount.Should().Be(1, "карточка C: NextReviewDate <= UtcNow");
        result.Stats.TotalCardsCount.Should().Be(5);
    }
}
