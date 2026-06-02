using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MongoObject.Core.Collections
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
        where TKey : notnull, IEquatable<TKey>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new();

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        // Helper methods to raise events
        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item));
            OnPropertyChanged(nameof(Count));
        }

        private void OnCollectionReset()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(nameof(Count));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // IDictionary Implementation
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                bool isUpdate = _dictionary.TryGetValue(key, out var oldValue);
                _dictionary[key] = value;

                if (isUpdate)
                {
                    var oldItem = new KeyValuePair<TKey, TValue>(key, oldValue!);
                    var newItem = new KeyValuePair<TKey, TValue>(key, value);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem));
                }
                else
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.TryGetValue(key, out var value) && _dictionary.Remove(key))
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value));
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _dictionary.Clear();
            OnCollectionReset();
        }

        // Boilerplate forwarding methods
        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value!);

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Contains(item);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
