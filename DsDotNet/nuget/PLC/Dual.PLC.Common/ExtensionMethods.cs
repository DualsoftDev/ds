using LanguageExt;

namespace Dual.PLC.Common
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Option 에서 Some 값을 강제 반환한다.  만일 None 값에 대해서 호출하면, ValueIsNoneException exception 이 발생한다.
		/// Dual.Common.Utilities.ExtensionMethods.EmLanguageExt.GetValueUnsafe() 와 동일.  Dsu.Common.Utilities 를 reference 하지 않으므로 복사본 보유.
		/// </summary>
		public static T GetValueUnsafe<T>(this Option<T> opt)
		{
			if (opt.IsNone)
				throw new ValueIsNoneException();

			return opt.IfNone(default(T));
		}
	}
}
