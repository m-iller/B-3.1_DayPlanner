using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class TagManagerViewModel : ViewModelBase
{
    private readonly ITagManager _tagManager;

    private ObservableCollection<Tag> _allTags = new();
    private ObservableCollection<string> _taskTags = new();
    private string _newTagName = string.Empty;
    private Tag? _selectedTag;
    private Guid _currentTaskId;

    public TagManagerViewModel(ITagManager tagManager)
    {
        _tagManager = tagManager;

        LoadAllTagsCommand = new RelayCommand(async () => await LoadAllTagsAsync());
        LoadTaskTagsCommand = new RelayCommand<Guid>(async taskId => await LoadTaskTagsAsync(taskId));
        CreateTagCommand = new RelayCommand(async () => await CreateTagAsync(), CanCreateTag);
        DeleteTagCommand = new RelayCommand(async () => await DeleteTagAsync(), CanDeleteTag);
        AssignTagToTaskCommand = new RelayCommand<string>(async tagName => await AssignTagToTaskAsync(tagName));
        RemoveTagFromTaskCommand = new RelayCommand<string>(async tagName => await RemoveTagFromTaskAsync(tagName));
    }

    public ObservableCollection<Tag> AllTags
    {
        get => _allTags;
        set => SetProperty(ref _allTags, value);
    }

    public ObservableCollection<string> TaskTags
    {
        get => _taskTags;
        set => SetProperty(ref _taskTags, value);
    }

    public string NewTagName
    {
        get => _newTagName;
        set => SetProperty(ref _newTagName, value);
    }

    public Tag? SelectedTag
    {
        get => _selectedTag;
        set => SetProperty(ref _selectedTag, value);
    }

    public Guid CurrentTaskId
    {
        get => _currentTaskId;
        set => SetProperty(ref _currentTaskId, value);
    }

    public ICommand LoadAllTagsCommand { get; }
    public ICommand LoadTaskTagsCommand { get; }
    public ICommand CreateTagCommand { get; }
    public ICommand DeleteTagCommand { get; }
    public ICommand AssignTagToTaskCommand { get; }
    public ICommand RemoveTagFromTaskCommand { get; }

    private async Task LoadAllTagsAsync()
    {
        var tags = await _tagManager.GetAllTagsAsync();
        AllTags = new ObservableCollection<Tag>(tags);
    }

    private async Task LoadTaskTagsAsync(Guid taskId)
    {
        CurrentTaskId = taskId;
        var tags = await _tagManager.GetTagsForTaskAsync(taskId);
        TaskTags = new ObservableCollection<string>(tags.Select(t => t.Name));
    }

    private bool CanCreateTag()
    {
        return !string.IsNullOrWhiteSpace(NewTagName);
    }

    private async Task CreateTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTagName))
            return;

        await _tagManager.CreateTagAsync(NewTagName);
        NewTagName = string.Empty;
        await LoadAllTagsAsync();
    }

    private bool CanDeleteTag()
    {
        return SelectedTag != null;
    }

    private async Task DeleteTagAsync()
    {
        if (SelectedTag == null)
            return;

        await _tagManager.DeleteTagAsync(SelectedTag.Name);
        SelectedTag = null;
        await LoadAllTagsAsync();
    }

    private async Task AssignTagToTaskAsync(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName) || CurrentTaskId == Guid.Empty)
            return;

        await _tagManager.AssignTagToTaskAsync(CurrentTaskId, tagName);
        await LoadTaskTagsAsync(CurrentTaskId);
    }

    private async Task RemoveTagFromTaskAsync(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName) || CurrentTaskId == Guid.Empty)
            return;

        await _tagManager.RemoveTagFromTaskAsync(CurrentTaskId, tagName);
        await LoadTaskTagsAsync(CurrentTaskId);
    }
}
