namespace Server.HW.Common
{
    public enum TagDataType
    {
        Bool,
        Byte,
        Double,
        Int16,
        Int32,
        Int64,
        Sbyte,
        Single,
        Uint16,
        Uint32,
        Uint64,
        String,
    }
    public enum TagIOType
    {
        Input,
        Output,
        Memory,
    }

    /// <summary>
    /// IO 접점.  
    /// </summary>
    public interface ITagHW
    {
        object Value { get; set; }
        string Name { get; set; }
        TagDataType DataType { get; }
        TagIOType IOType { get; }
    }
}
