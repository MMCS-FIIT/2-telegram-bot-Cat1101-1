using System.Net.Http.Json;

namespace SimpleTGBot;

public class MealDBApi
{ 
    private static readonly HttpClient _client = new HttpClient();

    /// <summary>
    /// Получает рецепты по ингредиенту
    /// </summary>
    /// <param name="ingredient">Ингредиент на английском</param>
    /// <param name="offset">Сколько рецептов пропустить</param>
    public static async Task<Meal[]> GetRecipesByIngredient(string ingredient, int offset=0)
    {
        var url = $"https://www.themealdb.com/api/json/v1/1/filter.php?i={ingredient}";
        
        Meals meals = await _client.GetFromJsonAsync<Meals>(url) ?? new Meals();
        if (meals.meals is null) throw new HttpRequestException("Некорректный запрос");

        return meals.meals.Skip(offset).Take(5).ToArray();
    }
    
    /// <summary>
    /// Получает рецепты по слову
    /// </summary>
    /// <param name="word">Слово на английском</param>
    /// <param name="offset">Сколько рецептов пропустить</param>
    public static async Task<Meal[]> GetRecipesByFirstWord(string word, int offset=0)
    {
        var url = $"https://www.themealdb.com/api/json/v1/1/search.php?s={word}";
        
        Meals meals = await _client.GetFromJsonAsync<Meals>(url) ?? new Meals();
        if (meals.meals is null) throw new HttpRequestException("Некорректный запрос");

        return meals.meals.Skip(offset).Take(5).ToArray();
    }
    
    /// <summary>
    /// Получает рецепты по категории
    /// </summary>
    /// <param name="category">Категория meal_db</param>
    /// <param name="offset">Сколько рецептов пропустить</param>
    public static async Task<Meal[]> GetRecipesByCategory(string category, int offset=0)
    {
        var url = $"https://www.themealdb.com/api/json/v1/1/filter.php?c={category}";
        
        Meals meals = await _client.GetFromJsonAsync<Meals>(url) ?? new Meals();
        if (meals.meals is null) throw new HttpRequestException("Некорректный запрос");

        return meals.meals.Skip(offset).Take(5).ToArray();
    }
    
    /// <summary>
    /// Получает случайный рецепт
    /// </summary>
    public static async Task<Meal> GetRandomRecipe()
    {
        var url = $"https://www.themealdb.com/api/json/v1/1/random.php";
        
        Meals meals = await _client.GetFromJsonAsync<Meals>(url) ?? new Meals();
        if (meals.meals is null) throw new HttpRequestException("Некорректный запрос");
        
        return meals.meals[0];
    }
    
    /// <summary>
    /// Получает рецепт по id
    /// </summary>
    public static async Task<Meal> GetRecipeByID(int id)
    {
        var url = $"https://www.themealdb.com/api/json/v1/1/lookup.php?i={id}";
        
        Meals meals = await _client.GetFromJsonAsync<Meals>(url) ?? new Meals();
        if (meals.meals is null) throw new HttpRequestException("Некорректный запрос");
        
        return meals.meals[0];
    }
    
    /// <summary>
    /// Получает список категорий meal_db
    /// </summary>
    public static async Task<Meal[]> GetCategories()
    {
        var url = $"https://www.themealdb.com/api/json/v1/1/list.php?c=list";
        
        Meals meals = await _client.GetFromJsonAsync<Meals>(url) ?? new Meals();
        if (meals.meals is null) throw new HttpRequestException("Некорректный запрос");
        
        return meals.meals;
    }
}