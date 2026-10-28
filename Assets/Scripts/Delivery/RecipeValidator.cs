using System.Collections.Generic;
using UnityEngine;

public class RecipeValidator : MonoBehaviour
{
    [SerializeField] private List<Recipe> availableRecipes;

    public bool ValidateRecipe(GameObject bottomBun, List<GameObject> middleIngredients, GameObject topBun)
    {
        List<string> plateIngredients = new List<string>();

        if (bottomBun != null)
            plateIngredients.Add(CleanName(bottomBun.name));

        foreach (var ing in middleIngredients)
        {
            if (ing != null)
                plateIngredients.Add(CleanName(ing.name));
        }

        if (topBun != null)
            plateIngredients.Add(CleanName(topBun.name));

        foreach (var recipe in availableRecipes)
        {
            if (MatchesRecipeUnordered(plateIngredients, recipe.requiredIngredients))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesRecipeUnordered(List<string> plateList, List<string> requiredList)
    {
        if (plateList.Count != requiredList.Count) return false;

        List<string> sortedPlate = new List<string>();
        foreach (var item in plateList) sortedPlate.Add(item.Trim());

        List<string> sortedRequired = new List<string>();
        foreach (var item in requiredList)
        {
            sortedRequired.Add(item.ToString().Trim());
        }

        sortedPlate.Sort();
        sortedRequired.Sort();

        for (int i = 0; i < sortedPlate.Count; i++)
        {
            if (!sortedPlate[i].Contains(sortedRequired[i]))
            {
                return false;
            }
        }

        return true;
    }

    private string CleanName(string rawName)
    {
        return rawName.Replace("(Clone)", "").Trim();
    }
}