using System.ComponentModel.DataAnnotations.Schema;

namespace Retetar.Models
{
    public class RecipeIngredients
    {
        public int Id { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }

        [ForeignKey("Recipe")]
        public int? RecipeId { get; set; }
        public virtual Recipe? Recipe { get; set; }

        [ForeignKey("Ingredient")]
        public int? IngredientId { get; set; }
        public virtual Ingredient? Ingredient { get; set; }
    }
}
