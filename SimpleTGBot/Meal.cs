namespace SimpleTGBot;

public class Meals 
{
    public Meal[] meals { get; set; }
}

public class Meal
{
    public int idMeal { get; set; }
    public string strMeal { get; set; }
    public string strMealAlternate { get; set; }
    public string strCategory { get; set; }
    public string strArea { get; set; }
    public string strInstructions { get; set; }
    public string strMealThumb { get; set; }
    public string strTags { get; set; }
    public string strYoutube { get; set; }
    public string strIngredient1 { get; set; }
    public string strIngredient2 { get; set; }
    public string strIngredient3 { get; set; }
    public string strIngredient4 { get; set; }
    public string strIngredient5 { get; set; }
    public string strIngredient6 { get; set; }
    public string strIngredient7 { get; set; }
    public string strIngredient8 { get; set; }
    public string strIngredient9 { get; set; }
    public string strIngredient10 { get; set; }
    public string strMeasure1 { get; set; }
    public string strMeasure2 { get; set; }
    public string strMeasure3 { get; set; }
    public string strMeasure4 { get; set; }
    public string strMeasure5 { get; set; }
    public string strMeasure6 { get; set; }
    public string strMeasure7 { get; set; }
    public string strMeasure8 { get; set; }
    public string strMeasure9 { get; set; }
    public string strMeasure10 { get; set; }

    public IEnumerable<string> GetIngredients()
    {
        yield return strIngredient1;
        yield return strIngredient2;
        yield return strIngredient3;
        yield return strIngredient4;
        yield return strIngredient5;
        yield return strIngredient6;
        yield return strIngredient7;
        yield return strIngredient8;
        yield return strIngredient9;
        yield return strIngredient10;
    }
    
    public IEnumerable<string> GetMeasures()
    {
        yield return strMeasure1;
        yield return strMeasure2;
        yield return strMeasure3;
        yield return strMeasure4;
        yield return strMeasure5;
        yield return strMeasure6;
        yield return strMeasure7;
        yield return strMeasure8;
        yield return strMeasure9;
        yield return strMeasure10;
    }
}