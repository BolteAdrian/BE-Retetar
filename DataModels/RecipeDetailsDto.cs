using Retetar.Models;

namespace Retetar.DataModels
{
    public class RecipeDetailsDto
    {
        public Recipe Recipe { get; set; }
        public List<RecipeIngredients>? Ingredients { get; set; }
        public List<RecipeCategory>? Categories { get; set; }
    }
}
