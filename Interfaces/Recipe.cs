namespace Retetar.Interfaces
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CookingInstructions { get; set; }
        public int CategoryId { get; set; }
        public string UserId { get; set; }

        public Category Category { get; set; }
        public User User { get; set; }
    }
}
