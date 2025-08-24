using Azure.Data.Tables;

namespace ABCRetailer.Services
{
    public interface IAzureStorageService
    {
        Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new();
    }
}
