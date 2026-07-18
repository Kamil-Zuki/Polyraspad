using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Services;
using VocabularyService.Services.Study;
using Moq;
using Microsoft.Extensions.Logging;

namespace VocabularyService.Tests;

public class DiagnosticsTest
{
    private readonly ITestOutputHelper _output;

    public DiagnosticsTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SimulateDeckStatsAndStudyQueue()
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new VocabularyServiceContext(options);
        
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        var deck = new Deck { Id = deckId, ProjectId = projectId, OwnerId = userId };
        context.Decks.Add(deck);

        var card1 = new Card { Id = Guid.NewGuid(), DeckId = deckId, CreatorId = userId, CreatedAt = DateTime.UtcNow };
        context.Cards.Add(card1);

        // A card with due date in 5 minutes
        var card2 = new Card { Id = Guid.NewGuid(), DeckId = deckId, CreatorId = userId, CreatedAt = DateTime.UtcNow };
        context.Cards.Add(card2);
        
        var progress2 = new UserCardProgress 
        { 
            Id = Guid.NewGuid(), UserId = userId, ProjectId = projectId, CardId = card2.Id, 
            State = 1, // Learning
            Due = DateTime.UtcNow.AddMinutes(5),
            IsSuspended = false
        };
        context.UserCardProgresses.Add(progress2);

        await context.SaveChangesAsync();

        // 1. Calculate Deck Stats
        var totalCards = await context.Cards.CountAsync(c => c.DeckId == deckId);
        var progressList = await context.UserCardProgresses
            .Where(p => p.UserId == userId && p.ProjectId == projectId && p.Card.DeckId == deckId)
            .ToListAsync();
            
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(20);
        var activeProgress = progressList.Where(p => !p.IsSuspended).ToList();

        var studyableNewWithoutProgress = totalCards - progressList.Count;
        var studyableUnreviewedWithProgress = activeProgress.Count(p => p.State == 0 && p.Reps == 0 && p.Lapses == 0);
        var studyableDueLearningReview = activeProgress.Count(p =>
            (p.State == 2 && p.Due <= now)
            || ((p.State == 1 || p.State == 3) && p.Due <= cutoff)
            || (p.State == 0 && p.Lapses > 0 && p.Due <= now));

        var studyableNowCount = studyableNewWithoutProgress + studyableUnreviewedWithProgress + studyableDueLearningReview;
        
        _output.WriteLine($"studyableNewWithoutProgress: {studyableNewWithoutProgress}");
        _output.WriteLine($"studyableUnreviewedWithProgress: {studyableUnreviewedWithProgress}");
        _output.WriteLine($"studyableDueLearningReview: {studyableDueLearningReview}");
        _output.WriteLine($"StudyableNowCount: {studyableNowCount}");

        // 2. Generate Queue
        var existingProgressSet = progressList.Select(p => p.CardId).ToHashSet();
        
        var newCardsWithoutProgress = await context.Cards
            .Where(c => c.DeckId == deckId
                && !existingProgressSet.Contains(c.Id)
                && (c.CreatorId == userId || c.Deck.OwnerId == userId || c.Deck.IsPublic))
            .Select(c => c.Id)
            .ToListAsync();

        var learning = await context.UserCardProgresses
            .Where(p => p.UserId == userId
                && p.ProjectId == projectId
                && p.Card.DeckId == deckId
                && (p.State == 1 || p.State == 3)
                && p.Due <= now
                && !p.IsSuspended)
            .Select(p => p.CardId)
            .ToListAsync();

        _output.WriteLine($"newCardsWithoutProgress: {newCardsWithoutProgress.Count}");
        _output.WriteLine($"learning (Due <= now): {learning.Count}");
        
        var learnAhead = await context.UserCardProgresses
            .Where(p => p.UserId == userId
                && p.ProjectId == projectId
                && p.Card.DeckId == deckId
                && (p.State == 1 || p.State == 3)
                && p.Due > now
                && p.Due <= cutoff
                && !p.IsSuspended)
            .Select(p => p.CardId)
            .ToListAsync();
            
        _output.WriteLine($"learnAhead (now < Due <= cutoff): {learnAhead.Count}");
        
        var queue = newCardsWithoutProgress.Concat(learning).Concat(learnAhead).ToList();
        _output.WriteLine($"Total Queue: {queue.Count}");

        Assert.Equal(studyableNowCount, queue.Count);
    }
}
