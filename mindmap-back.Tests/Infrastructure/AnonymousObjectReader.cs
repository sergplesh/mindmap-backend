using System.Reflection;

namespace mindmap_back.Tests.Infrastructure;

internal static class AnonymousObjectReader
{
    public static object? GetObject(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        Assert.NotNull(property);
        return property!.GetValue(source);
    }

    public static T Get<T>(object source, string propertyName)
    {
        var value = GetObject(source, propertyName);
        Assert.NotNull(value);
        return Assert.IsType<T>(value);
    }
}
