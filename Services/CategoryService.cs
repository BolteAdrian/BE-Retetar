using Microsoft.EntityFrameworkCore;
using Retetar.Interfaces;
using Retetar.Models;
using Retetar.Repository;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Services
{
    public class CategoryService
    {
        private readonly RecipeDbContext _dbContext;

        public CategoryService(RecipeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves a paginated list of Categorys based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of Categorys if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public List<Category> GetAllCategorysPaginated(IPaginationAndSearchOptions options)
        {
            try
            {
                IQueryable<Category> query = _dbContext.Category.AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(options.SearchTerm) && options.SearchFields != null)
                {
                    string searchTermLower = options.SearchTerm.ToLower();
                    query = query.Where(g =>
                        options.SearchFields.Any(f => g.Name.ToLower().Contains(searchTermLower))
                    );
                }

                // Sorting
                if (!string.IsNullOrEmpty(options.SortField))
                {
                    // In this example, we'll use the SortOrder enum to decide whether sorting is done in ascending or descending order
                    bool isAscending = options.SortOrder == SortOrder.Ascending;
                    query = SortQuery(query, options.SortField, isAscending);
                }

                // Calculate the total number of records
                int totalItems = query.Count();

                // Pagination - calculate the start index and retrieve the desired number of elements
                int startIndex = (options.PageNumber - 1) * options.PageSize;
                query = query.Skip(startIndex).Take(options.PageSize);

                // Return the results and pagination information
                return query.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Sorts the given query based on the provided sort field and sort order.
        /// </summary>
        /// <param name="query">The query to be sorted.</param>
        /// <param name="sortField">The field to be used for sorting.</param>
        /// <param name="isAscending">True for ascending sorting, false for descending sorting.</param>
        /// <returns>
        /// Returns the sorted query if successful.
        /// If the provided sort field is not recognized, returns the unchanged query.
        /// </returns>
        private IQueryable<Category> SortQuery(IQueryable<Category> query, string sortField, bool isAscending)
        {
            switch (sortField.ToLower())
            {
                case "name":
                    return isAscending ? query.OrderBy(g => g.Name) : query.OrderByDescending(g => g.Name);
                default:
                    return query; // If the sorting field does not exist or is not specified, return the unchanged query
            }
        }

        /// <summary>
        /// Retrieves a Category by its unique identifier from the database.
        /// </summary>
        /// <param name="IsRecipe">The identifier of the type of the Category.</param>
        /// <returns>
        /// Returns all the Categories of that type.
        /// If no Category is found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public async Task<List<Category>> GetCategoryByType(bool IsRecipe)
        {
            try
            {
                var categories = await _dbContext.Category
                    .Where(g => g.IsRecipe == IsRecipe)
                    .ToListAsync();

                if (categories == null)
                {
                    throw new Exception(string.Format(CATEGORY.NOT_FOUND));
                }

                return categories;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(CATEGORY.NOT_FOUND), ex);
            }
        }

        /// <summary>
        /// Retrieves a Category by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the Category.</param>
        /// <returns>
        /// Returns the Category object if found.
        /// If the Category with the specified ID is not found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public Category GetCategoryById(int id)
        {
            try
            {
                var category = _dbContext.Category.FirstOrDefault(g => g.Id == id);
                if (category == null)
                {
                    throw new Exception(string.Format(CATEGORY.NOT_FOUND, id));
                }
                return category;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(CATEGORY.NOT_FOUND, id), ex);
            }
        }

        /// <summary>
        /// Adds a new Category to the database.
        /// </summary>
        /// <param name="category">The Category object to be added.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Category object is null.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during saving the Category to the database.</exception>
        public void AddCategory(Category category)
        {
            try
            {
                _dbContext.Category.Add(category);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(CATEGORY.NOT_SAVED, ex);
            }
        }

        /// <summary>
        /// Updates an existing Category in the database.
        /// </summary>
        /// <param name="id">The ID of the Category to be updated.</param>
        /// <param name="category">The Category object containing the updated data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Category object is null.</exception>
        /// <exception cref="Exception">Thrown when the Category with the specified ID is not found or an error occurs during updating.</exception>
        public void UpdateCategory(int id, Category category)
        {
            try
            {
                var existingCategory = _dbContext.Category.FirstOrDefault(g => g.Id == id);

                if (existingCategory == null)
                {
                    throw new Exception(string.Format(CATEGORY.NOT_FOUND, id));
                }

                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.ShortDescription = category.ShortDescription;
                existingCategory.Picture = category.Picture;
                existingCategory.IsRecipe = category.IsRecipe;

                _dbContext.SaveChanges();

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(CATEGORY.ERROR_UPDATING, id), ex);
            }
        }

        /// <summary>
        /// Deletes a Category from the database based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Category to be deleted.</param>
        /// <exception cref="Exception">Thrown when the Category is not found or an error occurs during deletion.</exception>
        public void DeleteCategory(int id)
        {
            try
            {
                var category = _dbContext.Category.FirstOrDefault(g => g.Id == id);

                if (category == null)
                {
                    throw new Exception(string.Format(CATEGORY.NOT_FOUND, id));
                }

                _dbContext.Category.Remove(category);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(CATEGORY.ERROR_DELETING, id), ex);
            }
        }
    }
}