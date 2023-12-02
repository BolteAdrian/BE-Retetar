using Retetar.Models;

namespace Retetar.Interfaces
{
    public class RecipeDetails
    {
        public Recipe Recipe { get; set; }
        public List<RecipeIngredients>? Ingredients { get; set; }
        public List<RecipeCategory>? Categories { get; set; }
    }
}
