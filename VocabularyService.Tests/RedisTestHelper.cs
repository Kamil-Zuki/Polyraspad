using Moq;
using StackExchange.Redis;

namespace VocabularyService.Tests;

internal static class RedisTestHelper
{
    public static IConnectionMultiplexer CreateConnectionMultiplexer()
    {
        var lists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var sets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);

        databaseMock
            .Setup(db => db.ListRightPushAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue[] values, CommandFlags _) =>
            {
                var list = GetOrCreateList(lists, key!);
                foreach (var value in values)
                {
                    list.Add(value.ToString());
                }

                return list.Count;
            });

        databaseMock
            .Setup(db => db.ListRightPushAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue[] values, When _, CommandFlags _) =>
            {
                var list = GetOrCreateList(lists, key!);
                foreach (var value in values)
                {
                    list.Add(value.ToString());
                }

                return list.Count;
            });

        databaseMock
            .Setup(db => db.ListRightPushAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue value, When _, CommandFlags _) =>
            {
                var list = GetOrCreateList(lists, key!);
                list.Add(value.ToString());
                return list.Count;
            });

        databaseMock
            .Setup(db => db.ListLeftPushAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue value, When _, CommandFlags _) =>
            {
                var list = GetOrCreateList(lists, key!);
                list.Insert(0, value.ToString());
                return list.Count;
            });

        databaseMock
            .Setup(db => db.ListLeftPopAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                var list = GetOrCreateList(lists, key!);
                if (list.Count == 0)
                {
                    return RedisValue.Null;
                }

                var value = list[0];
                list.RemoveAt(0);
                return (RedisValue)value;
            });

        databaseMock
            .Setup(db => db.SetContainsAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue value, CommandFlags _) =>
            {
                var set = GetOrCreateSet(sets, key!);
                return set.Contains(value.ToString());
            });

        databaseMock
            .Setup(db => db.SetAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue value, CommandFlags _) =>
            {
                var set = GetOrCreateSet(sets, key!);
                return set.Add(value.ToString());
            });

        databaseMock
            .Setup(db => db.KeyExpireAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<ExpireWhen>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var multiplexerMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseMock.Object);

        return multiplexerMock.Object;
    }

    private static List<string> GetOrCreateList(Dictionary<string, List<string>> storage, RedisKey key)
    {
        var normalizedKey = key.ToString();
        if (!storage.TryGetValue(normalizedKey, out var list))
        {
            list = [];
            storage[normalizedKey] = list;
        }

        return list;
    }

    private static HashSet<string> GetOrCreateSet(Dictionary<string, HashSet<string>> storage, RedisKey key)
    {
        var normalizedKey = key.ToString();
        if (!storage.TryGetValue(normalizedKey, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            storage[normalizedKey] = set;
        }

        return set;
    }
}
