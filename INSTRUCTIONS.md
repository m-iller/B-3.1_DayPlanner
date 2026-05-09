# Day Planner App - User Instructions

## Getting Started

### First Launch

1. Run the application
2. Database automatically created at `%LOCALAPPDATA%/DayPlannerApp/dayplanner.db`
3. Default task types loaded (personal projects, learning, work, must do)

### Creating Your First Task

1. Click **New Task** button
2. Enter description (markdown supported)
3. Select task type
4. (Optional) Set deadline date/time
5. (Optional) Add tags
6. (Optional) Set urgency level
7. Click **Save**

## Task Management

### Editing Tasks

- Click task in list to open editor
- Modify any field
- Click **Save** to persist changes

### Deleting Tasks

- Open task editor
- Click **Delete** button
- Confirm deletion

### Markdown Formatting

Task descriptions support markdown:

```markdown
# Header
**bold** *italic*
- List item
[link](url)
```

## Organization

### Task Types

Four configurable categories:
- Personal Projects
- Learning
- Work
- Must Do

**Customize names**: Settings → Task Types

### Tags

Create custom tags for flexible categorization:

1. Click **Manage Tags**
2. Enter new tag name
3. Click **Create**

**Assign to task**: In task editor, select tags from list

**Search by tags**: Use tag filter in task list

### Coordinate Controller

Position tasks on 2D grid:
- **X-axis**: Importance (0-100)
- **Y-axis**: Complexity (0-100)

**Usage**:
1. Open Coordinate Controller view
2. Drag tasks to desired position
3. Coordinates saved automatically

## Calendar Views

### Day View

- Shows tasks for selected date
- Navigate: Previous/Next day buttons
- Click date picker to jump to specific date

### Week View

- Shows 7 consecutive days
- Navigate: Previous/Next week buttons
- Click day to see task details

## Time Tracking

### Starting a Session

1. Select task
2. Click **Start Tracking**
3. Timer begins

### Taking Breaks

1. Click **Start Break**
2. Break time excluded from total
3. Click **End Break** to resume

### Stopping a Session

1. Click **Stop Tracking**
2. Session saved to database
3. Total duration calculated (work time - breaks)

### Viewing History

- Task list shows accumulated time per task
- Includes all historical sessions

## Filtering and Sorting

### Filter Options

- By task type
- By tags (AND/OR logic)
- By date range
- By deadline proximity

### Sort Options

- By urgency (high to low)
- By deadline (nearest first)
- By creation date
- By last modified

## Keyboard Shortcuts

- `Ctrl+N`: New task
- `Ctrl+S`: Save task
- `Ctrl+F`: Focus search
- `Ctrl+T`: Start/stop time tracking
- `Ctrl+B`: Start/end break
- `Delete`: Delete selected task (with confirmation)

## Data Management

### Backup

Database file location: `%LOCALAPPDATA%/DayPlannerApp/dayplanner.db`

**Manual backup**: Copy database file to safe location

**Restore**: Replace database file with backup

### Export

(Feature available via modules or IPC API)

## Modules

### Installing Modules

1. Download module DLL
2. Place in `Modules/` directory (next to executable)
3. Restart application
4. Module auto-discovered and loaded

### Managing Modules

Settings → Modules:
- View installed modules
- Enable/disable modules
- View module information

## IPC Integration

External applications can control Day Planner via IPC API.

**Authentication**: Token stored in database, viewable in Settings → IPC

**Example use cases**:
- Command-line task creation
- Calendar sync scripts
- Automation workflows

See [IPC.md](IPC.md) for technical details.

## Troubleshooting

### Application Won't Start

- Check .NET 8 runtime installed
- Check database file permissions
- Check logs in `%LOCALAPPDATA%/DayPlannerApp/logs/`

### Database Corruption

1. Application detects corruption on startup
2. Error message displayed
3. Options:
   - Restore from backup
   - Start with empty database

### Module Load Failures

- Check module compatible with current version
- Check module dependencies installed
- View error details in Settings → Modules

### Time Tracking Issues

- Only one task can be tracked at a time
- Stopping tracking saves session immediately
- Break periods must be ended before stopping tracking

## Tips

- Use tags liberally for flexible organization
- Set deadlines to see proximity indicators
- Use coordinate controller for visual prioritization
- Review week view for workload balancing
- Track time to measure actual effort vs estimates

## Support

[Your support contact/link here]
