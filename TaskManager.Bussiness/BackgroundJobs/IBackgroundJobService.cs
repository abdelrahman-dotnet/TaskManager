using System.Linq.Expressions;

namespace TaskManager.Bussiness.BackgroundJobs;

public interface IBackgroundJobService
{
    string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall);

    string Schedule<TJob>(
        Expression<Func<TJob, Task>> methodCall,
        TimeSpan delay);

    void AddOrUpdateRecurring<TJob>(
        string recurringJobId,
        Expression<Func<TJob, Task>> methodCall,
        string cronExpression);
}