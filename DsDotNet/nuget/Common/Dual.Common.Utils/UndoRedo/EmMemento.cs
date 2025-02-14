#if MEMENTO
using System;
using System.Reactive.Disposables;

using Memento;


namespace Dual.Common.Utils.UndoRedo
{
    public static class EmMemento
    {
        /// <summary>
        /// 여러 operation 에 대해서 Undo/Redo 를 batch 로 1번 적용하는 구간을 생성
        /// </summary>
        public static IDisposable CreateBatchRegion(this Mementor m)
        {
            m.BeginBatch();
            return Disposable.Create(() => m.EndBatch());
        }

        /// <summary>
        /// Undo/Redo 를 적용하지 않는 구간을 생성
        /// </summary>
        public static IDisposable CreateNoTackRegion(this Mementor m)
        {
            var backup = m.IsTrackingEnabled;
            m.IsTrackingEnabled = false;
            return Disposable.Create(() => m.IsTrackingEnabled = backup);
        }
    }
}
#endif
