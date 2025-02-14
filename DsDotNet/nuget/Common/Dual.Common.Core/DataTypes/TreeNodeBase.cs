using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// https://www.codeproject.com/Articles/345191/Simple-Generic-Tree

namespace Dual.Common.Core
{
    public interface ITreeNode<T>
    {
        T Parent { get; set; }
        bool IsLeaf { get; }
        bool IsRoot { get; }
        T GetRootNode();

        string GetFullyQualifiedName(string separator);


        /* 확장 */
        string Name { get; }
        T GetDescendant(params string[] descendantnames);
        IEnumerable<T> GetDescendants(bool includeMe);
    }

    /// Generic Tree Node base class
    public abstract class TreeNodeBase<T> : ITreeNode<T> where T : class, ITreeNode<T>
    {
        protected TreeNodeBase(string name)
            : this()
        {
            Name = name;
        }
        [JsonConstructor]
        protected TreeNodeBase()
        {
            ChildNodes = new List<T>();
        }


        [JsonProperty()]
        public string Name { get; protected set; }

        [JsonProperty()]
        public T Parent { get; set; }

        [JsonProperty()]
        public List<T> ChildNodes { get; protected set; }

        /// <summary>
        /// 실제 tree node instance 의 객체.  RTTI(Runtime type identification)을 위해서 필요함.
        /// </summary>
        protected abstract T MySelf { get; }

        [JsonIgnore]
        public bool IsLeaf => ChildNodes.Count == 0;
        [JsonIgnore]
        public bool IsRoot => Parent == null;

        public IEnumerable<T> GetLeafNodes() => ChildNodes.Where(x => x.IsLeaf);

        public IEnumerable<T> GetStemNodes() => ChildNodes.Where(x => !x.IsLeaf);


        public T GetRootNode()
        {
            if (Parent == null)
                return MySelf;

            return Parent.GetRootNode();
        }

        /// Dot separated name from the Root to this Tree Node
        public string GetFullyQualifiedName(string separator=".")
        {
            // TreeListNode 에서 사용하는 dummy 를 고려하여 최상위 node 는 skip 한다.
            if (Parent == null)
                return null;

            var parentName = Parent.GetFullyQualifiedName(separator);
            if (parentName == null)
                return Name;

            return $"{parentName}{separator}{Name}";
        }
        public T GetDescendant(params string[] descendantnames)
        {
            if (descendantnames.Length == 0)
                return MySelf;

            var child = ChildNodes.FirstOrDefault(c => c.Name == descendantnames[0]);
            return child?.GetDescendant(descendantnames.Skip(1).ToArray());
        }
        public IEnumerable<T> GetDescendants(bool includeMe=true)
        {
            if (includeMe)
                yield return MySelf;
            foreach(var child in ChildNodes)
            {
                foreach (var grandson in child.GetDescendants(true))
                {
                    yield return grandson;
                }
            }
        }

        public bool AddChild(T child)
        {
            if (ChildNodes.Contains(child))
                return false;

            child.Parent = MySelf;
            ChildNodes.Add(child);
            return true;
        }

        public void AddChildren(IEnumerable<T> children)
        {
            foreach (T child in children)
                AddChild(child);
        }

        public void RemoveChild(T child)
        {
            child.Parent = null;
            ChildNodes.Remove(child);
        }
    }
}
