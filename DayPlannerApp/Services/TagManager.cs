using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

public class TagManager : ITagManager
{
    private readonly ITagRepository _tagRepository;
    private readonly ITaskRepository _taskRepository;

    public TagManager(ITagRepository tagRepository, ITaskRepository taskRepository)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    public async Task<Tag> CreateTagAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));
        }

        return await _tagRepository.CreateTagAsync(name);
    }

    public async Task DeleteTagAsync(string name)
    {
        await _tagRepository.DeleteTagAsync(name);
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _tagRepository.GetAllTagsAsync();
    }

    public async Task<IEnumerable<Tag>> GetTagsForTaskAsync(Guid taskId)
    {
        return await _tagRepository.GetTagsForTaskAsync(taskId);
    }

    public async Task AssignTagToTaskAsync(Guid taskId, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            throw new ArgumentException("Tag name cannot be empty.", nameof(tagName));
        }

        await _tagRepository.AssignTagToTaskAsync(taskId, tagName);
    }

    public async Task RemoveTagFromTaskAsync(Guid taskId, string tagName)
    {
        await _tagRepository.RemoveTagFromTaskAsync(taskId, tagName);
    }

    public async Task<IEnumerable<TaskEntity>> SearchTasksByTagsAsync(IEnumerable<string> tags)
    {
        var spec = new TaskQuerySpec
        {
            Tags = new List<string>(tags)
        };

        return await _taskRepository.QueryAsync(spec);
    }
}
