# Day Planner IPC API

Inter-process communication API for external application integration.

## Overview

Day Planner exposes a named pipe server for local IPC. External applications can send JSON requests to control tasks, time tracking, and query data.

**Transport**: Named Pipes (Windows)  
**Protocol**: JSON request/response  
**Authentication**: Shared secret token  

## Connection

### Pipe Name

```
DayPlannerApp.IPC
```

### C# Example

```csharp
using System.IO.Pipes;
using System.Text.Json;

var client = new NamedPipeClientStream(".", "DayPlannerApp.IPC", PipeDirection.InOut);
await client.ConnectAsync();

var request = new IPCRequest
{
    RequestId = Guid.NewGuid().ToString(),
    Method = "GET",
    Resource = "/tasks",
    AuthToken = "your-token-here"
};

var json = JsonSerializer.Serialize(request);
var bytes = Encoding.UTF8.GetBytes(json);
await client.WriteAsync(bytes, 0, bytes.Length);

// Read response
var buffer = new byte[4096];
var bytesRead = await client.ReadAsync(buffer, 0, buffer.Length);
var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
var response = JsonSerializer.Deserialize<IPCResponse>(responseJson);
```

### Python Example

```python
import json
import win32pipe
import win32file

pipe = win32file.CreateFile(
    r'\\.\pipe\DayPlannerApp.IPC',
    win32file.GENERIC_READ | win32file.GENERIC_WRITE,
    0, None,
    win32file.OPEN_EXISTING,
    0, None
)

request = {
    "requestId": "12345",
    "method": "GET",
    "resource": "/tasks",
    "authToken": "your-token-here"
}

win32file.WriteFile(pipe, json.dumps(request).encode())
result, data = win32file.ReadFile(pipe, 4096)
response = json.loads(data.decode())
```

## Authentication

### Getting Token

1. Open Day Planner
2. Go to Settings → IPC
3. Copy authentication token

Token stored in database table `IPCAuthTokens`.

### Using Token

Include in every request:

```json
{
  "authToken": "your-token-here"
}
```

**Unauthorized requests return**:

```json
{
  "statusCode": 401,
  "error": "Invalid or missing authentication token"
}
```

## Request Format

```json
{
  "requestId": "unique-id",
  "method": "GET|POST|PUT|DELETE",
  "resource": "/tasks|/tags|/time-tracking",
  "body": {},
  "authToken": "token"
}
```

### Fields

- **requestId**: Unique identifier for correlation (echoed in response)
- **method**: HTTP-style verb
- **resource**: API endpoint
- **body**: Request payload (for POST/PUT)
- **authToken**: Authentication token

## Response Format

```json
{
  "requestId": "unique-id",
  "statusCode": 200,
  "body": {},
  "error": null
}
```

### Status Codes

- **200**: Success
- **400**: Bad request (invalid format)
- **401**: Unauthorized (invalid token)
- **404**: Resource not found
- **500**: Internal server error

## API Endpoints

### Tasks

#### GET /tasks

Get all tasks.

**Request**:
```json
{
  "requestId": "1",
  "method": "GET",
  "resource": "/tasks",
  "authToken": "token"
}
```

**Response**:
```json
{
  "requestId": "1",
  "statusCode": 200,
  "body": {
    "tasks": [
      {
        "id": "guid",
        "description": "Task description",
        "taskTypeId": 1,
        "deadlineDate": "2026-05-15",
        "urgencyLevel": 5,
        "tags": ["important", "work"]
      }
    ]
  }
}
```

#### GET /tasks/{id}

Get specific task.

**Request**:
```json
{
  "requestId": "2",
  "method": "GET",
  "resource": "/tasks/12345-guid",
  "authToken": "token"
}
```

#### POST /tasks

Create new task.

**Request**:
```json
{
  "requestId": "3",
  "method": "POST",
  "resource": "/tasks",
  "body": {
    "description": "New task",
    "taskTypeId": 1,
    "urgencyLevel": 5,
    "tags": ["work"]
  },
  "authToken": "token"
}
```

**Response**:
```json
{
  "requestId": "3",
  "statusCode": 200,
  "body": {
    "task": {
      "id": "new-guid",
      "description": "New task",
      "taskTypeId": 1,
      "urgencyLevel": 5,
      "createdAt": "2026-05-09T10:30:00Z"
    }
  }
}
```

#### PUT /tasks/{id}

Update existing task.

**Request**:
```json
{
  "requestId": "4",
  "method": "PUT",
  "resource": "/tasks/12345-guid",
  "body": {
    "description": "Updated description",
    "urgencyLevel": 8
  },
  "authToken": "token"
}
```

#### DELETE /tasks/{id}

Delete task.

**Request**:
```json
{
  "requestId": "5",
  "method": "DELETE",
  "resource": "/tasks/12345-guid",
  "authToken": "token"
}
```

### Tags

#### GET /tags

Get all tags.

**Request**:
```json
{
  "requestId": "6",
  "method": "GET",
  "resource": "/tags",
  "authToken": "token"
}
```

