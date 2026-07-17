using TaskManager.API.Enums;

namespace TaskManager.API.DTOs.Params
{
    public class SortOption<TSortEnum> where TSortEnum : Enum
    {
        public TSortEnum Field { get; set; }
        public SortDirection Direction { get; set; } = SortDirection.Ascending;   
    }
}
