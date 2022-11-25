using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using AbpProcedure.Base.Helpers;

namespace AbpProcedure.Base
{
    public static class DynamicCreater
    {
        private static List<DynamicProperty> GetDynamicProperties(DataSet datas)
        {
            var listProperties = new List<DynamicProperty>();
            if (datas.Tables.Count > 0)
            {
                var table = datas.Tables[0];
                foreach (DataRow dr in table.Rows)
                {
                    foreach (DataColumn cl in table.Columns)
                    {
                        var dynamicProperty = new DynamicProperty();
                        dynamicProperty.PropertyName = cl.ColumnName;
                        dynamicProperty.DisplayName = cl.ColumnName;
                        dynamicProperty.SystemTypeName = cl.DataType.ToString();
                        listProperties.Add(dynamicProperty);
                    }
                    break;
                }
            }

            return listProperties;
        }

        public static List<object> GetListObject(DataSet dataSet)
        {
            var res = new List<object>();
            // Tạo Type
            var dynamicProperties = GetDynamicProperties(dataSet);
            var factory = new DynamicTypeFactory();
            var extendedType = factory.CreateNewTypeWithDynamicProperties(typeof(ObjectReturn), dynamicProperties);
            // Tạo object và gán value
            if (dataSet.Tables.Count > 0)
            {
                var table = dataSet.Tables[0];
                object extendedObject = null;
                foreach (DataRow dr in table.Rows)
                {
                    extendedObject = Activator.CreateInstance(extendedType);
                    foreach (DataColumn cl in table.Columns)
                    {
                        if (dr[cl].GetType() != typeof(System.DBNull))
                        {
                            extendedType.GetProperty(cl.ColumnName).SetValue(extendedObject, dr[cl], null);
                        }
                    }
                    res.Add(extendedObject);
                }
            }

            return res;
        }

        public static List<object> GetListObjectWithoutKey(DataSet dataSet)
        {
            var res = new List<object>();
            // Tạo Type
            var dynamicProperties = GetDynamicProperties(dataSet);
            var factory = new DynamicTypeFactory();
            var extendedType = factory.CreateNewTypeWithDynamicProperties(typeof(ObjectReturnWithoutKey), dynamicProperties);
            // Tạo object và gán value
            if (dataSet.Tables.Count > 0)
            {
                var table = dataSet.Tables[0];
                object extendedObject = null;
                foreach (DataRow dr in table.Rows)
                {
                    extendedObject = Activator.CreateInstance(extendedType);
                    foreach (DataColumn cl in table.Columns)
                    {
                        if (dr[cl].GetType() != typeof(System.DBNull))
                        {
                            extendedType.GetProperty(cl.ColumnName)
                                 .SetValue(extendedObject, dr[cl], null);
                        }
                    }
                   
                    res.Add(extendedObject);
                }
            }

            return res;
        }

        public static List<IDictionary<string, object>> GetDictionarys(DataSet dataSet)
        {
            var res = new List<IDictionary<string, object>>();
            if (dataSet.Tables.Count > 0)
            {
                var table = dataSet.Tables[0];
                foreach (DataRow dr in table.Rows)
                {
                    var row = new Dictionary<string, object>();
                    foreach (DataColumn cl in table.Columns)
                    {
                        row[cl.ColumnName] = dr[cl];
                    }

                    res.Add(row);
                }
            }

            return res;
        }

        public static List<T> GetListWithType<T>(DataSet dataSet)
        {
            var res = new List<T>();
            // Tạo Type
            var dynamicProperties = GetDynamicProperties(dataSet);
            var factory = new DynamicTypeFactory();
            var extendedType = typeof(T);
            var properties = extendedType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.CanRead && p.CanWrite);
            // Tạo object và gán value
            if (dataSet.Tables.Count > 0)
            {
                var table = dataSet.Tables[0];
                foreach (DataRow dr in table.Rows)
                {
                    T extendedObject = (T)Activator.CreateInstance(extendedType);
                    foreach (DataColumn cl in table.Columns)
                    {
                        if (dr[cl].GetType() != typeof(System.DBNull))
                        {
                            var prop = properties.Where(x => x.Name == cl.ColumnName).ToList();
                            if (prop.Count > 0)
                            {
                                var item = prop.First();
                                var value = dr[cl];
                                if(value != null)
                                {
                                    extendedType.GetProperty(cl.ColumnName).SetValue(extendedObject, Convert.ChangeType(value, item.PropertyType), null);
                                }
                            }
                        }
                    }
                    res.Add(extendedObject);
                }
            }

            return res;
        }

        #region Template

        private static List<string> _createFileClass(DataSet dataSet)
        {
            var dynamicProperties = GetDynamicProperties(dataSet);
            var factory = new DynamicTypeFactory();
            var extendedType = factory.CreateNewTypeWithDynamicProperties(typeof(ObjectReturnWithoutKey), dynamicProperties);
            var properties = extendedType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.CanRead && p.CanWrite);
            string className = GetClassTpl(extendedType.Name);
            var lst = new List<string>();
            lst.Add(className);
            lst.Add("{");
            foreach (PropertyInfo property in properties)
            {
                string typeStr = Helper.NormalizeType(property.PropertyType);
                string name = property.Name;
                string tlp = GetPropertyTpl(typeStr, name);
                tlp += "{ get; set; }";
                lst.Add(tlp);
            }
            lst.Add("}");

            return lst;
        }

        private static void _createFileClass(List<string> lines, string path)
        {
            System.IO.File.WriteAllLines(path, lines);
        }

        public static void CreateFileClass(DataSet dataSet, string path)
        {
            var lst = _createFileClass(dataSet);
            _createFileClass(lst, path);
        }

        private static string GetClassTpl(string className)
        {
            string tpl = "public class {0}";
            return string.Format(tpl, className);
        }

        private static string GetPropertyTpl(string typeProperty, string nameProperty)
        {
            string tpl = "public virtual {0} {1} ";
            return string.Format(tpl, typeProperty, nameProperty);
        }

        #endregion
    }
}
