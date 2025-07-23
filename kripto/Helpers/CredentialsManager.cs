namespace kripto.Helpers;

public class CredentialsManager
{
    private static CredentialsManager? instance;

    public string? Token { get; set; }
    public string? CurrentUser { get; set; }

    private CredentialsManager() { }

    public static CredentialsManager GetInstance()
    {
        if (instance == null)
        {
            instance = new CredentialsManager();
        }
        return instance;
    }
}
