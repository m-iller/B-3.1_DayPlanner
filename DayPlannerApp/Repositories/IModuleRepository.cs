using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Repositories;

/// <summary>
/// Repository for persisting module metadata and state
/// </summary>
public interface IModuleRepository
{
    Task<ModuleInfo> InsertAsync(ModuleInfo module);
    Task<ModuleInfo> UpdateAsync(ModuleInfo module);
    Task DeleteAsync(string moduleId);
    Task<ModuleInfo?> GetByIdAsync(string moduleId);
    Task<IEnumerable<ModuleInfo>> GetAllAsync();
    Task<IEnumerable<ModuleInfo>> GetEnabledAsync();
}
