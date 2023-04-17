namespace Common.Crypto;

public class KeyPair
{
    public KeyPair(string privatePem, string publicPem)
    {
        PrivatePem = privatePem;
        PublicPem = publicPem;
    }

    public string PrivatePem { get; set; }
    public string PublicPem { get; set; }
}