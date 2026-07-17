using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Params
{
    public class CommonQueryParams
    {
        //Search
        public string? Search { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
    }
}