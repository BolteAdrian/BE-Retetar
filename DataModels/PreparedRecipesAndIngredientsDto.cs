using Retetar.Models;

namespace Retetar.DataModels
{
    public class PreparedRecipesAndIngredientsDto
    {
        public List<PreparedRecipeDto>? PreparedRecipes { get; set; }
        public List<IngredientQuantityDto>? Quantities { get; set; }

        public class PreparedRecipeDto
        {
            public string RecipeName { get; set; }
            public PreparedRecipeHistory? PreparedRecipe { get; set; }
        }

        public class IngredientQuantityDto
        {
            public string IngredientName { get; set; }
            public IngredientQuantities Quantity { get; set; }
        }
    }
}
