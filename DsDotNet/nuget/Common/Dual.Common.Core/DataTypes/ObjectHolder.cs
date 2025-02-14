//using System;

//namespace Dual.Common.Core
//{
//    public enum ObjectHolderType
//    {
//        Undefined, 
//        Bool, 
//        Char, 
//        Byte, 
//        Int16, 
//        Int32, 
//        Int64, 
//        UInt16, 
//        UInt32, 
//        UInt64, 
//        Double, 
//        Single,
//        String
//    };
//    public class ObjectHolder
//    {
//        public object NaiveValue { get; set; }
//        public ObjectHolderType Type { get; set; }

//        public ObjectHolder()
//        {
//            NaiveValue = null;
//            Type = ObjectHolderType.Undefined;
//        }
//        public ObjectHolder(object value, ObjectHolderType type)
//        {
//            NaiveValue = value;
//            Type = type;
//        }
//        public static ObjectHolder Create(object value)
//        {
//            var valueType = value switch
//            {
//                bool _ => ObjectHolderType.Bool,
//                char _ => ObjectHolderType.Char,
//                Int16 _ => ObjectHolderType.Int16,
//                Int32 _ => ObjectHolderType.Int32,
//                Int64 _ => ObjectHolderType.Int64,
//                byte _ => ObjectHolderType.Byte,
//                UInt16 _ => ObjectHolderType.UInt16,
//                UInt32 _ => ObjectHolderType.UInt32,
//                UInt64 _ => ObjectHolderType.UInt64,
//                double _ => ObjectHolderType.Double,
//                Single _ => ObjectHolderType.Single,
//                string _ => ObjectHolderType.String,
//                _ => throw new Exception("ERROR")
//            };
//            return new ObjectHolder(value, valueType);
//        }
//        public object GetValue ()
//        {
//            return Type switch
//            {
//                ObjectHolderType.Undefined => throw new Exception("ERROR"),
//                ObjectHolderType.Bool => ((bool)NaiveValue) as object,
//                ObjectHolderType.Char => ((char)NaiveValue),
//                ObjectHolderType.Int16 => (Int16)NaiveValue,
//                ObjectHolderType.Int32 => (Int32)NaiveValue,
//                ObjectHolderType.Int64 => (Int64)NaiveValue,
//                ObjectHolderType.Byte => ((byte)NaiveValue),
//                ObjectHolderType.UInt16 => (UInt16)NaiveValue,
//                ObjectHolderType.UInt32 => (UInt32)NaiveValue,
//                ObjectHolderType.UInt64 =>
//                    NaiveValue switch
//                    {
//                        Int32 _ => (UInt64)(Int32)NaiveValue,
//                        Int64 _ => (UInt64)(Int64)NaiveValue,
//                        _ => (UInt64)NaiveValue
//                    },
//                _ => throw new Exception("ERROR"),
//            };
//        }

//        //public object Value => GetValue();
//        //public object Value => Type switch
//        //{
//        //    ObjectHolderType.Undefined => throw new Exception("ERROR"),
//        //    ObjectHolderType.Bool => ((bool)NaiveValue) as object,
//        //    ObjectHolderType.Int64 => (Int64)NaiveValue,
//        //    ObjectHolderType.UInt64 => (UInt64)((Int64)NaiveValue),
//        //};
//    }
//}
