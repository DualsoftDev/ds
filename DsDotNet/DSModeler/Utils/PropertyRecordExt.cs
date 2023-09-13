using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DSModeler.Utils
{
    public static class PropertyRecordExt
    {
        public static object Propertize(this IEnumerable<Vertex> vertices)
        {
            if (vertices != null && vertices.Any())
            {
                Vertex[] records = vertices.ToArray();
                return records.Length == 1 ? records[0] : new PropertyRecords(vertices);
            }
            return null;
        }

    }

    /// <summary>
    /// 주로 복수개의 속성를 속성창에 표시하기 위한 decorator class
    /// </summary>
    public class PropertyRecords
    {
        private readonly string _multi = "중복속성";
        private readonly Vertex[] _vertices;
        public PropertyRecords(IEnumerable<Vertex> vertices)
        {
            Debug.Assert(vertices.Count() > 1);
            _vertices = vertices.ToArray();
        }

        private string strSelector(Func<Vertex, string> func)
        {
            return _vertices.Select(func).Distinct().Count() == 1 ? func(_vertices[0]) : _multi;
        }

        [DisplayName("오브젝트 이름"), Editable(false)]
        public string PartName => strSelector(r => r.Name);

        [DisplayName("그룹 이름")]
        public string GroupName => strSelector(r => r.QualifiedName);
    }
}
