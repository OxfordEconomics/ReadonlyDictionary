﻿using Newtonsoft.Json;
using ReadOnlyDictionary.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadonlyDictionary.Storage.MemoryMappedFileIndex
{
    public class DictionaryMemoryMappedFileIndexFactory<T> : IMemoryMappedFileIndexFactory<T>
    {
        public IMemoryMappedFileIndex<T> Deserialize(byte[] bytes)
        {
            return new DictionaryMemoryMappedFileIndex<T>(bytes);
        }

        public byte[] Serialize(IEnumerable<KeyValuePair<T, long>> values)
        {
            return new DictionaryMemoryMappedFileIndex<T>(values).Serialize();
        }
    }

    public class DictionaryMemoryMappedFileIndex<T> : IMemoryMappedFileIndex<T>
    {
        private Dictionary<T, long> dictionary;

        public DictionaryMemoryMappedFileIndex(IEnumerable<KeyValuePair<T, long>> values)
        {
            this.dictionary = values.ToDictionary(v => v.Key, v => v.Value);
        }

        public DictionaryMemoryMappedFileIndex(byte[] bytes)
        {
            var indexJson = Encoding.UTF8.GetString(bytes);
            this.dictionary = JsonConvert.DeserializeObject<Dictionary<T, long>>(indexJson);
        }

        public byte[] Serialize()
        {
            var indexJson = JsonConvert.SerializeObject(this.dictionary); // todo: use passed in serializer (just for consistency)
            var indexBytes = Encoding.UTF8.GetBytes(indexJson);
            return indexBytes;
        }

        public void Add(T key, long index)
        {
            this.dictionary.Add(key, index);
        }

        public long Get(T key)
        {
            return this.dictionary[key];
        }


        public bool ContainsKey(T key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public bool TryGetValue(T key, out long index)
        {
            return this.dictionary.TryGetValue(key, out index);
        }

        public uint Count
        {
            get { return (uint)this.dictionary.LongCount(); }
        }

        public IEnumerable<T> Keys
        {
            get { return this.dictionary.Keys; }
        }
    }

}
