using System;
using System.Collections;

namespace Dual.Common.Utils
{
    /// <summary>
    /// 개체의 캐시기능을 제공합니다.
    /// </summary>
    public class CacheManager
    {
        /// <summary>
        /// 저장소 - 해시테이블
        /// </summary>
        private Hashtable list = new Hashtable();

        /// <summary>
        /// 캐시에 개체를 추가합니다.
        /// </summary>
        /// <param name="key">접근 키값</param>
        /// <param name="cacheFunction">개체조회를 위한 함수값</param>
        /// <param name="argument">개체조회를 위한 함수 실행시 입력값</param>
        /// <param name="duration">개체의 캐시 유지기간</param>
        public void Cache(string key, Func<object, object> cacheFunction, object argument, TimeSpan duration)
        {
            CacheItem item = new CacheItem()
            {
                CachedDataObject = cacheFunction(argument),
                CachedDateTime = DateTime.Now,
                CacheDuration = duration,
                CacheFunction = cacheFunction,
                CacheArg = argument
            };

            list.Add(key, item);
        }

        /// <summary>
        /// 키 값이 존재하는지를 반환합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return list.ContainsKey(key);
        }

        /// <summary>
        /// 캐시된 값을 반환합니다. 없을 경우 null값을 반환합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetCachedObject(string key)
        {
            object result = null;

            if (list.ContainsKey(key))
            {
                CacheItem item = list[key] as CacheItem;
                DateTime current = DateTime.Now;
                TimeSpan t = new TimeSpan(current.Ticks - item.CachedDateTime.Value.Ticks);

                if (t > item.CacheDuration.Value)
                {
                    list.Remove(key);
                    Cache(key, item.CacheFunction, item.CacheArg, item.CacheDuration.Value);

                    return GetCachedObject(key);
                }
                else
                {
                    result = item.CachedDataObject;
                }
            }

            return result;
        }

        /// <summary>
        /// 캐시를 모두 지웁니다.
        /// </summary>
        public void Clear()
        {
            list.Clear();
        }

        /// <summary>
        /// 해당 키의 캐시를 제거합니다.
        /// </summary>
        public void Clear(string key)
        {
            if (list.ContainsKey(key))
                list.Remove(key);
        }
    }
}
