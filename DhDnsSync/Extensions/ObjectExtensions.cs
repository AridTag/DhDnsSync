using System.Reflection;

namespace DhDnsSync.Extensions;

public static class ObjectExtensions
{
    public static T ToObject<T>(this IDictionary<string, object> source)
        where T : class, new()
    {
        var someObject = new T();
        var someObjectType = someObject.GetType();

        foreach (var (key, value) in source)
        {
            if (someObjectType.GetProperty(key) is not { } propertyInfo)
            {
                continue;
            }
            
            propertyInfo.SetValue(someObject, value, null);
        }

        return someObject;
    }

    public static IDictionary<string, object?> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
    {
        return source.GetType()
            .GetProperties(bindingAttr)
            .ToDictionary(
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)
            );
    }
}