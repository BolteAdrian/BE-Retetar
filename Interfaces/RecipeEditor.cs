using Retetar.Models;

namespace Retetar.Interfaces
{
    public class RecipeEditor
    {
        public Recipe Recipe { get; set; }
        public List<RecipeIngredients>? Ingredients { get; set; }
        public List<int>? Categories { get; set; }
    }
}
