using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mal;
using Microsoft.Scripting.Utils;
using MalVal = Mal.types.MalVal;
using MalConstant = Mal.types.MalConstant;
using MalInt = Mal.types.MalInt;
using MalSymbol = Mal.types.MalSymbol;
using MalString = Mal.types.MalString;
using MalList = Mal.types.MalList;
using MalHashMap = Mal.types.MalHashMap;
using MalAtom = Mal.types.MalAtom;
using MalFunc = Mal.types.MalFunc;
using Env = Mal.env.Env;
using stepA_mal = Mal.stepA_mal;

namespace Shell
{
    public class Interpreter
    {
        static MalConstant Nil = Mal.types.Nil;
        static MalConstant True = Mal.types.True;
        static MalConstant False = Mal.types.False;
        private Env env;

        public Interpreter(Env env)
        {
            this.env = env;
            Initialize();
        }

        public static Interpreter Create()
        {
            return stepA_mal.Create();
        }

        public string LoadFile(string filename)
        {
            if (File.Exists(filename))
                return Eval("(load-file \"" + filename + "\")");

            return "";
        }

        public string Eval(string line)
        {
            if (line == null) { return ""; }
            if (line == "") { return ""; }

            try
            {
                return (stepA_mal.PRINT(stepA_mal.RE(line, env)));
            }
            catch (types.MalContinue)
            {
            }
            catch (types.MalException e)
            {
                throw new ApplicationException(printer._pr_str(e.getValue(), false));
            }

            return "";
        }

        // ---------- Custom Functions ----------

        private void Initialize()
        {
            SetEnv("who", "kerry");
            SetEnv("ping", PingFunction);
            SetEnv("exit", ExitFunction);
            SetEnv("clr-static-call", ClrStaticCall);
            SetEnv("clr-using", ClrUsing);
            SetEnv("clr-reference", ClrReference);
            SetEnv("clr-types", AllTypes);
            SetEnv("clr-assemblies", AllAssemblies);
        }

        public void SetEnv(MalSymbol symbol, MalVal value)
        {
            env.set(symbol, value);
        }

        public void SetEnv(string symbol, string value)
        {
            env.set(new MalSymbol(symbol), new MalString(value));
        }

        public void SetEnv(string symbol, MalVal value)
        {
            env.set(new MalSymbol(symbol), value);
        }

        public static MalFunc PingFunction = new MalFunc(a =>
        {
            return new MalString("Pong");
        });

        public static MalFunc ExitFunction = new MalFunc(a =>
        {
            throw new ExitException();
        });


        // ---------- Interop ----------

        // (clr-static-call "TryDLR.MAL.Test" "Now")
        // (clr-static-call "Test" "Now")
        public static MalFunc ClrStaticCall = new MalFunc(a =>
        {
            // Syntax error exception
            string typeName = ((MalString) a[0]).getValue();
            string methodName = ((MalString)a[1]).getValue();
            MalList malList = a.slice(2);
            object[] parameters = malList.getValue().Select(i => MapMalToDotNet(i)).ToArray();

            // Quickest
            Type t = Type.GetType(typeName);

            // Next Fastest
            if (t == null)
            {
                t = FindUsingTypes(typeName);
            }

            // Slowest
            if (t == null)
            {
                t = FindTypeByName(typeName);
            }

            if (t == null)
                throw new ApplicationException(string.Format("Type '{0}' not found.", typeName));

            MethodInfo method = t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
                throw new ApplicationException(string.Format("Type '{0}' static method '{1}' not found.", typeName, methodName));
            object result = method.Invoke(null, parameters);
            return MapDotNetToMal(result);
        });

        private static List<Type> UsingTypeList = new List<Type>();
        public static Type FindUsingTypes(string typeName)
        {
            return UsingTypeList.Find(t => t.Name.Equals(typeName));
        }

        public static MalFunc ClrReference = new MalFunc(a =>
        {
            string assemblyName = ((MalString)a[0]).getValue();
            Assembly.Load(assemblyName);
            return Nil;
        });

        // (clr-using "Namespace")
        // (clr-using "Namespace.Type")
        // (clr-using "Type")
        public static MalFunc ClrUsing = new MalFunc(a =>
        {
            string namespaceOrClassName = ((MalString)a[0]).getValue();
            List<Type> findTypesByNameSpaceFullNameOrClassName = FindTypesByNameSpaceFullNameOrClassName(namespaceOrClassName);
            if (findTypesByNameSpaceFullNameOrClassName.Count == 0)
                throw new ApplicationException(string.Format("No types found matching '{0}'.", namespaceOrClassName));
            UsingTypeList.AddRange(findTypesByNameSpaceFullNameOrClassName);
            return Nil;
        });

