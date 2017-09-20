using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Nightfire_Source_Update_Builder
{
    class HookFunctions
    {
        public static void RunAll(string[] args)
        {
            Type type = typeof(Hooks);
            //FieldInfo[] fields = type.GetFields();
            MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);

            foreach (var item in methods)
            {
                MethodInfo m = Type.GetType(type.Name).GetMethod(item.Name);
                object result = m.Invoke(null, new[] { args });
                AddToFuncData(item.Name, result);
            }
        }

        public static void AddToFuncData(string funcName, object funcResult)
        {
            FuncStruct data = new FuncStruct();
            data.funcName = funcName;
            data.funcResult = funcResult;

            FuncListData.Add(data);
        }

        public static object GetReturnValueFromFunc(string funcName)
        {
            List<FuncStruct> rowList = FuncListData.Where(r => r.funcName.Equals(funcName)).ToList();
            RemoveFromFuncData(funcName);
            return rowList[0].funcResult;
        }

        //We can only have one result at a time anyway
        public static void RemoveFromFuncData(string funcName)
        {
            FuncListData.RemoveAll(x => x.funcName == funcName);
        }

        public class FuncStruct
        {
            public string funcName { set; get; }
            public object funcResult { set; get; }
        }

        public static List<FuncStruct> FuncListData = new List<FuncStruct>();
    }
}
