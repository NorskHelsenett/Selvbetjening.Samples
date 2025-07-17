namespace Common.Models;

public class Resource(string name, string[] scopes)
{
    public string Name { get; } = name;
    public string[] Scopes { get; } = scopes;
}