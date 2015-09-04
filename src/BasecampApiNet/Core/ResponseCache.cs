﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Caching;
using BasecampApiNet.Helpers;
using BasecampApiNet.Models;

namespace BasecampApiNet.Core
{
    public class ResponseCache
    {
        static readonly object padlock = new object();
        private MemoryCache _cache = new MemoryCache("BasecampApiCache");

        private void _add(string key, CacheWrapperModel value)
        {
            lock (padlock)
            {
                _cache.Add(key, value, DateTimeOffset.MaxValue);
            }
        }

        private void _remove(string key)
        {
            lock (padlock)
            {
                _cache.Remove(key);
            }
        }

        private object _get(string key)
        {
            lock (padlock)
            {
                return _cache.Get(key);
            }
        }

        internal T Get<T>(string url)
        {
            lock (padlock)
            {
                //send request, and last time retrieved date
                var currentCacheItem = _get(url);
                var eTag = "";

                //if in the cache, set the date for the request
                if (currentCacheItem != null)
                {
                    eTag = ((CacheWrapperModel) currentCacheItem).ETag;
                }

                var response = WebHelper.Get(url, eTag).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //remove the current
                    _remove(url);

                    var valueToCache = new object();

                    var baseType = _getEnumerableType(typeof(T));

                    //if enumerable
                    if (baseType != null)
                    {
                        valueToCache = response.Content.ReadAsStringAsync().Result.AsListModel<T>();    
                    }
                    else
                    {
                        //must be a single result
                        valueToCache = response.Content.ReadAsStringAsync().Result.AsModel<T>();
                    }

                    //add to cache
                    _add(url, new CacheWrapperModel()
                    {
                        ETag = response.Headers.ETag.ToString(),
                        LastRequested = DateTime.UtcNow,
                        Value = valueToCache,
                        TypeString = typeof(T).ToString()
                    });

                    return (T)valueToCache;
                }
                else if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    //get from cache
                    var wrapper = (CacheWrapperModel) _cache.Get(url);

                    //return the value of the cache item as an IEnumerable
                    return (T) wrapper.Value;
                }
                else
                {
                    throw new Exception("Basecamp returned an unexpected status code of: " + response.StatusCode);
                }
            }
        }

        internal Dictionary<string, CacheWrapperModel> CacheDump()
        {
            var list = new Dictionary<string, CacheWrapperModel>();

            foreach (var item in _cache)
            {
                var wrapped = (CacheWrapperModel) item.Value;

                list.Add(item.Key, wrapped);
            }

            return list;
        }

        private Type _getEnumerableType(Type type)
        {
            foreach (Type intType in type.GetInterfaces())
            {
                if (intType.IsGenericType
                    && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return intType.GetGenericArguments()[0];
                }
            }
            return null;
        }
    }
}
