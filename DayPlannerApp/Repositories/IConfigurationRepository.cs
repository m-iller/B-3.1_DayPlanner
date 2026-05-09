using System.Threading.Tasks;

namespace DayPlannerApp.Repositories;

public interface IConfigurationRepository
{
    Task<T?> GetSettingAsync<T>(string key);
    Task SetSettingAsync<T>(string key, T value);
}
