using Moq;
using StackExchange.Redis;

namespace VocabularyService.Tests;

internal static class RedisTestHelper
{
    public static IConnectionMultiplexer CreateConnectionMultiplexer()
    {
        var lists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var sets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var sortedSets = new Dictionary<string, SortedDictionary<double, HashSet<string>>>(StringComparer.Ordinal);
        var hashes = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

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
                if (!lists.TryGetValue(key.ToString(), out var list) || list.Count == 0)
                {
                    return RedisValue.Null;
                }

                var value = list[0];
                list.RemoveAt(0);
                if (list.Count == 0)
                {
                    lists.Remove(key.ToString());
                }

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
            .Setup(db => db.HashGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue field, CommandFlags _) =>
            {
                var hash = GetOrCreateHash(hashes, key!);
                return hash.TryGetValue(field.ToString(), out var value)
                    ? (RedisValue)value
                    : RedisValue.Null;
            });

        databaseMock
            .Setup(db => db.HashSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<RedisValue>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue field, RedisValue value, When _, CommandFlags _) =>
            {
                var hash = GetOrCreateHash(hashes, key!);
                var isNew = !hash.ContainsKey(field.ToString());
                hash[field.ToString()] = value.ToString();
                return isNew;
            });

        databaseMock
            .Setup(db => db.KeyExpireAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<ExpireWhen>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        databaseMock
            .Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                var k = key.ToString();
                lists.Remove(k);
                sets.Remove(k);
                sortedSets.Remove(k);
                hashes.Remove(k);
                return true;
            });

        databaseMock
            .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                var k = key.ToString();
                return lists.ContainsKey(k)
                    || sets.ContainsKey(k)
                    || sortedSets.ContainsKey(k)
                    || hashes.ContainsKey(k);
            });

        databaseMock
            .Setup(db => db.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
                lists.TryGetValue(key.ToString(), out var list) ? list.Count : 0);

        databaseMock
            .Setup(db => db.ListRangeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, long _, long _, CommandFlags _) =>
            {
                if (!lists.TryGetValue(key.ToString(), out var list))
                {
                    return [];
                }

                return list.Select(v => (RedisValue)v).ToArray();
            });

        databaseMock
            .Setup(db => db.SortedSetAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<double>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue member, double score, When _, CommandFlags _) =>
                SortedSetAdd(sortedSets, key!, member, score));

        databaseMock
            .Setup(db => db.SortedSetAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<double>(),
                It.IsAny<SortedSetWhen>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue member, double score, SortedSetWhen _, CommandFlags _) =>
                SortedSetAdd(sortedSets, key!, member, score));

        databaseMock
            .Setup(db => db.SortedSetRemoveAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue member, CommandFlags _) =>
            {
                var zset = GetOrCreateSortedSet(sortedSets, key!);
                var id = member.ToString();
                foreach (var bucket in zset.Values)
                {
                    if (bucket.Remove(id))
                        return true;
                }

                return false;
            });

        databaseMock
            .Setup(db => db.SortedSetScoreAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue member, CommandFlags _) =>
            {
                var zset = GetOrCreateSortedSet(sortedSets, key!);
                var id = member.ToString();
                foreach (var (score, bucket) in zset)
                {
                    if (bucket.Contains(id))
                        return score;
                }

                return (double?)null;
            });

        databaseMock
            .Setup(db => db.SortedSetRangeByScoreAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<Exclude>(),
                It.IsAny<Order>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((
                RedisKey key,
                double start,
                double stop,
                Exclude _,
                Order order,
                long skip,
                long take,
                CommandFlags _) =>
            {
                var zset = GetOrCreateSortedSet(sortedSets, key!);
                var entries = zset
                    .Where(kv => kv.Key >= start && kv.Key <= stop)
                    .SelectMany(kv => kv.Value.Select(v => (Score: kv.Key, Member: v)))
                    .OrderBy(e => e.Score)
                    .ThenBy(e => e.Member)
                    .Skip((int)skip)
                    .Take(take > 0 ? (int)take : int.MaxValue)
                    .Select(e => (RedisValue)e.Member)
                    .ToArray();
                return entries;
            });

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

    private static bool SortedSetAdd(
        Dictionary<string, SortedDictionary<double, HashSet<string>>> storage,
        RedisKey key,
        RedisValue member,
        double score)
    {
        var zset = GetOrCreateSortedSet(storage, key);
        if (!zset.TryGetValue(score, out var bucket))
        {
            bucket = new HashSet<string>(StringComparer.Ordinal);
            zset[score] = bucket;
        }

        return bucket.Add(member.ToString());
    }

    private static SortedDictionary<double, HashSet<string>> GetOrCreateSortedSet(
        Dictionary<string, SortedDictionary<double, HashSet<string>>> storage,
        RedisKey key)
    {
        var normalizedKey = key.ToString();
        if (!storage.TryGetValue(normalizedKey, out var zset))
        {
            zset = new SortedDictionary<double, HashSet<string>>();
            storage[normalizedKey] = zset;
        }

        return zset;
    }

    private static Dictionary<string, string> GetOrCreateHash(
        Dictionary<string, Dictionary<string, string>> storage,
        RedisKey key)
    {
        var normalizedKey = key.ToString();
        if (!storage.TryGetValue(normalizedKey, out var hash))
        {
            hash = new Dictionary<string, string>(StringComparer.Ordinal);
            storage[normalizedKey] = hash;
        }

        return hash;
    }
}
