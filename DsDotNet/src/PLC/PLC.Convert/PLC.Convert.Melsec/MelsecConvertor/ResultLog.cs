namespace MelsecConverter
{
    public enum ResultCase
    {
        Program,
        Address,
        System,
    }

    public enum ResultData
    {
        Success,
        Warning,
        Failure,
    }

    internal class ResultLog
    {
        public ResultCase Case { get; set; }
        public ResultData Result { get; set; }
        public string Program { get; set; }
        public string Message { get; set; }


        public override string ToString()
        {
            return $"[{Case}] {Result}: {Message}";
        }
    }
}