        private static Type FindTypeByName(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name.Equals(typeName))
                        return type;
                }
            }
            return null;
        }

        private static List<Type> FindTypesByNameSpaceFullNameOrClassName(string namespaceOrClassName)
        {
            List<Type> types = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if ((type != null && !string.IsNullOrEmpty(type.Namespace) && type.Namespace.Equals(namespaceOrClassName)) || type.FullName.Equals(namespaceOrClassName) || type.Name.Equals(namespaceOrClassName))
                        types.Add(type);
                }
            }
            return types;
        }

        private static MalFunc AllTypes = new MalFunc(a =>
        {
            List<string> types = new List<string>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                        types.Add(type.FullName);
                }
            }
            return MapDotNetToMal(types);
        });

        private static MalFunc AllAssemblies = new MalFunc(a =>
        {
            List<string> assemblies = new List<string>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                assemblies.Add(assembly.FullName);
            }
            return MapDotNetToMal(assemblies);
        });


        // ---------- Interop Mapping ----------

        private static MalVal MapDotNetToMal(object dotnetvalue)
        {
            if (dotnetvalue is string)
                return new MalString((string)dotnetvalue);

            if (dotnetvalue is int)
                return new MalInt(Convert.ToInt64(dotnetvalue));

            if (dotnetvalue is bool && (bool)dotnetvalue)
                return True;

            if (dotnetvalue is bool && !(bool)dotnetvalue)
                return False;

            if (dotnetvalue is DateTime)
                return new MalString(((DateTime)dotnetvalue).ToString());

            if (dotnetvalue == null)
                return Nil;

            if (dotnetvalue is IEnumerable)
                return MapEnumerableToMalList((IEnumerable) dotnetvalue);

            if (IsNonPrimitiveObjectType(dotnetvalue))
                return MapObjectToMalHashMap(dotnetvalue);

            return new MalString(dotnetvalue.ToString());
        }

        public static MalList MapEnumerableToMalList(IEnumerable enumerable)
        {
            List<MalVal> list = enumerable.Select(item => MapDotNetToMal(item)).ToList();
            return new MalList(list);
        }

        // http://stackoverflow.com/questions/737151/how-to-get-the-list-of-properties-of-a-class
        public static MalHashMap MapObjectToMalHashMap(object obj)
        {
            Dictionary<string, MalVal> dictionary = new Dictionary<string, MalVal>();
            PropertyInfo[] propertyInfos = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in propertyInfos)
            {
//                Console.WriteLine("{0}={1}", prop.Name, prop.GetValue(obj, null));
                dictionary.Add(prop.Name, MapDotNetToMal(prop.GetValue(obj, null)));
            }

            var fieldInfos = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fieldInfos)
            {
                dictionary.Add(field.Name, MapDotNetToMal(field.GetValue(obj)));
            }

            return new MalHashMap(dictionary);
        }

        private static bool IsNonPrimitiveObjectType(object obj)
        {
            Type t = obj.GetType();
            return !(t.IsPrimitive || t == typeof (Decimal) || t == typeof (String));
        }

        private static object MapMalToDotNet(MalVal malValue)
        {
            if (malValue is MalString)
                return ((MalString) malValue).getValue();

            if (malValue is MalAtom)
                return ((MalAtom)malValue).getValue();

            if (malValue is MalInt)
                return (int)(((MalInt) malValue).getValue());

            if (malValue is MalConstant && malValue == True)
                return true;

            if (malValue is MalConstant && malValue == False)
                return true;

            if (malValue is MalConstant && malValue == Nil)
                return true;

            if (malValue is MalList)
                return MapMalListToDotNetList((MalList) malValue);

            if (malValue is MalHashMap)
                return MapMalHashMapToDotNetDictionary((MalHashMap)malValue);

            return malValue.ToString();
        }

        private static List<object> MapMalListToDotNetList(MalList malList)
        {
            List<object> dotNetList = new List<object>();
            foreach (MalVal malVal in malList.getValue())
            {
                dotNetList.Add(MapMalToDotNet(malVal));
            }
            return dotNetList;
        }

        private static Dictionary<string, object> MapMalHashMapToDotNetDictionary(MalHashMap malHashMap)
        {
            Dictionary<string, object> dotNetDictionary = new Dictionary<string, object>();
            foreach (KeyValuePair<string, MalVal> keyValuePair in malHashMap.getValue())
            {
                dotNetDictionary.Add(keyValuePair.Key, MapMalToDotNet(keyValuePair.Value));
            }
            return dotNetDictionary;
        }
    }


    // ---------- Exit Exception ----------

    public class ExitException : ApplicationException
    {
    }
    
}