namespace Retetar.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string? Picture { get; set; }
        public string? CookingInstructions { get; set; }

        public List<RecipeCategory>? RecipeCategories { get; set; }

        public List<RecipeIngredients>? RecipeIngredients { get; set; }
    }
}
