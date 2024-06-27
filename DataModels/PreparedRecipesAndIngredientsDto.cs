using Retetar.Models;

namespace Retetar.DataModels
{
    public class PreparedRecipeDto
    {
        public string RecipeName { get; set; }
        public PreparedRecipeHistory? PreparedRecipe { get; set; }
    }

    public class UsedIngredientQuantityDto
    {
        public string IngredientName { get; set; }
        public IngredientQuantities Quantity { get; set; }
    }
}
