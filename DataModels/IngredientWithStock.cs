using Retetar.Models;

namespace Retetar.DataModels
{
    public class IngredientWithStock : Ingredient
    {
        public bool StockEmpty { get; set; } = false;
        public bool StockExpired { get; set; } = false;
        public bool StockAlmostExpired { get; set; } = false;
    }
}
