using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsParser
{
    public class Model
    {
        public List<DsSystem> Systems = new List<DsSystem>();
        public List<Cpu> Cpus = new List<Cpu>();
    }

    public class Named
    {
        public string Name;

        public Named(string name)
        {
            Name = name;
        }
    }


public class DsSystem : Named
    {
        public Model Model;
        public List<Flow> Flows = new List<Flow>();
        public List<Task> Tasks = new List<Task>();
        public DsSystem(string name, Model model)
            : base(name)
        {
            Model = model;
            model.Systems.Add(this);
        }
    }

    public class Flow : Named
    {
        public DsSystem System;
        public List<Segment> Segments = new List<Segment>();
        public List<Edge> Edges = new List<Edge>();

        public Flow(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.Flows.Add(this);
        }
    }

    public class Task : Named
    {
        public DsSystem System;
        public List<Call> Calls = new List<Call>();

        public Task(string name, DsSystem system)
            : base(name)
        {
            System = system;
            system.Tasks.Add(this);
        }
    }
    public class Cpu
    {
        public List<Flow> Flows = new List<Flow>();
    }

    public class Segment : Named
    {
        public Flow Flow;
        public Segment(string name, Flow flow)
            : base(name)
        {
            Flow = flow;
            flow.Segments.Add(this);
        }
    }

    public class RootSegment: Segment
    {
        public List<Segment> Children = new List<Segment>();
        public RootSegment(string name, Flow flow)
            : base(name, flow)
        {
        }
    }

    public class Call : Named
    {
        public Task Task;

        public Call(string name, Task task)
            : base(name)
        {
            Task = task;
            task.Calls.Add(this);
        }
    }

    public class Edge
    {
    }
}
