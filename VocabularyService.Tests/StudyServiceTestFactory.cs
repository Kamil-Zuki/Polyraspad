#nullable enable
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Services;
using VocabularyService.Services.Study;

namespace VocabularyService.Tests;

internal static class StudyServiceTestFactory
{
    public static StudyService Create(
        VocabularyServiceContext context,
        ICardService cardService,
        IFsrsScheduler fsrsScheduler,
        IUserSettingsService userSettingsService,
        IMediaService mediaService,
        IConnectionMultiplexer? redis = null)
    {
        var mux = redis ?? RedisTestHelper.CreateConnectionMultiplexer();
        var preview = new FsrsPreviewService(fsrsScheduler);
        var queue = new AnkiStudyQueueService(context, mux);
        return new StudyService(
            context,
            Mock.Of<ILogger<StudyService>>(),
            cardService,
            Mock.Of<IDeckService>(),
            userSettingsService,
            fsrsScheduler,
            preview,
            queue,
            mediaService,
            mux);
    }
}
