using System;

namespace Dual.Common.Utils
{
    /// <summary>
    /// 캐시 아이템
    /// </summary>
    [Serializable]
    public class CacheItem
    {
        /// <summary>
        /// 캐시 기간
        /// </summary>
        public TimeSpan? CacheDuration = null;

        /// <summary>
        /// 캐시된 시각
        /// </summary>
        public DateTime? CachedDateTime = null;

        /// <summary>
        /// 캐시될 개체
        /// </summary>
        public object CachedDataObject = null;

        /// <summary>
        /// 캐시될 개체를 조회할 함수 
        /// </summary>
        public Func<object, object> CacheFunction = null;

        /// <summary>
        /// 매개변수
        /// </summary>
        public object CacheArg = null;
    }
}
