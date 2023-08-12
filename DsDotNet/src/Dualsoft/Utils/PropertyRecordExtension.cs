using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using DevExpress.Utils.Text;
using Dual.Common.Core;
using static Engine.Core.CoreModule;

namespace DSModeler
{
    public static class PropertyRecordExtension
    {
        public static object Propertize(this IEnumerable<Vertex> vertices)
        {
            if (vertices.NonNullAny())
            {
                var records = vertices.ToArray();
                if (records.Length == 1)
                    return records[0];
                return new PropertyRecords(vertices);
            }
            return null;
        }

    }

    /// <summary>
    /// 주로 복수개의 속성를 속성창에 표시하기 위한 decorator class
    /// </summary>
    public class PropertyRecords
    {
        string _multi = "중복속성";
        Vertex[] _vertices;
        public PropertyRecords(IEnumerable<Vertex> vertices)
        {
            Debug.Assert(vertices.Count() > 1);
            _vertices = vertices.ToArray();
        }

        string strSelector(Func<Vertex, string> func) => _vertices.Select(func).Distinct().Count() == 1 ? func(_vertices[0]) : _multi;
      
        [DisplayName("오브젝트 이름"), Editable(false)]
        public string PartName => strSelector(r => r.Name);

        [DisplayName("그룹 이름")]
        public string GroupName => strSelector(r => r.QualifiedName);
    }
}
