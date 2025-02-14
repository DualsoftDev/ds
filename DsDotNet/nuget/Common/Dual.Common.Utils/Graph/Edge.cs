/*
 * http://www.Planet-Source-Code.com/vb/scripts/ShowCode.asp?txtCodeId=4587&lngWId=10
 * http://stackoverflow.com/questions/10032940/iterative-connected-components-algorithm
 */


using System;

namespace Dual.Common.Utils.Graph
{
    /// Represents additional data which is attached to an edge.
    /// <typeparam name="N">The node type</typeparam>
    /// <typeparam name="E">The edge type</typeparam>
    public struct Edge<N, E>
        where N : IEquatable<N>
    {
        private E data;
        private N start;
        private N end;

        /// The attached data
        public E Data { get { return data; } set { data = value; } }
        public N Start { get { return start; } }
        public N End { get { return end; } }

        /// <param name="obj">Another object to compare to.</param>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Edge<N, E> edge = (Edge<N, E>)obj;
            return (edge.start.Equals(start) && edge.end.Equals(end));
        }
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode() { return start.GetHashCode() ^ end.GetHashCode(); }

        /// Creates a new instance of the Edge structure.  꼭 필요한 경우 아니면 호출 자제... internal
        /// <param name="start">The start node</param>
        /// <param name="end">The end node</param>
        /// <param name="data">The attached data</param>
        public Edge(N start, N end, E data)
        {
            this.start = start;
            this.end = end;
            this.data = data;
        }
    }
}


