using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Service.PlayCheckCommon
{
    public class CustomSerializationBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName == "PlayCheck")
            {
                assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            }

            var resolvedType = Type.GetType($"{typeName}, {assemblyName}");
            if (resolvedType == null)
            {
                throw new JsonSerializationException($"Could not resolve type: {typeName}, {assemblyName}");
            }

            return resolvedType;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.Assembly.GetName().Name;
            typeName = serializedType.FullName;
        }
    }
}