// http://www.codeproject.com/Articles/24710/Multiple-Inheritance-in-C
// 다중 상속 (개념)을 사용하는 클래스들의 base class

using System;
using System.Collections.Generic;

namespace Dual.Common.Core
{
    public class CDynamicClassBase : MarshalByRefObject
    {
        private const string STR_AttributeNotFound = "The requested '{0}' attribute at the class '{1}' was not found !";
        private const string STR_DupliciteAttributeFound = "A duplicite '{0}' attribute was detected on the class '{1}' !";

        private readonly Dictionary<Type, Object> attributes = null;

        public Dictionary<Type, Object> Attributes
        {
            get { return attributes; }
        }

        public CDynamicClassBase()
        {
            attributes = GetAttributes(GetType());
        }

        private Dictionary<Type, Object> GetAttributes(Type sourceType)
        {
            Object[] collection = sourceType.GetCustomAttributes(true);
            Dictionary<Type, Object> result = new Dictionary<Type, Object>();

            foreach (Object attribute in collection)
            {
                Type attributeType = attribute.GetType();

                if (result.ContainsKey(attributeType))
                {
                    throw new Exception(string.Format(STR_DupliciteAttributeFound, attributeType.Name, sourceType.Name));
                }
                else
                {
                    result.Add(attributeType, attribute);
                }
            }

            return result;
        }

        public Boolean Check<TAttribute>()
        {
            return attributes.ContainsKey(typeof(TAttribute));
        }

        public Boolean Is<TAncestor>() where TAncestor : CDynamicClassBase
        {
            Boolean result = true;
            Dictionary<Type, Object> sourceList = Attributes;
            Dictionary<Type, Object> destinationList = GetAttributes(typeof(TAncestor));

            foreach (KeyValuePair<Type, Object> destinationPair in destinationList)
            {
                result = result && sourceList.ContainsKey(destinationPair.Key);
            }

            return result;
        }

        public TAttribute Use<TAttribute>()
        {
            if (Check<TAttribute>())
            {
                return (TAttribute)attributes[typeof(TAttribute)];
            }
            else
            {
                throw new ArgumentNullException(string.Format(STR_AttributeNotFound, typeof(TAttribute).Name, GetType().Name));
            }
        }
    }
}
