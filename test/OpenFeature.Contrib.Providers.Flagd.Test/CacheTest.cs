using System.Threading.Tasks;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;


public class UnitTestLRUCache
{
    [Fact]
    public void TestCacheSetGet()
    {
        int capacity = 5;
        var cache = new LRUCache<string, string>(capacity);

        cache.Add("my-key", "my-value");

        var value = cache.TryGet("my-key");
        Assert.Equal("my-value", value);
    }
    [Fact]
    public async Task TestCacheCapacity()
    {
        int capacity = 5;
        var cache = new LRUCache<string, string>(capacity);

        var tasks = new System.Collections.Generic.List<Task>(capacity);

        for (int i = 0; i < capacity; i++)
        {
            cache.Add($"key-{i}", $"value-{i}");
        }

        await Task.WhenAll(tasks);

        string value;
        // verify that we can retrieve all items
        for (int i = 0; i < capacity; i++)
        {
            value = cache.TryGet($"key-{i}");

            Assert.Equal($"value-{i}", value);
        }

        // add another item - now the least recently used item ("key-0") should be replaced
        cache.Add("new-item", "new-value");

        value = cache.TryGet("key-0");
        Assert.Null(value);

        value = cache.TryGet("new-item");
        Assert.Equal("new-value", value);
    }

    [Fact]
    public async Task TestCacheCapacityMultiThreaded()
    {
        int capacity = 5;
        var cache = new LRUCache<string, string>(capacity);

        var tasks = new System.Collections.Generic.List<Task>(capacity);

        var counter = 0;
        for (int i = 0; i < capacity; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var id = System.Threading.Interlocked.Increment(ref counter);
                cache.Add($"key-{id}", $"value-{id}");
            }));
            //cache.Add($"key-{i}", $"value-{i}");
        }

        await Task.WhenAll(tasks);

        string value;
        // verify that we can retrieve all items
        for (int i = 1; i <= capacity; i++)
        {
            value = cache.TryGet($"key-{i}");

            Assert.Equal($"value-{i}", value);
        }

        // add another item - now the least recently used item ("key-0") should be replaced
        cache.Add("new-item", "new-value");

        value = cache.TryGet("key-0");
        Assert.Null(value);

        value = cache.TryGet("new-item");
        Assert.Equal("new-value", value);
    }

    [Fact]
    public void TestCacheDeleteOnlyItem()
    {
        int capacity = 5;
        var cache = new LRUCache<string, string>(capacity);

        var key = "my-key";
        var expectedValue = "my-value";

        cache.Add(key, expectedValue);

        string value;

        value = cache.TryGet(key);
        Assert.Equal(expectedValue, value);

        cache.Delete(key);

        value = cache.TryGet(key);
        Assert.Null(value);

        // check that we can add something again after deleting the last item
        cache.Add(key, expectedValue);
        Assert.Equal(expectedValue, cache.TryGet(key));
    }

    [Fact]
    public void TestCacheDeleteHead()
    {
        int capacity = 5;
        var cache = new LRUCache<string, string>(capacity);


        cache.Add("tail", "tail");
        cache.Add("middle", "middle");
        cache.Add("head", "head");

        cache.Delete("head");

        Assert.Null(cache.TryGet("head"));
        Assert.Equal("middle", cache.TryGet("middle"));
        Assert.Equal("tail", cache.TryGet("tail"));
    }

    [Fact]
    public void TestCacheDeleteMiddle()
    {
        int capacity = 5;
        var cache = new LRUCache<string, string>(capacity);


        cache.Add("tail", "tail");
        cache.Add("middle", "middle");
        cache.Add("head", "head");

        cache.Delete("middle");

        Assert.Null(cache.TryGet("middle"));
        Assert.Equal("head", cache.TryGet("head"));
        Assert.Equal("tail", cache.TryGet("tail"));
    }

    [Fact]
    public void TestPurge()
    {
        int capacity = 5;
        var cache = new LRUCache<string, string>(capacity);


        cache.Add("tail", "tail");
        cache.Add("middle", "middle");
        cache.Add("head", "head");

        cache.Purge();

        Assert.Null(cache.TryGet("head"));
        Assert.Null(cache.TryGet("middle"));
        Assert.Null(cache.TryGet("tail"));
    }
}
