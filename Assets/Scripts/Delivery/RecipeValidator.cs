using System.Collections.Generic;
using UnityEngine;

public class RecipeValidator : MonoBehaviour
{
    [Header("Scriptable Objects (Tus Recetas)")]
    public List<Recipe> availableRecipes;

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
            // Acá revisa que las variables coincidan con tu ScriptableObject
            if (MatchesRecipeUnordered(plateIngredients, recipe.requiredIngredients))
            {
                Debug.Log($"¡Pedido correcto! Receta completada");
                return true;
            }
        }

        Debug.Log("Pedido incorrecto: No coincide con ninguna receta.");
        return false;
    }

    private bool MatchesRecipeUnordered(List<string> plateList, List<string> requiredList)
    {
        if (plateList.Count != requiredList.Count) return false;

        List<string> sortedPlate = new List<string>();
        foreach (var item in plateList) sortedPlate.Add(item.Trim());

        // Asegurate de que el tipo de requiredList coincida con cómo armaste tu ScriptableObject
        List<string> sortedRequired = new List<string>();
        foreach (var item in requiredList)
        {
            // Si en tu ScriptableObject la lista es de GameObjects, usá item.name. Si son strings, dejalo así.
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