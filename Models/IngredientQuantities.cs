using System.ComponentModel.DataAnnotations.Schema;

namespace Retetar.Models
{
    public class IngredientQuantities
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public string Unit { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public DateTime ExpiringDate { get; set; }
        public DateTime DateOfPurchase { get; set; }
        public bool Used { get; set; } = false;

        [ForeignKey("Ingredient")]
        public int IngredientId { get; set; }
        public virtual Ingredient? Ingredient { get; set; }
    }
}
