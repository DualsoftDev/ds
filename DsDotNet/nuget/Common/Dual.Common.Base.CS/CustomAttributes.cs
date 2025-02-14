using System;

namespace Dual.Common.Base.CS
{
    /// <summary>
    /// Type 이 serialize 대상임을 나타내는 코멘트 성격의 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class SerializableTypeAttribute : Attribute
    {
        public string Comment { get; }

        public SerializableTypeAttribute(string comment = null)
        {
            Comment = comment;
        }
    }

    /// <summary>
    /// UI 에 영향을 미치거나, UI 를 고려해야 하는 class/type 임을 나타내는 코멘트 성격의 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class UIElementTypeAttribute : Attribute
    {
        public string Comment { get; }

        public UIElementTypeAttribute(string comment = null)
        {
            Comment = comment;
        }
    }

    /// <summary>
    /// Todo 목록을 나타내는 코멘트 성격의 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true)]
    public class TodoAttribute : Attribute
    {
        public string Comment { get; }

        public TodoAttribute(string comment = null)
        {
            Comment = comment;

            // 컴파일 타임에 경고 메시지를 표시: [<Todo()>] Attribute 를 사용한 코드를 찾아서 처리할 것
//#pragma warning disable 1591 // Disable XML comment warnings (if necessary)
//#warning Something todo!!
//#pragma warning restore 1591 // Restore warnings (if disabled above)
        }
    }

}
