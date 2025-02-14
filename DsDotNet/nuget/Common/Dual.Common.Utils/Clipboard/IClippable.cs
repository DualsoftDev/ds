namespace Dual.Common.Utils
{
    /// <summary>
    /// 단일 프로그램 내에서 copy/paste 를 지원하기 위한 interface.
    /// </summary>
    public interface IIntraProgramClipContainer
    {
        IIntraProgramClipItem IntraProgramClipItem { get; set; }
        void Copy(IIntraProgramClipItem item);
    }

    public interface IIntraProgramClipItem
    {
    }

    /// <summary>
    /// 프로그램 내에서의 data 복사를 위한 interface.
    /// 단일 프로그램 내에서의 복사만 허용하므로, 복사될 data 는 serializable 일 필요가 없다.
    /// </summary>
    public interface IIntraProgramClipWrapper : IIntraProgramClipItem
    {
        /// <summary>
        /// 실제 copy/paste 할 data
        /// </summary>
        object ClipData { get; }
    }
}
