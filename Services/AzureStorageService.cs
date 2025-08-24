
namespace ABCRetailer.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        public AzureStorageService() { }

        async Task<List<T>> IAzureStorageService.GetAllEntitiesAsync<T>()
        {
            await Task.Delay(100);
            return new List<T>();
        }
    }
}
