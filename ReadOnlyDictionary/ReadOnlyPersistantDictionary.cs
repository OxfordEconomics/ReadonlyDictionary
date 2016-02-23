﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPHTest.MPH;
using System.IO.MemoryMappedFiles;
using System.IO;

namespace ReadOnlyDictionary
{
    public interface IKeyValueStore<TValue> : IDisposable
    {
        bool ContainsKey(Guid key);
        bool TryGetValue(Guid key, out TValue value);
        uint Count { get; }
    }

    public static class IKeyValueStoreExtensions
    {
        public static TValue Get<TValue>(this IKeyValueStore<TValue> store, Guid key) where TValue: class
        {
            TValue temp;
            if (store.TryGetValue(key, out temp))
            {
                return temp;
            }
            else
            {
                return null;
            }
        }
    }

    public class InMemoryKeyValueStorage<TValue> : IKeyValueStore<TValue>
    {
        private readonly Dictionary<Guid, TValue> index;

        public InMemoryKeyValueStorage(IEnumerable<KeyValuePair<Guid, TValue>> values)
        {
            this.index = values.ToDictionary(v => v.Key, v => v.Value);
        }

        public bool ContainsKey(Guid key)
        {
            return this.index.ContainsKey(key);
        }

        public bool TryGetValue(Guid key, out TValue value)
        {
            return this.index.TryGetValue(key, out value);
        }

        public uint Count
        {
            get { return (uint)this.index.LongCount(); }
        }

        public void Dispose()
        {
        }
    }

    public class FileIndexKeyValueStorage<TValue> : IKeyValueStore<TValue>, IDisposable
    {
        private readonly Dictionary<Guid, long> index;
        private readonly MemoryMappedFile mmf;
        private readonly MemoryMappedViewAccessor accessor;
        private readonly Func<byte[], TValue> deserializer;

        public FileIndexKeyValueStorage(
            IEnumerable<KeyValuePair<Guid, TValue>> values, 
            string filename, 
            long initialSize, 
            Func<TValue, byte[]> serializer, 
            Func<byte[], TValue> deserializer,
            long count)
        {
            this.deserializer = deserializer;

            this.index = new Dictionary<Guid, long>();

            var fi = new FileInfo(filename);
            if (fi.Exists)
            {
                fi.Delete();
            }

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.CreateNew, filename, initialSize);

            this.accessor = mmf.CreateViewAccessor();

            var guidBytes = Guid.NewGuid().ToByteArray().Length;
            // allocate space for index
            long indexSize = count * (guidBytes + sizeof(long));
            long position = indexSize + sizeof(long);

            foreach(var item in values)
            {
                var value = item.Value;
                var serialized = serializer(value);
                accessor.Write(position, serialized.Length);
                accessor.WriteArray(position + sizeof(Int32), serialized, 0, serialized.Length);

                index.Add(item.Key, position);
                position += serialized.Length + sizeof(Int32);
            }

            // store count in file
            accessor.Write(0, count);

            // write index after count
            long indexPosition = sizeof(long);
            foreach (var item in index)
            {
                var keyBytes = item.Key.ToByteArray();
                accessor.Write(indexPosition, item.Value);
                indexPosition += sizeof(long);
                accessor.WriteArray(indexPosition, keyBytes, 0, keyBytes.Length);
                indexPosition += keyBytes.Length;
            }
           
            this.accessor.Flush();

        }

        public FileIndexKeyValueStorage(string filename, Func<byte[], TValue> deserializer)
        {
            this.deserializer = deserializer;
            var fi = new FileInfo(filename);
            this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.Open);
            this.accessor = mmf.CreateViewAccessor();
            this.index = new Dictionary<Guid, long>();

            // file begins with count
            var count = accessor.ReadInt64(0);

            // read index after count
            long indexPosition = sizeof(long);
            var guidBytes = Guid.NewGuid().ToByteArray().Length;

            for(int i = 0; i < count; i++)
            {
                var offset = accessor.ReadInt64(indexPosition);
                indexPosition += sizeof(long);
                byte[] keyBytes = new byte[guidBytes];
                accessor.ReadArray(indexPosition, keyBytes, 0, guidBytes);
                indexPosition += keyBytes.Length;

                index.Add(new Guid(keyBytes), offset);
            }
        }

        public bool ContainsKey(Guid key)
        {
            return this.index.ContainsKey(key);
        }

        public bool TryGetValue(Guid key, out TValue value)
        {
            long index;
            if (this.index.TryGetValue(key, out index))
            {
                var serializedSize = this.accessor.ReadInt32(index);
                byte[] serialized = new byte[serializedSize];
                this.accessor.ReadArray(index + sizeof(Int32), serialized, 0, serializedSize);
                value = deserializer(serialized);
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public uint Count
        {
            get { return (uint)this.index.LongCount(); }
        }

        public void Dispose()
        {
            this.accessor.Flush();
            this.accessor.Dispose();            
            this.mmf.Dispose();
        }
    }

    public class MinPerfectHashKeyValueStorage<TValue> : IKeyValueStore<TValue>
    {
        private readonly MinPerfectHash hashFunction;
        private readonly uint count;

        public MinPerfectHashKeyValueStorage(IEnumerable<KeyValuePair<Guid, TValue>> values, uint count)
        {
            var keyGenerator = new MphKeySource<Guid>(values.Select(v => v.Key), count);

            this.hashFunction = MinPerfectHash.Create(keyGenerator, 1);
            this.count = count;
        }

        public bool ContainsKey(Guid key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Guid key, out TValue value)
        {
            throw new NotImplementedException();
        }

        public uint Count { get { return count; } }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
