using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
namespace MerkonDB;

[MessagePackObject]
public class VectorDatabase
{
    [Key(0)]
    public Dictionary<string, List<DatabaseEntry>> VectorData { get; set; } = new();

    public void AddCollection(string Collection)
    {
        if (!this.VectorData.ContainsKey(Collection))
        {
            this.VectorData.Add(Collection, new());
        }
    }

    public DatabaseEntry? GetItem(string collection, string key)
    {
        this.AddCollection(collection);
        var seldata = this.VectorData[collection];
        var selitem = seldata.Where(x => x.Key == key).FirstOrDefault();
        return selitem;
    }
    public IEnumerable<string> GetCollections()
    {
        return this.VectorData.Keys;
    }

    public IEnumerable<DatabaseEntry> GetCollection(string collection)
    {
        this.AddCollection(collection);
        return this.VectorData[collection];
    }

    public bool IsCollectionExists(string Collection)
    {
        return this.VectorData.ContainsKey(Collection);
    }

    public bool RemoveCollection(string collection)
    {
        if (this.VectorData.ContainsKey(collection))
        {
            return this.VectorData.Remove(collection);
        }
        return false;
    }

    public bool RemoveItem(string collection, string key)
    {
        if (this.VectorData.ContainsKey(collection))
        {
            var data = this.VectorData[collection];
            var selitem = data.Where(x => x.Key == key).FirstOrDefault();
            if (selitem != null)
            {
                return data.Remove(selitem);
            }
        }
        return false;
    }

    public bool RemoveEmptyKeys(string collection)
    {
        if (this.VectorData.ContainsKey(collection))
        {
            var data = this.VectorData[collection];
            var selitems = data.Where(x => string.IsNullOrEmpty(x.Key)).ToList();
            if (selitems != null)
            {
                foreach (var item in selitems)
                {
                    data.Remove(item);
                }
                return true;
            }
        }
        return false;
    }
    public void InsertOrUpdate(string collection, string key, string metadata, string embedding, string timestamp)
    {
        this.AddCollection(collection);
        var db = VectorData[collection];
        var selItem = db.Where(x => x.Key == key).FirstOrDefault();
        if (selItem == null)
        {
            db.Add(new() { Key = key, EmbeddingString = embedding, MetadataString = metadata, Timestamp = timestamp });
        }
        else
        {
            selItem.MetadataString = metadata;
            selItem.Timestamp = timestamp;
            selItem.EmbeddingString = embedding;
        }
    }
}
public class MerkonDatabase
{
    readonly string DbFileName = "MerkonData.bin";
    public VectorDatabase VectorData { get; set; } = new();
    string GetDbPath(string DBName)
    {
        string fileName = Path.Combine(Environment.GetFolderPath(
    Environment.SpecialFolder.ApplicationData), DBName);
        return fileName;
    }

    public MerkonDatabase()
    {
        this.DbFileName = GetDbPath(this.DbFileName);
        Load();
    }
    public MerkonDatabase(string DatabaseName)
    {
        var formatted = DatabaseName.Replace(" ", "_") + ".bin";
        this.DbFileName = GetDbPath(formatted);
        Load();
    }
    public bool Load()
    {
        try
        {
            if (File.Exists(this.DbFileName))
            {
                byte[] bytes = File.ReadAllBytes(this.DbFileName);
                VectorData = MessagePackSerializer.Deserialize<VectorDatabase>(bytes);
                return true;
            }
            else
            {
                Save();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return false;
    }
    public bool Save()
    {
        try
        {
            byte[] bytes = MessagePackSerializer.Serialize(VectorData);
            File.WriteAllBytes(this.DbFileName, bytes);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return false;
    }

    public async Task CreateTableAsync(CancellationToken cancellationToken = default)
    {
        //do nothing
        this.Save();

    }

    public async Task CreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        this.VectorData.AddCollection(collectionName);
        this.Save();
    }

    public async Task UpdateAsync(string collection, string key, string? metadata, string? embedding, string? timestamp, CancellationToken cancellationToken = default)
    {
        this.VectorData.InsertOrUpdate(collection, key, metadata ?? string.Empty, embedding ?? string.Empty, timestamp ?? string.Empty);
        this.Save();
    }

    public async Task InsertOrIgnoreAsync(
        string collection, string key, string? metadata, string? embedding, string? timestamp, CancellationToken cancellationToken = default)
    {
        this.VectorData.InsertOrUpdate(collection, key, metadata ?? string.Empty, embedding ?? string.Empty, timestamp ?? string.Empty);
        this.Save();
    }

    public async Task<bool> DoesCollectionExistsAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        return this.VectorData.IsCollectionExists(collectionName);
    }

    public async IAsyncEnumerable<string> GetCollectionsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in this.VectorData.GetCollections())
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<DatabaseEntry> ReadAllAsync(
        string collectionName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        foreach (var item in this.VectorData.GetCollection(collectionName))
        {
            yield return item;
        }
    }

    public async Task<DatabaseEntry?> ReadAsync(
        string collectionName,
        string key,
        CancellationToken cancellationToken = default)
    {
        return this.VectorData.GetItem(collectionName, key);
    }

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        this.VectorData.RemoveCollection(collectionName);
        this.Save();
    }

    public async Task DeleteAsync(string collectionName, string key, CancellationToken cancellationToken = default)
    {
        this.VectorData.RemoveItem(collectionName, key);
        this.Save();
    }

    public async Task DeleteEmptyAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        this.VectorData.RemoveEmptyKeys(collectionName);
        this.Save();
    }
}
[MessagePackObject]
public class DatabaseEntry
{
    [Key(0)]
    public string Key { get; set; }
    [Key(1)]
    public string MetadataString { get; set; }
    [Key(2)]
    public string EmbeddingString { get; set; }
    [Key(3)]
    public string? Timestamp { get; set; }= DateTimeOffset.Now.ToString("u", CultureInfo.InvariantCulture);
}
