using System.ComponentModel.DataAnnotations.Schema;

namespace Retetar.Models
{
    public class PreparedRecipeHistory
    {
        public int Id { get; set; }
        public int Amount { get; set; }

        [ForeignKey("Recipe")]
        public int RecipeId { get; set; }
        public virtual Recipe Recipe { get; set; }

    }
}
