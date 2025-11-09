using System.Runtime;
using System.Security.Cryptography.X509Certificates;

string[] recipeData = File.ReadAllLines("recipeData.csv");
Console.WriteLine($"Loaded {recipeData.Length} lines from recipeData.csv");

var ingredients = new List<string>();
var recipes = new Dictionary<(string Ingredient1, string Ingredient2), (int Quantity, string Result)>();
var creationRecipes = new Dictionary<string, (string Ingredient1, string Ingredient2)>();

foreach (var line in recipeData)
{
    Console.WriteLine(line);
    if (line.StartsWith("//"))
    {
        continue;
    }

    var pieces = line.Split(',');
    if (ingredients.Count == 0)
    {
        Console.WriteLine("Ingredients headers:");
        foreach (var piece in pieces)
        {
            if (piece == "X")
            {
                Console.WriteLine("Skipping first header");
                continue;
            }
            Console.WriteLine(piece);

            if (ingredients.Contains(piece))
            {
                Console.WriteLine($"Ingredient {piece} already exists in the set!");
                return;
            }
            ingredients.Add(piece);
        }
        continue;
    }

    if (pieces.Length != (ingredients.Count + 1))
    {
        Console.WriteLine($"Row length {pieces.Length} does not match ingredients count {ingredients.Count}!");
        return;
    }

    var ingredient1 = pieces[0];
    Console.WriteLine($"Processing ingredient: {ingredient1}");
    if (!ingredients.Contains(ingredient1))
    {
        Console.WriteLine($"Ingredient {ingredient1} not found in the set!");
        return;
    }

    for (int i = 1; i < pieces.Length; i++)
    {
        var ingredient2 = ingredients[i - 1];
        if (recipes.ContainsKey((ingredient1, ingredient2)))
        {
            Console.WriteLine($"Recipe for ({ingredient1}, {ingredient2}) already exists!");
            return;
        }

        var resultText = pieces[i];

        var resultPieces = resultText.Split(' ', 2);

        int quantity = 1;
        var result = resultText;
        if (resultPieces.Length > 1)
        {
            if (int.TryParse(resultPieces[0], out var parsedQuantity) && parsedQuantity > 0)
            {
                quantity = parsedQuantity;
                result = resultPieces[1];
            }
        }

        Console.WriteLine($"Adding recipe: ({ingredient1}, {ingredient2}) => [{quantity}] x {result}");
        recipes[(ingredient1, ingredient2)] = (quantity, result);

        if (ingredients.Contains(result))
        {
            if (creationRecipes.TryGetValue(result, out var existingIngredients))
            {
                if (existingIngredients.Ingredient1 != ingredient2 || existingIngredients.Ingredient2 != ingredient1)
                {
                    Console.WriteLine($"Conflict: Ingredient {result} can be created by both ({existingIngredients.Ingredient1}, {existingIngredients.Ingredient2}) and ({ingredient1}, {ingredient2})");
                    return;
                }
            }

            Console.WriteLine($"Adding creation recipe for {result}: ({ingredient1}, {ingredient2})");
            creationRecipes[result] = (ingredient1, ingredient2);
        }
    }
}

string[] inventoryData = File.ReadAllLines("inventoryData.csv");
Console.WriteLine($"Loaded {inventoryData.Length} lines from inventoryData.csv");

var inventory = new Dictionary<string, int>();
var first = true;

foreach (var line in inventoryData)
{
    Console.WriteLine(line);
    if (first)
    {
        if (line != "Ingredient,Quantity")
        {
            Console.WriteLine("Invalid inventory header!");
            return;
        }
        first = false;
        continue;
    }

    var pieces = line.Split(',');
    if (pieces.Length != 2)
    {
        Console.WriteLine($"Invalid inventory line: {line}");
        return;
    }
    var ingredient = pieces[0];
    if (!int.TryParse(pieces[1], out var quantity) || quantity < 0)
    {
        Console.WriteLine($"Invalid quantity for ingredient {ingredient}: {pieces[1]}");
        return;
    }

    if (!ingredients.Contains(ingredient))
    {
        Console.WriteLine($"Ingredient {ingredient} not found in ingredients set!");
        return;
    }

    if (inventory.ContainsKey(ingredient))
    {
        Console.WriteLine($"Ingredient {ingredient} already exists in inventory!");
        return;
    }

    Console.WriteLine($"Adding to inventory: {ingredient} => {quantity}");
    inventory[ingredient] = quantity;
}

foreach (var ingredient in ingredients)
{
    if (!inventory.ContainsKey(ingredient))
    {
        inventory[ingredient] = 0;
    }
}

var ingredientCost = new Dictionary<string, Dictionary<string, int>>();

foreach (var ingredient in creationRecipes.Keys)
{
    var cost = computeCreateCost(ingredient);
    Console.WriteLine($"Cost to create {ingredient}: {string.Join(", ", cost.Select(c => $"{c.Value} x {c.Key}"))}");
}

Dictionary<string, int> computeCreateCost(string ingredient)
{
    if (ingredientCost.TryGetValue(ingredient, out var cachedCost))
    {
        return cachedCost;
    }

    var result = new Dictionary<string, int>();
    if (creationRecipes.TryGetValue(ingredient, out var existingIngredients))
    {
        result = new(computeCreateCost(existingIngredients.Ingredient1));
        combineCosts(result, computeCreateCost(existingIngredients.Ingredient2));
    }
    else
    {
        result = new() { [ingredient] = 1 };
    }
    ingredientCost[ingredient] = result;
    return result;
}

void combineCosts(Dictionary<string, int> target, Dictionary<string, int> addition)
{
    foreach (var (ingredient, quantity) in addition)
    {
        var existing = target.GetValueOrDefault(ingredient, 0);
        target[ingredient] = existing + quantity;
    }
}

var rewardCosts = new Dictionary<string, List<(string Ingredient1, string Ingredient2, int Quantity, Dictionary<string, int> Cost)>>();

for (int i = 0; i < ingredients.Count; i++)
{
    for (int j = i; j < ingredients.Count; j++)
    {
        var ingredient1 = ingredients[i];
        var ingredient2 = ingredients[j];
        (var quantity, var result) = recipes[(ingredient1, ingredient2)];

        var cost = new Dictionary<string, int>(ingredientCost[ingredient1]);
        combineCosts(cost, ingredientCost[ingredient2]);

        if (!rewardCosts.TryGetValue(result, out var existingCosts))
        {
            existingCosts = new();
            rewardCosts[result] = existingCosts;
        }
        existingCosts.Add((ingredient1, ingredient2, quantity, cost));
    }
}

foreach (var reward in rewardCosts.Keys.OrderBy(x => x))
{
    Console.WriteLine($"Possible ways to get reward {reward}:");
    foreach (var (ingredient1, ingredient2, quantity, cost) in rewardCosts[reward].OrderByDescending(x => x.Quantity))
    {
        Console.WriteLine($"  From ({ingredient1}, {ingredient2}) x {quantity} with cost: {string.Join(", ", cost.Select(c => $"{c.Value} x {c.Key}"))}");
    }
}