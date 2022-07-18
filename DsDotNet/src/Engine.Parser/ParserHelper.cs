using System;
using System.Collections.Generic;

namespace DsParser
{
    public class ParserHelper
    {
        public Dictionary<string, object> QualifiedPathMap = new Dictionary<string, object>();
        //public Dictionary<ParserRuleContext, object> ContextMap = new Dictionary<ParserRuleContext, object>();

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



    }
}
