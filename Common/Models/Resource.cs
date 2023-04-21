namespace Common.Models;

public class Resource
{
    public Resource(string name, string[] scopes)
    {
        Name = name;
        Scopes = scopes;
    }

    public string Name { get; set; }
    public string[] Scopes { get; set; }
}