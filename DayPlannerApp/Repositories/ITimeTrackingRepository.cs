using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Repositories;

public interface ITimeTrackingRepository
{
    Task<TimeTrackingSession> InsertSessionAsync(TimeTrackingSession session);
    Task<TimeTrackingSession> UpdateSessionAsync(TimeTrackingSession session);
    Task<IEnumerable<TimeTrackingSession>> GetSessionsByTaskIdAsync(Guid taskId);
}
