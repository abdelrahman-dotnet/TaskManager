namespace TaskManager.Data.Entities
{
    // G-7 / D-29: @Mention storage as a relational entity (user-approved Option A).
    // One row per (Comment, MentionedWorkspaceMember) — unique constraint enforced
    // in CommentMentionConfiguration. MentionedWorkspaceMember must be a valid
    // membership in the comment's task->project->workspace at mention time.
    public class CommentMention : BaseEntity
    {
        public long CommentId { get; set; }
        public Comment Comment { get; set; } = null!;

        public long MentionedWorkspaceMemberId { get; set; }
        public WorkspaceMember MentionedWorkspaceMember { get; set; } = null!;

        public DateTime MentionedAt { get; set; }
    }
}
