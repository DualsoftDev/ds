using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dual.Common.Winform
{
    public static class EmTreeList
    {
        /// <summary>
        /// treelist 의 node 및 그 하부의 모든 node 들을 열거
        /// </summary>
        public static IEnumerable<TreeListNode> Populate(TreeListNode node)
        {
            yield return node;
            foreach (var n in node.Nodes.SelectMany(node => Populate(node)))
                yield return n;
        }

        /// <summary>
        /// treelist 하부의 모든 node 들을 열거
        /// </summary>
        public static IEnumerable<TreeListNode> Populate(this TreeList treelist)
        {
            foreach (var n in treelist.Nodes.SelectMany(node => Populate(node)))
                yield return n;
        }

        /// <summary>
        /// treelist 상의 node 의 경로를 문자열로 반환
        /// </summary>
        public static string GetPath(this TreeListNode node, Func<TreeListNode, string> nameExtractor, string separator="/")
        {
            IEnumerable<string> CollectPathComponnetUp(TreeListNode node)
            {
                yield return nameExtractor(node);
                if (node.ParentNode != null)
                {
                    foreach (var s in CollectPathComponnetUp(node.ParentNode))
                        yield return s;
                }
            }
            return string.Join(separator, CollectPathComponnetUp(node).Reverse());
        }

        /// <summary>
        /// Treelist 에서 문자열로 주어진 경로에 해당하는 node 를 찾아서 반환
        /// </summary>
        public static TreeListNode GetNode(TreeList treelist, Func<TreeListNode, string> nameExtractor, string path, string separator = "/")
        {
            return treelist.FindNode(node => node.GetPath(nameExtractor) == path);
        }
    }
}
