#if MEMENTO
using System;


namespace Dual.Common.Utils.UndoRedo
{
    /// <summary>
    /// Undo/Redo 에서 제외할 속성 정의
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class UndoRedoIgnoreAttribute : Attribute
    {

    }
}
#endif
