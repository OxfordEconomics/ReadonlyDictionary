﻿using MPHTest.MPH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadOnlyDictionary
{
    public class MphKeySource<TKey> : IKeySource
    {
        private readonly uint count;

        private IEnumerator<TKey> enumerator;

        public MphKeySource(IEnumerable<TKey> keys, uint count)
        {
            this.count = count;
            this.enumerator = keys.GetEnumerator();
        }

        public uint NbKeys
        {
            get { return this.count; }
        }

        public byte[] Read()
        {
            enumerator.MoveNext();

            return Encoding.UTF8.GetBytes(string.Format("KEY-{0}", enumerator.Current));
        }

        public void Rewind()
        {
            this.enumerator.Reset();
        }
    }
}
