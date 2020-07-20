using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace InExactObjectMapping
{
    public static class InExactObjectMapper
    {
        private static readonly ConcurrentDictionary<string, Delegate> CompiledSourceTypeDelegate =
            new ConcurrentDictionary<string, Delegate>();

        private static readonly ConcurrentDictionary<string, Delegate> CompiledTargetTypeDelegate =
            new ConcurrentDictionary<string, Delegate>();

        private static readonly ConcurrentDictionary<string, object> DifficultTypes =
            new ConcurrentDictionary<string, object>();

        private static readonly char ClassSeparator = (char)36;

        private static void SetGetterDelegate(Type sourceType)
        {
            ParameterExpression objInstance = Expression.Parameter(sourceType);
            foreach (PropertyInfo property in sourceType.GetProperties().Where(k => k.CanRead))
            {
                MethodInfo propertyGetMethodInfo = property.GetMethod;
                MethodCallExpression invokerExpression = Expression.Call(objInstance, propertyGetMethodInfo);

                UnaryExpression primitivesConverter = Expression.Convert(invokerExpression, property.PropertyType);
                Delegate cDelegate = Expression.Lambda(primitivesConverter, objInstance).Compile();

                string cacheKey = string.Concat(sourceType.Name, ClassSeparator, property.Name);
                if (!CompiledSourceTypeDelegate.ContainsKey(cacheKey))
                    CompiledSourceTypeDelegate.TryAdd(cacheKey, cDelegate);
            }
        }

        private static void SetSetterDelegate(Type targetType)
        {
            ParameterExpression objInstance = Expression.Parameter(targetType);
            foreach (PropertyInfo property in targetType.GetProperties().Where(k => k.CanWrite))
            {
                MethodInfo propertySetMethodInfo = property.SetMethod;
                ParameterExpression valueInstance = Expression.Parameter(property.PropertyType);
                MethodCallExpression invokerExpression = Expression.Call(objInstance, propertySetMethodInfo,
                    valueInstance);

                Delegate cDelegate = Expression.Lambda(invokerExpression, objInstance, valueInstance).Compile();

                string cacheKey = string.Concat(targetType.Name, ClassSeparator, property.Name);
                if (!CompiledTargetTypeDelegate.ContainsKey(cacheKey))
                    CompiledTargetTypeDelegate.TryAdd(cacheKey, cDelegate);
            }
        }

        private static object GetDefaultInstanceIfNull(object instance, string propertyName)
        {
            try
            {
                var pType = instance.GetType().GetProperty(propertyName).PropertyType;
                object ret = pType.Name == typeof(string).Name
                    ? string.Empty
                    : (pType.IsArray ? Array.CreateInstance(pType.GetElementType(), 0) : Activator.CreateInstance(pType));
                return ret;
            }
            catch
            {
                return null;
            }
        }

        public static TSister CopyValuesFromMatchingPropertiesToSisterType<TSister>(this object instance, TSister sisterInstance = null, bool disableNull = false)
            where TSister : class
        {
            if (instance == null) return null;

            SetGetterDelegate(instance.GetType());
            TSister ret = sisterInstance ?? Activator.CreateInstance<TSister>();
            SetSetterDelegate(ret.GetType());

            string sourceTypeName = instance.GetType().Name;
            string targetTypeName = ret.GetType().Name;

            foreach (KeyValuePair<string, Delegate> readerDelegate in CompiledSourceTypeDelegate.Where(
                k => k.Key.StartsWith(string.Concat(sourceTypeName, ClassSeparator))))
            {
                string targetTypecacheKey = readerDelegate.Key.Replace(sourceTypeName, targetTypeName);
                Delegate writerDelegate;

                if (!CompiledTargetTypeDelegate.TryGetValue(targetTypecacheKey, out writerDelegate)) continue;
                string propertyName = targetTypecacheKey.Remove(0, targetTypeName.Length + 1);
                object value = readerDelegate.Value.DynamicInvoke(instance);

                try
                {
                    value = value ?? (disableNull ? GetDefaultInstanceIfNull(ret, propertyName) : null);
                    writerDelegate.DynamicInvoke(ret, value);
                }
                catch (System.Exception)
                {
                    HandleComplexTypes(propertyName, ret, value);
                }
            }

            return ret;
        }

        private static void HandleComplexTypes<TSister>(string propertyName, TSister ret, object value)
            where TSister : class
        {
            Type innerTargetType;

            try
            {
                innerTargetType = ret.GetType().GetProperty(propertyName).PropertyType;
            }
            catch (System.Exception)
            {
                return;
            }

            bool cannotCreate = innerTargetType.GetConstructor(Type.EmptyTypes) == null;
            if (cannotCreate)
            {
                if (innerTargetType.IsArray)
                {
                    Type elementTypeOfInnerTargetType = innerTargetType.GetElementType();
                    var sourceValuesArray = (Array)value;
                    Array targetValues = Array.CreateInstance(elementTypeOfInnerTargetType,
                        sourceValuesArray?.Length ?? 0);

                    if (sourceValuesArray != null)
                    {
                        var counter = 0;
                        foreach (object sourceValue in sourceValuesArray)
                        {
                            targetValues.SetValue(
                                sourceValue.CopyValuesFromMatchingPropertiesToSisterType(Activator.CreateInstance(elementTypeOfInnerTargetType)),
                                counter);
                            counter++;
                        }
                    }

                    ret.GetType().GetProperty(propertyName).SetValue(ret, targetValues);
                    return;
                }
                DifficultTypes.TryAdd(propertyName, value);
                return;
            }

            ret.GetType().GetProperty(propertyName).SetValue(ret, Activator.CreateInstance(innerTargetType));
            value.CopyValuesFromMatchingPropertiesToSisterType(ret.GetType().GetProperty(propertyName).GetValue(ret));
        }
    }
}
