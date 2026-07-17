using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

namespace TaskManager.Bussiness.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
    }
}
