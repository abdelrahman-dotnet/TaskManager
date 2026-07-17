using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.Attachment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Exceptions;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AttachmentService> _logger;
        private readonly IAuditLogService _auditLogService;

        public AttachmentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AttachmentService> logger,IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;

        }

        public async Task<PagedResult<AttachmentReadDto>> GetAllAsync(AttachmentQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Attachments.GetAllQuery().AsNoTracking();

            query = query.ApplyFiltering(queryParams, AttachmentFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(a => a.FileName.Contains(search));
            }

            // NOTE: was missing the tie-breaker used everywhere else (x => x.Id), which made
            // Skip/Take non-deterministic whenever the client sends no explicit sort.
            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Attachments, x => x.Id);

            var projected = query.ProjectTo<AttachmentReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Attachments retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<AttachmentReadDto> CreateAsync(AttachmentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var taskExists = await _unitOfWork.Tasks.ExistsAsync(t => t.Id == dto.TaskItemId, cancellationToken);
            if (!taskExists)
            {
                _logger.LogWarning("CreateAttachment failed. Task not found. TaskId: {TaskId}", dto.TaskItemId);
                throw new NotFoundException("Task not found.");
            }

            var attachment = _mapper.Map<Attachment>(dto);
            attachment.UploadedByUserId = currentUserId;

            await _unitOfWork.Attachments.AddAsync(attachment, cancellationToken);
            var newValues = JsonSerializer.Serialize(new
            {
                attachment.FileName,
                attachment.FilePath,
                attachment.TaskItemId,
                attachment.UploadedByUserId
            });
            await _unitOfWork.CompleteAsync(cancellationToken);
            await _auditLogService.LogAsync(
                currentUserId,
                "Create Attachment",
                nameof(Attachment),
                attachment.Id.ToString(),
                null,
                newValues,
                cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Attachment uploaded successfully. AttachmentId: {AttachmentId}, TaskId: {TaskId}, UserId: {UserId}",
                attachment.Id, dto.TaskItemId, currentUserId);

            return _mapper.Map<AttachmentReadDto>(attachment);
        }

        public async Task DeleteAsync(long id, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var attachment = await _unitOfWork.Attachments.GetByIdAsync(id, cancellationToken);
            if (attachment == null)
            {
                _logger.LogWarning("DeleteAttachment failed. Not found. AttachmentId: {AttachmentId}", id);
                throw new NotFoundException("Attachment not found.");
            }

            if (!canManageAny && attachment.UploadedByUserId != currentUserId)
            {
                _logger.LogWarning("DeleteAttachment forbidden. UserId: {UserId} tried to delete AttachmentId: {AttachmentId}", currentUserId, id);
                throw new ForbiddenException("You can only delete your own attachments.");
            }

            // Deleting the physical file from disk/blob storage should happen in the controller/infrastructure
            // layer, since this Service only owns the DB record.
            var oldValues = JsonSerializer.Serialize(new
            {
                attachment.FileName,
                attachment.FilePath,
                attachment.TaskItemId,
                attachment.UploadedByUserId
            });

            _unitOfWork.Attachments.Delete(attachment);

            await _auditLogService.LogAsync(
                currentUserId,
                "Delete Attachment",
                nameof(Attachment),
                id.ToString(),
                oldValues,
                null,
                cancellationToken);

            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Attachment deleted successfully. AttachmentId: {AttachmentId}", id);
        }
    }
}