**Response**:
```json
{
  "requestId": "6",
  "statusCode": 200,
  "body": {
    "tags": [
      {"name": "work", "createdAt": "2026-01-01T00:00:00Z"},
      {"name": "personal", "createdAt": "2026-01-02T00:00:00Z"}
    ]
  }
}
```

#### POST /tags

Create new tag.

**Request**:
```json
{
  "requestId": "7",
  "method": "POST",
  "resource": "/tags",
  "body": {
    "name": "urgent"
  },
  "authToken": "token"
}
```

### Time Tracking

#### POST /time-tracking/start

Start tracking time for task.

**Request**:
```json
{
  "requestId": "8",
  "method": "POST",
  "resource": "/time-tracking/start",
  "body": {
    "taskId": "task-guid"
  },
  "authToken": "token"
}
```

#### POST /time-tracking/stop

Stop tracking time.

**Request**:
```json
{
  "requestId": "9",
  "method": "POST",
  "resource": "/time-tracking/stop",
  "body": {
    "taskId": "task-guid"
  },
  "authToken": "token"
}
```

#### POST /time-tracking/break/start

Start break.

**Request**:
```json
{
  "requestId": "10",
  "method": "POST",
  "resource": "/time-tracking/break/start",
  "body": {
    "taskId": "task-guid"
  },
  "authToken": "token"
}
```

#### POST /time-tracking/break/end

End break.

**Request**:
```json
{
  "requestId": "11",
  "method": "POST",
  "resource": "/time-tracking/break/end",
  "body": {
    "taskId": "task-guid"
  },
  "authToken": "token"
}
```

#### GET /time-tracking/sessions/{taskId}

Get time tracking sessions for task.

**Request**:
```json
{
  "requestId": "12",
  "method": "GET",
  "resource": "/time-tracking/sessions/task-guid",
  "authToken": "token"
}
```

**Response**:
```json
{
  "requestId": "12",
  "statusCode": 200,
  "body": {
    "sessions": [
      {
        "id": "session-guid",
        "taskId": "task-guid",
        "startTime": "2026-05-09T09:00:00Z",
        "endTime": "2026-05-09T11:00:00Z",
        "totalDuration": "01:45:00",
        "totalBreakTime": "00:15:00"
      }
    ]
  }
}
```

## Query Parameters

### Filter Tasks

**GET /tasks?filter=...**

```json
{
  "resource": "/tasks?filter=tags:work,urgent&dateFrom=2026-05-01&dateTo=2026-05-31"
}
```

Supported filters:
- `tags:tag1,tag2` - Tasks with all specified tags
- `type:1` - Tasks of specific type
- `dateFrom:YYYY-MM-DD` - Tasks from date
- `dateTo:YYYY-MM-DD` - Tasks until date
- `urgency:5` - Tasks with urgency >= value

## Error Handling

### Invalid Request Format

```json
{
  "requestId": "13",
  "statusCode": 400,
  "error": "Invalid JSON format"
}
```

### Resource Not Found

```json
{
  "requestId": "14",
  "statusCode": 404,
  "error": "Task not found: 12345-guid"
}
```

### Internal Error

```json
{
  "requestId": "15",
  "statusCode": 500,
  "error": "Database connection failed"
}
```

## Rate Limiting

(Not currently implemented)

Future: 100 requests per minute per token.

## Use Cases

### Command-Line Task Creation

```bash
# PowerShell script
$request = @{
    requestId = [guid]::NewGuid().ToString()
    method = "POST"
    resource = "/tasks"
    body = @{
        description = "Task from CLI"
        taskTypeId = 1
        urgencyLevel = 5
    }
    authToken = "your-token"
} | ConvertTo-Json

# Send to named pipe (requires additional pipe handling code)
```

### Automation Script

```python
# Create task from email
def create_task_from_email(subject, body):
    request = {
        "requestId": str(uuid.uuid4()),
        "method": "POST",
        "resource": "/tasks",
        "body": {
            "description": f"# {subject}\n\n{body}",
            "taskTypeId": 3,  # Work
            "urgencyLevel": 5
        },
        "authToken": get_token()
    }
    return send_ipc_request(request)
```

### Integration with Other Tools

- Import tasks from project management tools
- Export tasks to calendar applications
- Sync with time tracking services
- Automate task creation from various sources

## Security Considerations

- Token stored locally (not transmitted over network)
- Only local connections accepted
- No remote access
- Token rotation recommended periodically
- Audit logging for all IPC operations

## Troubleshooting

### Cannot Connect to Pipe

- Ensure Day Planner is running
- Check pipe name is correct: `DayPlannerApp.IPC`
- Verify no firewall blocking local pipes

### Authentication Failures

- Verify token is correct (check Settings → IPC)
- Token may have been rotated
- Check token not expired (if expiration enabled)

### Timeout Issues

- Large queries may take time
- Increase client timeout
- Consider pagination for large result sets

## Future Enhancements

(Not yet implemented)

- WebSocket support for real-time updates
- Batch operations
- Pagination for large result sets
- Webhook notifications
- Rate limiting
- Token expiration and rotation
- Request validation schemas

## Support

For IPC integration questions, see main documentation or contact support.
