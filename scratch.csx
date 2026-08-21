using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VocabularyService.Data;

var dbContextOptions = new DbContextOptionsBuilder<VocabularyServiceContext>()
    .UseNpgsql("Host=localhost;Port=5434;Database=vocabulary_service;Username=postgres;Password=postgres")
    .Options;

using var db = new VocabularyServiceContext(dbContextOptions);
var deck = db.Decks.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
if (deck != null) {
    Console.WriteLine($"Deck: {deck.Id}");
    var userId = deck.OwnerId;
    var projectId = deck.ProjectId;
    var deckId = deck.Id;
    var now = DateTime.UtcNow;
    var cutoff = now.AddMinutes(20);
    
    var totalCards = db.Cards.Count(c => c.DeckId == deckId);
    var progressList = db.UserCardProgresses.Where(p => p.UserId == userId && p.ProjectId == projectId && p.Card.DeckId == deckId).ToList();
    var activeProgress = progressList.Where(p => !p.IsSuspended).ToList();
    
    var newCount = (totalCards - progressList.Count) + activeProgress.Count(p => p.Reps == 0);
    var learningCount = activeProgress.Count(p => (p.State == 1 || p.State == 3 || p.ScheduledDays < 1) && p.Due <= cutoff);
    var dueCount = activeProgress.Count(p => p.State == 2 && p.Due <= now);
    
    Console.WriteLine($"Stats -> New: {newCount}, Learning: {learningCount}, Due: {dueCount}");
    
    var reviews = db.UserCardProgresses.Where(p => p.UserId == userId && p.ProjectId == projectId && p.Card.DeckId == deckId && p.State == 2 && p.Due <= now && !p.IsSuspended).ToList();
    Console.WriteLine($"Query Reviews -> {reviews.Count}");
    
    var unreviewedWithProgress = db.UserCardProgresses.Where(p => p.UserId == userId && p.ProjectId == projectId && p.Card.DeckId == deckId && p.State == 0 && p.Reps == 0 && p.Lapses == 0 && !p.IsSuspended).ToList();
    Console.WriteLine($"Query unreviewedWithProgress -> {unreviewedWithProgress.Count}");
    
    var existingProgressSet = db.UserCardProgresses.Where(p => p.UserId == userId && p.Card.DeckId == deckId).Select(p => p.CardId).ToHashSet();
    var newCardsWithoutProgress = db.Cards.Where(c => c.DeckId == deckId && !existingProgressSet.Contains(c.Id) && (c.CreatorId == userId || c.Deck.OwnerId == userId || c.Deck.IsPublic)).ToList();
    Console.WriteLine($"Query newCardsWithoutProgress -> {newCardsWithoutProgress.Count}");
}
