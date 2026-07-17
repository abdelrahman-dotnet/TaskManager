using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

namespace TaskManager.Bussiness.Repositories
{
    public class AttachmentRepository : Repository<Attachment>, IAttachmentRepository
    {
        public AttachmentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
