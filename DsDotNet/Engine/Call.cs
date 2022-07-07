namespace Engine
{
    public class Call : SegmentOrCallBase
    {
        public Task Task;
        public Segment TX;
        public Segment RX;

        public Call(string name, Task task)
            : base(name)
        {
            Task = task;
            task.Calls.Add(this);
        }
    }
}
