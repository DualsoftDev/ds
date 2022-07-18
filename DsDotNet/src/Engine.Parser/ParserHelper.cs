using Engine.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DsParser
{
    public class ParserHelper
    {
        public Dictionary<string, object> QualifiedPathMap = new Dictionary<string, object>();
        //public Dictionary<ParserRuleContext, object> ContextMap = new Dictionary<ParserRuleContext, object>();



        public Model Model { get; } = new Model();
        internal DsSystem _system;
        internal DsTask _task;
        internal RootFlow _rootFlow;
        internal Segment _parenting;

        internal string CurrentPath
        {
            get
            {
                if (_task != null)
                    return $"{_system.Name}.{_task.Name}";
                if (_parenting != null)
                    return $"{_system.Name}.{_rootFlow.Name}.{_parenting.Name}";
                if (_rootFlow != null)
                    return $"{_system.Name}.{_rootFlow.Name}";
                if (_system != null)
                    return _system.Name;

                throw new Exception("ERROR");
            }
        }

        public T FindObject<T>(string qualifiedName) where T : class => PickQualifiedPathObject<T>(qualifiedName);

        public T[] FindObjects<T>(string qualifiedNames) where T : class
        {
            if (qualifiedNames == "_")
                return Array.Empty<T>();

            return
                qualifiedNames
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(name => FindObject<T>(name))
                    .ToArray()
                    ;
        }


        T PickQualifiedPathObject<T>(string qualifiedName, Func<T> creator = null) where T : class
        {
            var dict = QualifiedPathMap;
            if (dict.ContainsKey(qualifiedName))
                return (T)dict[qualifiedName];

            if (creator == null)
                throw new Exception("ERROR");

            var t = creator();
            dict[qualifiedName] = t;

            return t;
        }


        public static string ToFQDN(string name, DsSystem system, string middleName, string parentingName)
        {
            string concat(params string[] names) =>
                String.Join(".", names.Where(n => n != null))
                ;
            var sysName = system.Name;
            var tasks = system.Tasks.Select(t => t.Name);
            if (tasks.Any(t => name.StartsWith($"{t}.")))
                return concat(sysName, name);

            var nameComponents = name.Split(new[] { '.' }).ToArray();
            var mid = name.StartsWith($"{middleName}.") ? null : middleName;
            var par = name.StartsWith($"{parentingName}.") ? null : parentingName;
            switch (nameComponents.Length)
            {
                case 1:
                    if (system.AliasNameMap.ContainsKey(name))
                        //return system.AliasNameMap[name];
                        return name;
                    return concat(sysName, middleName, parentingName, name);
                case 2:
                    Debug.Assert(!name.StartsWith(sysName));
                    return concat(sysName, mid, par, name);
                case 3:
                    return name;
                default:
                    throw new Exception("ERROR");
            }
        }


    }
}
