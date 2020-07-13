namespace AzureBusDepot.Abstractions
{
    public interface IEndpointConfig
    {
        string ConnectionString { get; set; }

        string EntityPath { get; set; }
    }
}