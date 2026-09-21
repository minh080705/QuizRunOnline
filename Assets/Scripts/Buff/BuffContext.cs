using System.Collections.Generic;

public class BuffContext
{
    private Dictionary<string, object> data = new();

    public void Set(string key, object value) => data[key] = value;

    public T Get<T>(string key, T defaultValue = default)
    {
        if (data.TryGetValue(key, out var value)) return (T)value;
        return defaultValue;
    }
}