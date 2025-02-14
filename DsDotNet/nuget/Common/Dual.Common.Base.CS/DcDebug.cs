using System.Collections.Generic;
using System.Diagnostics;

namespace Dual.Common.Base.CS
{
    public class DcDebug
    {
        // 각 함수별로 브레이크가 발생한 기록을 저장하는 HashSet
        static HashSet<string> _breakHistory = new HashSet<string>();

        /// 함수별로 한 번만 Debug.Break()를 호출, 같은 함수에서도 라인 번호에 따라 다르게 동작
        public static void BreakOnce()
        {
            // 현재 호출된 함수에 대한 StackFrame 정보 가져오기
            var stackTrace = new StackTrace(true); // true로 설정해 파일 정보와 라인 번호를 가져옴
            var frame = stackTrace.GetFrame(1); // 1번째 호출된 프레임에서 메서드 정보
            var method = frame.GetMethod(); // 메서드 정보 가져옴
            string methodName = $"{method.DeclaringType.FullName}.{method.Name}"; // 메서드 이름과 클래스 정보 포함
            int lineNumber = frame.GetFileLineNumber(); // 라인 번호 가져오기

            // 함수 이름 + 라인 번호 조합
            string uniqueKey = $"{methodName}:{lineNumber}";

            // 해당 함수와 라인 번호 조합이 이미 브레이크된 적이 있는지 확인
            if (!_breakHistory.Contains(uniqueKey))
            {
                // 아직 브레이크된 적이 없다면 Break 실행
                Debugger.Break();
                // 브레이크된 함수와 라인 번호 조합 정보를 기록
                _breakHistory.Add(uniqueKey);
            }
        }

        /// <summary>
        /// VisualStudio IDE 내에서 debugging 중 여부
        /// </summary>
        public static bool IsDebuggerAttached => System.Diagnostics.Debugger.IsAttached;
        /// <summary>
        /// VisualStudio IDE 내에서 debugging 중 여부
        /// </summary>
        public static bool IsInVisualStudio => IsDebuggerAttached;

        /// <summary>
        /// DEBUG flag 설정 여부.  '#if DEBUG' 로 설정할 수 없다.   call site 의 DEBUG 설정 여부에 의해서 결정되어야 한다.
        /// </summary>
        public static bool IsDebugVersion => DcApp.IsDebugVersion;
    }
}
