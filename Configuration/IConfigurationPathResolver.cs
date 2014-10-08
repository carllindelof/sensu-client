namespace sensu_client.Configuration
{
    public interface IConfigurationPathResolver
    {
        string Configdir();
        string ConfigFileName();

    }
}