﻿using System;
using System.Collections.Generic;
using Suteki.Shop.Extensions;

namespace Suteki.Shop.ViewData
{
    public class ScaffoldViewData<T> : IMessageViewData, IErrorViewData
    {
        public string ErrorMessage { get; set; }
        public string Message { get; set; }

        public IEnumerable<T> Items { get; set; }
        public T Item { get; set; }

        private Dictionary<Type, object> lookupLists = new Dictionary<Type, object>();

        public ScaffoldViewData<T> With(T item)
        {
            this.Item = item;
            return this;
        }

        public ScaffoldViewData<T> With(IEnumerable<T> items)
        {
            this.Items = items;
            return this;
        }

        public ScaffoldViewData<T> WithErrorMessage(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
            return this;
        }

        public ScaffoldViewData<T> WithMessage(string message)
        {
            this.Message = message;
            return this;
        }

        public ScaffoldViewData<T> WithLookupList(Type entityType, object items)
        {
            lookupLists.Add(entityType, items);
            return this;
        }

        public IEnumerable<TLookup> GetLookUpList<TLookup>()
        {
            if (!lookupLists.ContainsKey(typeof(TLookup)))
            {
                throw new ApplicationException("List of type {0} does not exist in lookup list".With(typeof(TLookup).Name));
            }
            return (IEnumerable<TLookup>)lookupLists[typeof(TLookup)];
        }
    }

    public static class Scaffold
    {
        public static ScaffoldViewData<T> Data<T>()
        {
            return new ScaffoldViewData<T>();
        }
    }
}