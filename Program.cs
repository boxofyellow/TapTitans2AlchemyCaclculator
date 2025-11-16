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
            existingCosts = [];
            rewardCosts[result] = existingCosts;
        }
        existingCosts.Add((ingredient1, ingredient2, quantity, cost));
    }
}


//var allOptionsRewardCosts = new Dictionary<string, List<(string Ingredient1, string Ingredient2, int Quantity, Dictionary<string, int> Cost)>>(rewardCosts);
foreach (var recipe in recipes)
{
    var (ingredient1, ingredient2) = recipe.Key;
    var (quantity, result) = recipe.Value;

    if (!rewardCosts.TryGetValue(result, out var existingCosts))
    {
        throw new Exception("Should always exist from previous population");
    }

    if (creationRecipes.ContainsKey(ingredient1))
    {
        var cost2 = new Dictionary<string, int>(ingredientCost[ingredient2]); 
        combineCosts(cost2, new Dictionary<string, int> { [ingredient1] = 1 });
        existingCosts.Add((ingredient1, ingredient2, quantity, cost2));
    }

    if (creationRecipes.ContainsKey(ingredient2))
    {
        var cost1 = new Dictionary<string, int>(ingredientCost[ingredient1]);
        combineCosts(cost1, new Dictionary<string, int> { [ingredient2] = 1 });
        existingCosts.Add((ingredient1, ingredient2, quantity, cost1));

        if (creationRecipes.ContainsKey(ingredient1))
        {
            existingCosts.Add((ingredient1, ingredient2, quantity, new Dictionary<string, int> { [ingredient2] = 1, [ingredient2] = 2 }));
        }
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

// //var allOptionsRewardCosts = new Dictionary<string, List<(string Ingredient1, string Ingredient2, int Quantity, Dictionary<string, int> Cost)>>(rewardCosts);
// foreach (var recipe in recipes)
// {
//     var (ingredient1, ingredient2) = recipe.Key;
//     var (quantity, result) = recipe.Value;

//     if (!rewardCosts.TryGetValue(result, out var existingCosts))
//     {
//         throw new Exception("Should always exist from previous population");
//     }

//     if (creationRecipes.ContainsKey(ingredient1))
//     {
//         var cost2 = new Dictionary<string, int>(ingredientCost[ingredient2]); 
//         combineCosts(cost2, new Dictionary<string, int> { [ingredient1] = 1 });
//         existingCosts.Add((ingredient1, ingredient2, quantity, cost2));
//     }

//     if (creationRecipes.ContainsKey(ingredient2))
//     {
//         var cost1 = new Dictionary<string, int>(ingredientCost[ingredient1]);
//         combineCosts(cost1, new Dictionary<string, int> { [ingredient2] = 1 });
//         existingCosts.Add((ingredient1, ingredient2, quantity, cost1));

//         if (creationRecipes.ContainsKey(ingredient1))
//         {
//             existingCosts.Add((ingredient1, ingredient2, quantity, new Dictionary<string, int> { [ingredient2] = 1, [ingredient2] = 2 }));
//         }
//     }
// }

var targetedReward = "Wildcards";

Console.WriteLine($"\n=== Computing optimal recipes to maximize {targetedReward} ===\n");

// Get all possible ways to create the targeted reward
if (!rewardCosts.TryGetValue(targetedReward, out var targetRewardOptions))
{
    Console.WriteLine($"No recipes found that produce {targetedReward}!");
    return;
}


while (true)
{
    // For each recipe option, compute how many times we can execute it with current inventory
    var feasibleRecipes = new List<(string Ingredient1, string Ingredient2, int Quantity, int MaxExecutions, Dictionary<string, int> Cost)>();

    foreach (var (ingredient1, ingredient2, quantity, cost) in targetRewardOptions)
    {
        // Calculate how many times we can execute this recipe based on our inventory
        int maxExecutions = int.MaxValue;
        
        foreach (var (requiredIngredient, requiredQuantity) in cost)
        {
            var available = inventory[requiredIngredient];
            var possibleExecutions = available / requiredQuantity;
            maxExecutions = Math.Min(maxExecutions, possibleExecutions);
        }
        
        if (maxExecutions > 0)
        {
            feasibleRecipes.Add((ingredient1, ingredient2, quantity, maxExecutions, cost));
        }
    }

    if (feasibleRecipes.Count == 0)
    {
        Console.WriteLine($"No recipes can be executed with current inventory to produce {targetedReward}!");
        break;
    }
    else
    {
        Console.WriteLine($"Feasible recipes for {targetedReward}:");
        foreach (var (ingredient1, ingredient2, quantity, maxExecutions, cost) in feasibleRecipes.OrderByDescending(x => x.Quantity * x.MaxExecutions))
        {
            var totalReward = quantity * maxExecutions;
            Console.WriteLine($"  ({ingredient1}, {ingredient2}) can be executed {maxExecutions} times");
            Console.WriteLine($"    Yields: {quantity} x {targetedReward} per execution = {totalReward} total {targetedReward}");
            Console.WriteLine($"    Cost per execution: {string.Join(", ", cost.Select(c => $"{c.Value} x {c.Key}"))}");
        }
        
        // Now find the optimal combination using an iterative approach that considers crafting intermediate ingredients
        Console.WriteLine($"\n=== Finding optimal recipe combination (including intermediate crafting) ===\n");
        
        // First, build a set of all ingredients that can eventually lead to the target reward
        var ingredientsLeadingToTarget = new HashSet<string> { targetedReward };
        bool foundNewIngredients = true;
        
        while (foundNewIngredients)
        {
            foundNewIngredients = false;
            
            for (int i = 0; i < ingredients.Count; i++)
            {
                for (int j = i; j < ingredients.Count; j++)
                {
                    var ing1 = ingredients[i];
                    var ing2 = ingredients[j];
                    
                    if (!recipes.ContainsKey((ing1, ing2)))
                    {
                        continue;
                    }
                    
                    var (_, result) = recipes[(ing1, ing2)];
                    
                    // If this recipe produces something that leads to target, then the ingredients also lead to target
                    if (ingredientsLeadingToTarget.Contains(result))
                    {
                        if (!ingredientsLeadingToTarget.Contains(ing1))
                        {
                            ingredientsLeadingToTarget.Add(ing1);
                            foundNewIngredients = true;
                        }
                        if (!ingredientsLeadingToTarget.Contains(ing2))
                        {
                            ingredientsLeadingToTarget.Add(ing2);
                            foundNewIngredients = true;
                        }
                    }
                }
            }
        }
        
        Console.WriteLine($"Ingredients that can lead to {targetedReward}: {string.Join(", ", ingredientsLeadingToTarget.OrderBy(x => x))}");
        
        var workingInventory = new Dictionary<string, int>(inventory);
        var executionPlan = new List<(string Ingredient1, string Ingredient2, string Result, int Quantity, int TimesToExecute)>();
        int totalRewardObtained = 0;
        
        bool madeProgress = true;
        int iteration = 0;
        
        while (madeProgress)
        {
            madeProgress = false;
            iteration++;
            Console.WriteLine($"\n--- Iteration {iteration} ---");
            
            // Try all possible recipes with current working inventory
            var possibleRecipes = new List<(string Ing1, string Ing2, string Result, int Quantity, int CanExecute, double Value)>();
            
            for (int i = 0; i < ingredients.Count; i++)
            {
                for (int j = i; j < ingredients.Count; j++)
                {
                    var ing1 = ingredients[i];
                    var ing2 = ingredients[j];
                    
                    if (!recipes.ContainsKey((ing1, ing2)))
                    {
                        continue;
                    }
                    
                    var (quantity, result) = recipes[(ing1, ing2)];
                    var cost = new Dictionary<string, int>(ingredientCost[ing1]);
                    combineCosts(cost, ingredientCost[ing2]);
                    
                    // Check how many times we can execute this recipe
                    int canExecute = int.MaxValue;
                    foreach (var (requiredIngredient, requiredQuantity) in cost)
                    {
                        var available = workingInventory[requiredIngredient];
                        var possibleExecutions = available / requiredQuantity;
                        canExecute = Math.Min(canExecute, possibleExecutions);
                    }
                    
                    if (canExecute > 0)
                    {
                        // Calculate the value of this recipe
                        // Only consider recipes that lead to the target reward
                        double value = 0;
                        
                        if (result == targetedReward)
                        {
                            // Direct production of target is highly valuable
                            value = quantity * 1000.0;
                        }
                        else if (ingredientsLeadingToTarget.Contains(result))
                        {
                            // This result can eventually lead to the target
                            // Value it based on how directly needed it is
                            if (rewardCosts.ContainsKey(targetedReward))
                            {
                                foreach (var (reqIng1, reqIng2, reqQty, reqCost) in rewardCosts[targetedReward])
                                {
                                    if (reqCost.ContainsKey(result))
                                    {
                                        // This ingredient is directly needed for the target!
                                        value = quantity * 100.0;
                                        break;
                                    }
                                }
                            }
                            
                            if (value == 0)
                            {
                                // It leads to target but indirectly
                                value = quantity * 10.0;
                            }
                        }
                        
                        // Only add recipes that have value > 0 (i.e., lead to the target)
                        if (value > 0)
                        {
                            // Adjust value by cost efficiency
                            var totalCost = cost.Sum(c => c.Value);
                            value = value / totalCost;
                            
                            possibleRecipes.Add((ing1, ing2, result, quantity, canExecute, value));
                        }
                    }
                }
            }
            
            if (possibleRecipes.Count == 0)
            {
                Console.WriteLine("No more recipes can be executed.");
                break;
            }
            
            // Prioritize recipes that directly produce the target reward
            var directTargetRecipes = possibleRecipes.Where(r => r.Result == targetedReward).ToList();
            
            var bestRecipe = directTargetRecipes.Count > 0
                ? directTargetRecipes.OrderByDescending(r => r.Value).First()
                : possibleRecipes.OrderByDescending(r => r.Value).First();
            
            var (bestIng1, bestIng2, bestResult, bestQuantity, bestCanExecute, bestValue) = bestRecipe;
            var bestCost = new Dictionary<string, int>(ingredientCost[bestIng1]);
            combineCosts(bestCost, ingredientCost[bestIng2]);
            
            // Execute this recipe
            foreach (var (requiredIngredient, requiredQuantity) in bestCost)
            {
                workingInventory[requiredIngredient] -= requiredQuantity * bestCanExecute;
            }
            
            // Add the result to inventory
            if (bestResult == targetedReward)
            {
                var rewardYield = bestQuantity * bestCanExecute;
                totalRewardObtained += rewardYield;
                Console.WriteLine($"Execute ({bestIng1}, {bestIng2}) x {bestCanExecute} times -> {rewardYield} {targetedReward} (DIRECT)");
                executionPlan.Add((bestIng1, bestIng2, bestResult, bestQuantity, bestCanExecute));
            }
            else
            {
                var produced = bestQuantity * bestCanExecute;
                workingInventory[bestResult] = workingInventory.GetValueOrDefault(bestResult, 0) + produced;
                Console.WriteLine($"Execute ({bestIng1}, {bestIng2}) x {bestCanExecute} times -> {produced} {bestResult} (intermediate, value={bestValue:F2})");
                executionPlan.Add((bestIng1, bestIng2, bestResult, bestQuantity, bestCanExecute));
            }
            
            madeProgress = true;
            
            // Safety limit to prevent infinite loops
            if (iteration > 1000)
            {
                Console.WriteLine("Hit iteration limit!");
                break;
            }
        }
        
        Console.WriteLine($"\n=== Execution Plan Summary ===");
        Console.WriteLine($"Total {targetedReward} obtained: {totalRewardObtained}\n");
        
        if (executionPlan.Count > 0)
        {
            Console.WriteLine("Recipes to execute:");
            foreach (var (ingredient1, ingredient2, result, quantity, timesToExecute) in executionPlan)
            {
                var produced = quantity * timesToExecute;
                Console.WriteLine($"  Combine {ingredient1} + {ingredient2}: {timesToExecute} times -> {produced} {result}");
            }
            
            Console.WriteLine("\nRemaining inventory:");
            foreach (var (ingredient, quantity) in workingInventory.OrderBy(x => x.Key))
            {
                if (quantity > 0)
                {
                    Console.WriteLine($"  {ingredient}: {quantity}");
                }
            }
        }

        var madeOverAllProgress = false;
        foreach (var ingredient in inventory.Keys)
        {
            if (!(workingInventory.TryGetValue(ingredient, out var workingQuantity) && workingQuantity == inventory[ingredient]))
            {
                madeOverAllProgress = true;
                break;
            }
        }

        if (!madeOverAllProgress)
        {
            break;
        }

        inventory = workingInventory;
    }
}