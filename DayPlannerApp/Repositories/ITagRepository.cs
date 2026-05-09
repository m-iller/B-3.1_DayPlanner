using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Repositories;

public interface ITagRepository
{
    Task<Tag> CreateTagAsync(string name);
    Task DeleteTagAsync(string name);
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task<IEnumerable<Tag>> GetTagsForTaskAsync(Guid taskId);
    Task AssignTagToTaskAsync(Guid taskId, string tagName);
    Task RemoveTagFromTaskAsync(Guid taskId, string tagName);
}
