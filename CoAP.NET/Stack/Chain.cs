/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Com.AugustCellars.CoAP.Stack
{
    /// <summary>
    /// Represents a chain of filters.
    /// </summary>
    /// <typeparam name="TFilter">the type of filters</typeparam>
    /// <typeparam name="TNextFilter">the type of next filters</typeparam>
    public interface IChain<TFilter, TNextFilter>
    {
        /// <summary>
        /// Gets the <see cref="IEntry&lt;TFilter, TNextFilter&gt;"/> with the specified <paramref name="name"/> in this chain.
        /// </summary>
        /// <param name="name">the filter's name we are looking for</param>
        /// <returns>the <see cref="IEntry&lt;TFilter, TNextFilter&gt;"/> with the given name, or null if not found</returns>
        IEntry<TFilter, TNextFilter> GetEntry(string name);

        /// <summary>
        /// Gets the <see cref="IEntry&lt;TFilter, TNextFilter&gt;"/> with the specified <paramref name="filter"/> in this chain.
        /// </summary>
        /// <param name="filter">the filter we are looking for</param>
        /// <returns>the <see cref="IEntry&lt;TFilter, TNextFilter&gt;"/>, or null if not found</returns>
        IEntry<TFilter, TNextFilter> GetEntry(TFilter filter);

        /// <summary>
        /// Gets the <see cref="IEntry&lt;TFilter, TNextFilter&gt;"/> with the specified <paramref name="filterType"/> in this chain.
        /// </summary>
        /// <remarks>If there's more than one filter with the specified type, the first match will be chosen.</remarks>
        /// <param name="filterType">the type of filter we are looking for</param>
        /// <returns>the <see cref="IEntry&lt;TFilter, TNextFilter&gt;"/>, or null if not found</returns>
        IEntry<TFilter, TNextFilter> GetEntry(Type filterType);

        /// <summary>
        /// Gets the <typeparamref name="TFilter"/> with the specified <paramref name="name"/> in this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <returns>the <typeparamref name="TFilter"/>, or null if not found</returns>
        TFilter Get(string name);

        /// <summary>
        /// Gets the <typeparamref name="TNextFilter"/> of the <typeparamref name="TFilter"/>
        /// with the specified <paramref name="name"/> in this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <returns>the <typeparamref name="TNextFilter"/>, or null if not found</returns>
        TNextFilter GetNextFilter(string name);

        /// <summary>
        /// Gets the <typeparamref name="TNextFilter"/> of the <typeparamref name="TFilter"/>
        /// with the specified <paramref name="filter"/> in this chain.
        /// </summary>
        /// <param name="filter">the filter</param>
        /// <returns>the <typeparamref name="TNextFilter"/>, or null if not found</returns>
        TNextFilter GetNextFilter(TFilter filter);

        /// <summary>
        /// Gets the <typeparamref name="TNextFilter"/> of the <typeparamref name="TFilter"/>
        /// with the specified <paramref name="filterType"/> in this chain.
        /// </summary>
        /// <remarks>If there's more than one filter with the specified type, the first match will be chosen.</remarks>
        /// <param name="filterType">the type of filter</param>
        /// <returns>the <typeparamref name="TNextFilter"/>, or null if not found</returns>
        TNextFilter GetNextFilter(Type filterType);

        /// <summary>
        /// Gets all <see cref="IEntry&lt;TFilter, TNextFilter&gt;"/>s in this chain.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEntry<TFilter, TNextFilter>> GetAll();

        /// <summary>
        /// Checks if this chain contains a filter with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <returns>true if this chain contains a filter with the specified <paramref name="name"/></returns>
        bool Contains(string name);

        /// <summary>
        /// Checks if this chain contains the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">the filter</param>
        /// <returns>true if this chain contains the specified <paramref name="filter"/></returns>
        Boolean Contains(TFilter filter);

        /// <summary>
        /// Checks if this chain contains a filter with the specified <paramref name="filterType"/>.
        /// </summary>
        /// <param name="filterType">the filter's type</param>
        /// <returns>true if this chain contains a filter with the specified <paramref name="filterType"/></returns>
        Boolean Contains(Type filterType);

        /// <summary>
        /// Adds the specified filter with the specified name at the beginning of this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        void AddFirst(string name, TFilter filter);

        /// <summary>
        /// Adds the specified filter with the specified name at the end of this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        void AddLast(string name, TFilter filter);

        /// <summary>
        /// Adds the specified filter with the specified name just before the filter whose name is
        /// <paramref name="baseName"/> in this chain.
        /// </summary>
        /// <param name="baseName">the targeted filter's name</param>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        void AddBefore(string baseName, string name, TFilter filter);

        /// <summary>
        /// Adds the specified filter with the specified name just after the filter whose name is
        /// <paramref name="baseName"/> in this chain.
        /// </summary>
        /// <param name="baseName">the targeted filter's name</param>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        void AddAfter(string baseName, string name, TFilter filter);

        /// <summary>
        /// Replace the filter with the specified name with the specified new filter.
        /// </summary>
        /// <param name="name">the name of the filter to replace</param>
        /// <param name="newFilter">the new filter</param>
        /// <returns>the old filter</returns>
        TFilter Replace(string name, TFilter newFilter);

        /// <summary>
        /// Replace the specified filter with the specified new filter.
        /// </summary>
        /// <param name="oldFilter">the filter to replace</param>
        /// <param name="newFilter">the new filter</param>
        void Replace(TFilter oldFilter, TFilter newFilter);

        /// <summary>
        /// Removes the filter with the specified name from this chain.
        /// </summary>
        /// <param name="name">the name of the filter to remove</param>
        /// <returns>the removed filter</returns>
        TFilter Remove(string name);

        /// <summary>
        /// Removes the specified filter.
        /// </summary>
        /// <param name="filter">the filter to remove</param>
        void Remove(TFilter filter);

        /// <summary>
        /// Removes all filters added to this chain.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Abstract implementation of <see cref="IChain&lt;TFilter, TNextFilter&gt;"/>
    /// </summary>
    /// <typeparam name="TChain">the actual type of the chain</typeparam>
    /// <typeparam name="TFilter">the type of filters</typeparam>
    /// <typeparam name="TNextFilter">the type of next filters</typeparam>
    public abstract class Chain<TChain, TFilter, TNextFilter> : IChain<TFilter, TNextFilter>
        where TChain : Chain<TChain, TFilter, TNextFilter>
    {
        private readonly IDictionary<string, Entry> _name2Entry = new Dictionary<string, Entry>();
        private readonly Func<TFilter, TFilter, Boolean> _equalsFunc;
        private readonly Func<TChain, Entry, Entry, string, TFilter, Entry> _entryFactory;

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="nextFilterFactory">the factory to create <typeparamref name="TNextFilter"/>s by (entry)</param>
        /// <param name="headFilterFactory">the factory to create the head <typeparamref name="TFilter"/></param>
        /// <param name="tailFilterFactory">the factory to create the tail <typeparamref name="TFilter"/></param>
        protected Chain(Func<Entry, TNextFilter> nextFilterFactory, Func<TFilter> headFilterFactory, Func<TFilter> tailFilterFactory)
            : this((chain, prev, next, name, filter) => new Entry(chain, prev, next, name, filter, nextFilterFactory),
                headFilterFactory, tailFilterFactory)
        {
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="entryFactory">the factory to create entries by (chain, prev, next, name, filter)</param>
        /// <param name="headFilterFactory">the factory to create the head <typeparamref name="TFilter"/></param>
        /// <param name="tailFilterFactory">the factory to create the tail <typeparamref name="TFilter"/></param>
        protected Chain(Func<TChain, Entry, Entry, string, TFilter, Entry> entryFactory,
                        Func<TFilter> headFilterFactory, Func<TFilter> tailFilterFactory)
            : this(entryFactory, headFilterFactory, tailFilterFactory, (t1, t2) => Object.ReferenceEquals(t1, t2))
        {
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="entryFactory">the factory to create entries by (chain, prev, next, name, filter)</param>
        /// <param name="headFilterFactory">the factory to create the head <typeparamref name="TFilter"/></param>
        /// <param name="tailFilterFactory">the factory to create the tail <typeparamref name="TFilter"/></param>
        /// <param name="equalsFunc">the function to check equality between two <typeparamref name="TFilter"/>s</param>
        protected Chain(Func<TChain, Entry, Entry, string, TFilter, Entry> entryFactory,
                        Func<TFilter> headFilterFactory, Func<TFilter> tailFilterFactory,
                        Func<TFilter, TFilter, Boolean> equalsFunc)
        {
            _equalsFunc = equalsFunc;
            _entryFactory = entryFactory;
            Head = entryFactory((TChain) this, null, null, "head", headFilterFactory());
            Tail = entryFactory((TChain) this, Head, null, "tail", tailFilterFactory());
            Head.NextEntry = Tail;
        }

        /// <summary>
        /// Head of this chain.
        /// </summary>
        protected Entry Head { get; }

        /// <summary>
        /// Tail of this chain.
        /// </summary>
        protected Entry Tail { get; }

        /// <inheritdoc/>
        public IEntry<TFilter, TNextFilter> GetEntry(string name)
        {
            Entry e;
            _name2Entry.TryGetValue(name, out e);
            return e;
        }

        /// <inheritdoc/>
        public TFilter Get(string name)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(name);
            return e == null ? default(TFilter) : e.Filter;
        }

        /// <inheritdoc/>
        public IEntry<TFilter, TNextFilter> GetEntry(TFilter filter)
        {
            Entry e = Head.NextEntry;
            while (e != Tail) {
                if (_equalsFunc(e.Filter, filter)) {
                    return e;
                }

                e = e.NextEntry;
            }

            return null;
        }

        /// <inheritdoc/>
        public IEntry<TFilter, TNextFilter> GetEntry(Type filterType)
        {
            Entry e = Head.NextEntry;
            while (e != Tail) {
#if NETSTANDARD1_3
                if (filterType.GetTypeInfo().IsAssignableFrom(e.Filter.GetType().GetTypeInfo()))
                    return e;
#else
                if (filterType.IsInstanceOfType(e.Filter)) {
                    return e;
                }
#endif
                e = e.NextEntry;
            }

            return null;
        }

        /// <inheritdoc/>
        public TNextFilter GetNextFilter(string name)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(name);
            return e == null ? default : e.NextFilter;
        }

        /// <inheritdoc/>
        public TNextFilter GetNextFilter(TFilter filter)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(filter);
            return e == null ? default : e.NextFilter;
        }

        /// <inheritdoc/>
        public TNextFilter GetNextFilter(Type filterType)
        {
            IEntry<TFilter, TNextFilter> e = GetEntry(filterType);
            return e == null ? default : e.NextFilter;
        }

        /// <inheritdoc/>
        public IEnumerable<IEntry<TFilter, TNextFilter>> GetAll()
        {
            Entry e = Head.NextEntry;
            while (e != Tail) {
                yield return e;
                e = e.NextEntry;
            }
        }

        /// <inheritdoc/>
        public Boolean Contains(string name)
        {
            return GetEntry(name) != null;
        }

        /// <inheritdoc/>
        public Boolean Contains(TFilter filter)
        {
            return GetEntry(filter) != null;
        }

        /// <inheritdoc/>
        public Boolean Contains(Type filterType)
        {
            return GetEntry(filterType) != null;
        }

        /// <inheritdoc/>
        public void AddFirst(string name, TFilter filter)
        {
            CheckAddable(name);
            Register(Head, name, filter);
        }

        /// <inheritdoc/>
        public void AddLast(string name, TFilter filter)
        {
            CheckAddable(name);
            Register(Tail.PrevEntry, name, filter);
        }

        /// <inheritdoc/>
        public void AddBefore(string baseName, string name, TFilter filter)
        {
            Entry baseEntry = CheckOldName(baseName);
            CheckAddable(name);
            Register(baseEntry.PrevEntry, name, filter);
        }

        /// <inheritdoc/>
        public void AddAfter(string baseName, string name, TFilter filter)
        {
            Entry baseEntry = CheckOldName(baseName);
            CheckAddable(name);
            Register(baseEntry, name, filter);
        }

        /// <inheritdoc/>
        public TFilter Replace(string name, TFilter newFilter)
        {
            Entry entry = CheckOldName(name);
            TFilter oldFilter = entry.Filter;
            entry.Filter = newFilter;
            return oldFilter;
        }

        /// <inheritdoc/>
        public void Replace(TFilter oldFilter, TFilter newFilter)
        {
            Entry e = Head.NextEntry;
            while (e != Tail) {
                if (_equalsFunc(e.Filter, oldFilter)) {
                    e.Filter = newFilter;
                    return;
                }

                e = e.NextEntry;
            }

            throw new ArgumentException("Filter not found: " + oldFilter.GetType().Name);
        }

        /// <inheritdoc/>
        public TFilter Remove(string name)
        {
            Entry entry = CheckOldName(name);
            Deregister(entry);
            return entry.Filter;
        }

        /// <inheritdoc/>
        public void Remove(TFilter filter)
        {
            Entry e = Head.NextEntry;
            while (e != Tail) {
                if (_equalsFunc(e.Filter, filter)) {
                    Deregister(e);
                    return;
                }

                e = e.NextEntry;
            }

            throw new ArgumentException("Filter not found: " + filter.GetType().Name);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var entry in _name2Entry.Values) {
                Deregister(entry);
            }
        }

        private void CheckAddable(string name)
        {
            if (_name2Entry.ContainsKey(name)) {
                throw new ArgumentException("Other filter is using the same name '" + name + "'");
            }
        }

        private Entry CheckOldName(string baseName)
        {
            return _name2Entry[baseName];
        }

        private void Register(Entry prevEntry, string name, TFilter filter)
        {
            Entry newEntry = _entryFactory((TChain) this, prevEntry, prevEntry.NextEntry, name, filter);

            OnPreAdd(newEntry);

            prevEntry.NextEntry.PrevEntry = newEntry;
            prevEntry.NextEntry = newEntry;
            _name2Entry.Add(name, newEntry);

            OnPostAdd(newEntry);
        }

        private void Deregister(Entry entry)
        {
            OnPreRemove(entry);
            Deregister0(entry);
            OnPostRemove(entry);
        }

        /// <summary>
        /// Deregister an entry from this chain.
        /// </summary>
        protected void Deregister0(Entry entry)
        {
            Entry prevEntry = entry.PrevEntry;
            Entry nextEntry = entry.NextEntry;
            prevEntry.NextEntry = nextEntry;
            nextEntry.PrevEntry = prevEntry;

            _name2Entry.Remove(entry.Name);
        }

        /// <summary>
        /// Fires before the entry is added to this chain.
        /// </summary>
        protected virtual void OnPreAdd(Entry entry)
        {
        }

        /// <summary>
        /// Fires after the entry is added to this chain.
        /// </summary>
        protected virtual void OnPostAdd(Entry entry)
        {
        }

        /// <summary>
        /// Fires before the entry is removed to this chain.
        /// </summary>
        protected virtual void OnPreRemove(Entry entry)
        {
        }

        /// <summary>
        /// Fires after the entry is removed to this chain.
        /// </summary>
        protected virtual void OnPostRemove(Entry entry)
        {
        }

        /// <summary>
        /// Represents an entry of filter in the chain.
        /// </summary>
        public class Entry : IEntry<TFilter, TNextFilter>
        {
            private TFilter _filter;

            /// <summary>
            /// Instantiates.
            /// </summary>
            /// <param name="chain">the chain this entry belongs to</param>
            /// <param name="prevEntry">the previous one</param>
            /// <param name="nextEntry">the next one</param>
            /// <param name="name">the name of this entry</param>
            /// <param name="filter">the associated <typeparamref name="TFilter"/></param>
            /// <param name="nextFilterFactory">the factory to create <typeparamref name="TNextFilter"/> by (entry)</param>
            public Entry(TChain chain, Entry prevEntry, Entry nextEntry,
                         string name, TFilter filter, Func<Entry, TNextFilter> nextFilterFactory)
            {
                if (filter == null) {
                    throw new ArgumentNullException(nameof(filter));
                }

                Chain = chain;
                PrevEntry = prevEntry;
                NextEntry = nextEntry;
                Name = name ?? throw new ArgumentNullException(nameof(name));
                _filter = filter;
                NextFilter = nextFilterFactory(this);
            }

            /// <inheritdoc/>
            public string Name { get; }

            /// <inheritdoc/>
            public TFilter Filter
            {
                get => _filter;
                set {
                    if (value == null) {
                        throw new ArgumentNullException(nameof(value));
                    }

                    _filter = value;
                }
            }

            /// <inheritdoc/>
            public TNextFilter NextFilter { get; }

            /// <summary>
            /// Gets the chain this entry belongs to.
            /// </summary>
            public TChain Chain { get; }

            /// <summary>
            /// Gets the previous entry in the chain.
            /// </summary>
            public Entry PrevEntry { get; internal set; }

            /// <summary>
            /// Gets the next entry in the chain.
            /// </summary>
            public Entry NextEntry { get; internal set; }

            /// <inheritdoc/>
            public void AddBefore(string name, TFilter filter)
            {
                Chain.AddBefore(Name, name, filter);
            }

            /// <inheritdoc/>
            public void AddAfter(string name, TFilter filter)
            {
                Chain.AddAfter(Name, name, filter);
            }

            /// <inheritdoc/>
            public void Replace(TFilter newFilter)
            {
                Chain.Replace(Name, newFilter);
            }

            /// <inheritdoc/>
            public void Remove()
            {
                Chain.Remove(Name);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                // Add the current filter
                sb.Append("('").Append(Name).Append('\'');

                // Add the previous filter
                sb.Append(", prev: '");

                if (PrevEntry != null) {
                    sb.Append(PrevEntry.Name);
                    sb.Append(':');
                    sb.Append(PrevEntry.Filter.GetType().Name);
                }
                else {
                    sb.Append("null");
                }

                // Add the next filter
                sb.Append("', next: '");

                if (NextEntry != null) {
                    sb.Append(NextEntry.Name);
                    sb.Append(':');
                    sb.Append(NextEntry.Filter.GetType().Name);
                }
                else {
                    sb.Append("null");
                }

                sb.Append("')");
                return sb.ToString();
            }
        }
    }
}
