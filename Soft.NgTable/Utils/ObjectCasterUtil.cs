using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Soft.NgTable.Models;

namespace Soft.NgTable.Utils
{
    public static class ObjectCasterUtil
    {
        public static object CastPropertiesTypeList(Type propertyType, object value)
        {
            var arrayCast = (JArray) value;
            if (propertyType == typeof(int))
                return arrayCast.ToObject<List<int>>();
            if (propertyType == typeof(int?))
                return arrayCast.ToObject<List<int?>>();
            if (propertyType == typeof(double))
                return arrayCast.ToObject<List<double>>();
            if (propertyType == typeof(double?))
                return arrayCast.ToObject<List<double?>>();
            if (propertyType == typeof(DateTime))
                return arrayCast.ToObject<List<DateTime>>();
            if (propertyType == typeof(DateTime?))
                return arrayCast.ToObject<List<DateTime?>>();
            if (propertyType == typeof(bool))
                return arrayCast.ToObject<List<bool>>();
            if (propertyType == typeof(bool?))
                return arrayCast.ToObject<List<bool?>>();
            if (propertyType == typeof(short))
                return arrayCast.ToObject<List<short>>();
            if (propertyType == typeof(short?))
                return arrayCast.ToObject<List<short?>>();
            if (propertyType == typeof(long))
                return arrayCast.ToObject<List<long>>();
            if (propertyType == typeof(long?))
                return arrayCast.ToObject<List<long?>>();
            if (propertyType == typeof(float))
                return arrayCast.ToObject<List<float>>();
            if (propertyType == typeof(float?))
                return arrayCast.ToObject<List<float?>>();
            if (propertyType == typeof(decimal))
                return arrayCast.ToObject<List<decimal>>();
            if (propertyType == typeof(decimal?))
                return arrayCast.ToObject<List<decimal?>>();
            if (propertyType == typeof(byte))
                return arrayCast.ToObject<List<byte>>();
            if (propertyType == typeof(byte?))
                return arrayCast.ToObject<List<byte?>>();

            return arrayCast.ToObject<List<string>>();
        }

        public static object CastPropertiesType(Type propertyType, object value)
        {
                
            if (propertyType == typeof(int))
                return Convert.ToInt32(value);
            if (propertyType == typeof(int?))
                return Convert.ToInt32(value);
            if (propertyType == typeof(double))
                return Convert.ToDouble(value);
            if (propertyType == typeof(double?))
                return Convert.ToDouble(value);
            if (propertyType == typeof(DateTime))
                return Convert.ToDateTime(value);
            if (propertyType == typeof(DateTime?))
                return Convert.ToDateTime(value);
            if (propertyType == typeof(bool))
                return Convert.ToBoolean(value);
            if (propertyType == typeof(bool?))
                return Convert.ToBoolean(value);
            if (propertyType == typeof(short))
                return Convert.ToInt16(value);
            if (propertyType == typeof(short?))
                return Convert.ToInt16(value);
            if (propertyType == typeof(long))
                return Convert.ToInt64(value);
            if (propertyType == typeof(long?))
                return Convert.ToInt64(value);
            if (propertyType == typeof(float))
                return Convert.ToSingle(value);
            if (propertyType == typeof(float?))
                return Convert.ToSingle(value);
            if (propertyType == typeof(decimal))
                return Convert.ToDecimal(value);
            if (propertyType == typeof(decimal?))
                return Convert.ToDecimal(value);
            if (propertyType == typeof(byte))
                return Convert.ToByte(value);
            if (propertyType == typeof(byte?))
                return Convert.ToByte(value);

            return value.ToString();
        }

        public static TableFilterContext CastJObjectToTableFilterContext(JObject obj)
            => obj.ToObject<TableFilterContext>();
    }
}