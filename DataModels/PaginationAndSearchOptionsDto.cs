namespace Retetar.DataModels
{
    public class PaginationAndSearchOptionsDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string[]? SearchFields { get; set; } = new string[] { "Name", "Category" };
        public SortOrder SortOrder { get; set; }

        public string? SortField { get; set; }
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }
}
