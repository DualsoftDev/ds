using System.Globalization;
using System.Threading;

namespace Dual.Common.Core
{
    public static class EmCulture
    {
        /// <summary>
        /// Form 이나 Control 생성 이전에 global 하게 먼저 호출되어야 한다.
        /// </summary>
        /// <param name="ci"></param>
        public static void Apply(this CultureInfo ci)
        {
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        /// <summary>
        /// cultureName = { "en-US", "ko-KR", ... }.   CultureInfo.CurrentUICulture.Name
        /// </summary>
        /// <param name="cultureName"></param>
        public static void Apply(string cultureName) => new CultureInfo(cultureName).Apply();

        /// <summary>
        /// 영어와 한국어 중에서 현재 culture 에 맞는 문자열 반환
        /// </summary>
        public static string PickEK(string english, string korean) =>
            (Thread.CurrentThread.CurrentUICulture.Name == "ko-KR") ? korean : english;
    }
}
