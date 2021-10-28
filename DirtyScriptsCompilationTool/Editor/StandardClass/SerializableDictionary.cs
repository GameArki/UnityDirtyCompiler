using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalFrame.EditorTool {

    [Serializable]
    public class SerializableDictionary<TKey, TValue> {

        [SerializeField]
        List<TKey> keyList;

        [SerializeField]
        List<TValue> valueList;

        public SerializableDictionary() {
            this.keyList = new List<TKey>();
            this.valueList = new List<TValue>();
        }

        public void Add(TKey key, TValue value) {
            keyList.Add(key);
            valueList.Add(value);
        }

        public bool Contains(TKey key) {
            return keyList.Contains(key);
        }

        public void Remove(TKey key) {
            int index = keyList.IndexOf(key);
            valueList.RemoveAt(index);
            keyList.RemoveAt(index);
        }

        public bool TryGetValue(TKey key, out TValue value) {
            int index = keyList.IndexOf(key);
            if (index == -1) {
                value = default;
                return false;
            }
            value = valueList[index];
            return true;
        }

        public void Clear() {
            keyList.Clear();
            valueList.Clear();
        }

    }

}