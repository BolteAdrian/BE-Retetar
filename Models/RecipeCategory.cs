using System.ComponentModel.DataAnnotations.Schema;

namespace Retetar.Models
{
    public class RecipeCategory
    {
        public int Id { get; set; }

        [ForeignKey("Recipe")]
        public int RecipeId { get; set; }
        public virtual Recipe? Recipe { get; set; }


        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }
    }
}
