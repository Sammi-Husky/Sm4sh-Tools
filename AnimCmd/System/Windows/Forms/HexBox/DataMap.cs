using System;
using System.Collections;

namespace Be.Windows.Forms {
    internal class DataMap : ICollection {
        internal int Version;

        public DataMap() {
            SyncRoot = new object();
        }

        public DataMap(IEnumerable collection) {
            SyncRoot = new object();
            if (collection == null)
                throw new ArgumentNullException("collection");
            foreach (DataBlock item in collection)
                AddLast(item);
        }

        public DataBlock FirstBlock { get; internal set; }

        public void AddAfter(DataBlock block, DataBlock newBlock) {
            AddAfterInternal(block, newBlock);
        }

        public void AddBefore(DataBlock block, DataBlock newBlock) {
            AddBeforeInternal(block, newBlock);
        }

        public void AddFirst(DataBlock block) {
            if (FirstBlock == null)
                AddBlockToEmptyMap(block);
            else
                AddBeforeInternal(FirstBlock, block);
        }

        public void AddLast(DataBlock block) {
            if (FirstBlock == null)
                AddBlockToEmptyMap(block);
            else
                AddAfterInternal(GetLastBlock(), block);
        }

        public void Remove(DataBlock block) {
            RemoveInternal(block);
        }

        public void RemoveFirst() {
            if (FirstBlock == null)
                throw new InvalidOperationException("The collection is empty.");
            RemoveInternal(FirstBlock);
        }

        public void RemoveLast() {
            if (FirstBlock == null)
                throw new InvalidOperationException("The collection is empty.");
            RemoveInternal(GetLastBlock());
        }

        public DataBlock Replace(DataBlock block, DataBlock newBlock) {
            AddAfterInternal(block, newBlock);
            RemoveInternal(block);
            return newBlock;
        }

        public void Clear() {
            var block = FirstBlock;
            while (block != null) {
                var nextBlock = block.NextBlock;
                InvalidateBlock(block);
                block = nextBlock;
            }
            FirstBlock = null;
            Count = 0;
            Version++;
        }

        private void AddAfterInternal(DataBlock block, DataBlock newBlock) {
            newBlock.PreviousBlock = block;
            newBlock.NextBlock = block.NextBlock;
            newBlock.Map = this;
            if (block.NextBlock != null)
                block.NextBlock.PreviousBlock = newBlock;
            block.NextBlock = newBlock;
            Version++;
            Count++;
        }

        private void AddBeforeInternal(DataBlock block, DataBlock newBlock) {
            newBlock.NextBlock = block;
            newBlock.PreviousBlock = block.PreviousBlock;
            newBlock.Map = this;
            if (block.PreviousBlock != null)
                block.PreviousBlock.NextBlock = newBlock;
            block.PreviousBlock = newBlock;
            if (FirstBlock == block)
                FirstBlock = newBlock;
            Version++;
            Count++;
        }

        private void RemoveInternal(DataBlock block) {
            var previousBlock = block.PreviousBlock;
            var nextBlock = block.NextBlock;
            if (previousBlock != null)
                previousBlock.NextBlock = nextBlock;
            if (nextBlock != null)
                nextBlock.PreviousBlock = previousBlock;
            if (FirstBlock == block)
                FirstBlock = nextBlock;
            InvalidateBlock(block);
            Count--;
            Version++;
        }

        private DataBlock GetLastBlock() {
            DataBlock lastBlock = null;
            for (var block = FirstBlock; block != null; block = block.NextBlock)
                lastBlock = block;
            return lastBlock;
        }

        private static void InvalidateBlock(DataBlock block) {
            block.Map = null;
            block.NextBlock = null;
            block.PreviousBlock = null;
        }

        private void AddBlockToEmptyMap(DataBlock block) {
            block.Map = this;
            block.NextBlock = null;
            block.PreviousBlock = null;
            FirstBlock = block;
            Version++;
            Count++;
        }

        #region ICollection Members
        public void CopyTo(Array array, int index) {
            var blockArray = (DataBlock[]) array;
            for (var block = FirstBlock; block != null; block = block.NextBlock)
                blockArray[index++] = block;
        }

        public int Count { get; internal set; }

        public bool IsSynchronized {
            get { return false; }
        }

        public object SyncRoot { get; private set; }
        #endregion

        #region IEnumerable Members
        public IEnumerator GetEnumerator() {
            return new Enumerator(this);
        }
        #endregion

        #region Enumerator Nested Type
        internal class Enumerator : IEnumerator, IDisposable {
            private readonly DataMap _map;
            private DataBlock _current;
            private int _index;
            private readonly int _version;

            internal Enumerator(DataMap map) {
                _map = map;
                _version = map.Version;
                _current = null;
                _index = -1;
            }

            object IEnumerator.Current {
                get {
                    if (_index < 0 || _index > _map.Count)
                        throw new InvalidOperationException(
                            "Enumerator is positioned before the first element or after the last element of the collection.");
                    return _current;
                }
            }

            public bool MoveNext() {
                if (_version != _map.Version)
                    throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
                if (_index >= _map.Count)
                    return false;
                _current = ++_index == 0 ? _map.FirstBlock : _current.NextBlock;
                return (_index < _map.Count);
            }

            void IEnumerator.Reset() {
                if (_version != _map.Version)
                    throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
                _index = -1;
                _current = null;
            }

            public void Dispose() {}
        }
        #endregion
    }
}