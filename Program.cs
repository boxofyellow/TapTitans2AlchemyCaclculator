// Parse optional command line argument for targeted reward
var targetedReward = "Wildcards";
if (args.Length > 0)
{
    targetedReward = args[0];
}

string[] recipeData = File.ReadAllLines("recipeData.csv");

// Start hidden section for recipe data loading
Console.WriteLine("<details>");
Console.WriteLine("<summary>📁 Recipe Data Loading</summary>");
Console.WriteLine();
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

// End recipe data loading section
Console.WriteLine();
Console.WriteLine("</details>");

string[] inventoryData = File.ReadAllLines("inventoryData.csv");

// Start hidden section for inventory data loading
Console.WriteLine("<details>");
Console.WriteLine("<summary>📦 Inventory Data Loading</summary>");
Console.WriteLine();
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

// End inventory data loading section
Console.WriteLine();
Console.WriteLine("</details>");

foreach (var ingredient in ingredients)
{
    if (!inventory.ContainsKey(ingredient))
    {
        inventory[ingredient] = 0;
    }
}

var ingredientCost = new Dictionary<string, Dictionary<string, int>>();

// Start hidden section for recipe analysis
Console.WriteLine("<details>");
Console.WriteLine("<summary>📈 Recipe Analysis</summary>");
Console.WriteLine();

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

foreach (var reward in rewardCosts.Keys.OrderBy(x => x))
{
    Console.WriteLine($"Possible ways to get reward {reward}:");
    foreach (var (ingredient1, ingredient2, quantity, cost) in rewardCosts[reward].OrderByDescending(x => x.Quantity))
    {
        Console.WriteLine($"  From ({ingredient1}, {ingredient2}) x {quantity} with cost: {string.Join(", ", cost.Select(c => $"{c.Value} x {c.Key}"))} [{((double)quantity)/(double)cost.Sum(c => c.Value)}]");
    }
}

// End recipe analysis section
Console.WriteLine();
Console.WriteLine("</details>");

// Maximize crafting the target reward
Console.WriteLine();
Console.WriteLine($"## 🎯 OPTIMIZING FOR TARGET REWARD: {targetedReward}");
Console.WriteLine();

if (!rewardCosts.ContainsKey(targetedReward))
{
    Console.WriteLine($"Target reward '{targetedReward}' not found in recipes!");
    return;
}

// Get all recipes for the target reward, ordered by efficiency (quantity per base ingredient)
var targetRecipes = rewardCosts[targetedReward]
    .OrderByDescending(r => (double)r.Quantity / r.Cost.Sum(c => c.Value))
    .ToList();

// Create a working copy of inventory
var workingInventory = new Dictionary<string, int>(inventory);

// Track total rewards obtained and crafting steps
var totalRewards = 0;
var craftingPlan = new List<(string Action, string Ingredient1, string Ingredient2, int Quantity, string Details)>();

// Helper function to check if we can craft an ingredient
bool canCraftIngredient(string ingredient, Dictionary<string, int> currentInventory)
{
    if (currentInventory.GetValueOrDefault(ingredient, 0) > 0)
    {
        return true;
    }

    if (creationRecipes.TryGetValue(ingredient, out var recipe))
    {
        var (ing1, ing2) = recipe;
        return canCraftIngredient(ing1, currentInventory) && canCraftIngredient(ing2, currentInventory);
    }
    
    return false;
}

// Helper function to craft an ingredient (recursively if needed)
bool tryCraftIngredient(string ingredient, Dictionary<string, int> currentInventory, List<(string, string, string, string)> steps)
{
    if (currentInventory.GetValueOrDefault(ingredient, 0) > 0)
    {
        currentInventory[ingredient]--;
        return true;
    }
    
    if (!creationRecipes.TryGetValue(ingredient, out var recipe))
    {
        return false;
    }

    var (ing1, ing2) = recipe;

    if (tryCraftIngredient(ing1, currentInventory, steps) && tryCraftIngredient(ing2, currentInventory, steps))
    {
        steps.Add(("CREATE", ing1, ing2, ingredient));
        return true;
    }
    
    return false;
}

// Start hidden section for Phase 1
Console.WriteLine("<details>");
Console.WriteLine("<summary>⚗️ PHASE 1: Using existing complex ingredients</summary>");
Console.WriteLine();

// Phase 1: Prioritize using complex ingredients to minimize waste
foreach (var (ing1, ing2, quantity, cost) in targetRecipes)
{
    while (true)
    {
        var steps = new List<(string, string, string, string)>();
        var testInventory = new Dictionary<string, int>(workingInventory);
        
        if (tryCraftIngredient(ing1, testInventory, steps) && tryCraftIngredient(ing2, testInventory, steps))
        {
            // Calculate how many complex ingredients we used
            var complexUsed = steps.Count(s => s.Item1 == "CREATE");
            
            // Only proceed if we're using at least one complex ingredient in this phase
            if (complexUsed == 0)
            {
                break;
            }
            
            // Apply the crafting
            workingInventory = testInventory;
            
            // Log intermediate crafting steps
            foreach (var (action, i1, i2, result) in steps)
            {
                craftingPlan.Add((action, i1, i2, 1, $"Creating {result}"));
            }
            
            // Log the recipe execution
            craftingPlan.Add(("CRAFT", ing1, ing2, quantity, $"Yielding {quantity} x {targetedReward}"));
            totalRewards += quantity;
            
            Console.WriteLine($"Crafted ({ing1}, {ing2}) => {quantity} x {targetedReward}");
            if (steps.Count > 0)
            {
                Console.WriteLine($"  Required creating: {string.Join(", ", steps.Select(s => s.Item4))}");
            }
        }
        else
        {
            break;
        }
    }
}

Console.WriteLine($"\nPhase 1 complete. Total rewards so far: {totalRewards}");
Console.WriteLine($"Remaining inventory: {string.Join(", ", workingInventory.Where(kv => kv.Value > 0).Select(kv => $"{kv.Value} x {kv.Key}"))}");

// End Phase 1 section
Console.WriteLine();
Console.WriteLine("</details>");

// Start hidden section for Phase 2
Console.WriteLine("<details>");
Console.WriteLine("<summary>⚗️ PHASE 2: Using remaining base ingredients</summary>");
Console.WriteLine();

// Phase 2: Use base ingredients efficiently
foreach (var (ing1, ing2, quantity, cost) in targetRecipes)
{
    while (true)
    {
        var steps = new List<(string, string, string, string)>();
        var testInventory = new Dictionary<string, int>(workingInventory);
        
        if (tryCraftIngredient(ing1, testInventory, steps) && tryCraftIngredient(ing2, testInventory, steps))
        {
            // Apply the crafting
            workingInventory = testInventory;
            
            // Log intermediate crafting steps
            foreach (var (action, i1, i2, result) in steps)
            {
                if (action == "CREATE")
                {
                    craftingPlan.Add((action, i1, i2, 1, $"Creating {result}"));
                }
            }
            
            // Log the recipe execution
            craftingPlan.Add(("CRAFT", ing1, ing2, quantity, $"Yielding {quantity} x {targetedReward}"));
            totalRewards += quantity;
            
            Console.WriteLine($"Crafted ({ing1}, {ing2}) => {quantity} x {targetedReward}");
            if (steps.Count > 0)
            {
                var createdIngredients = steps.Where(s => s.Item1 == "CREATE").Select(s => s.Item4);
                if (createdIngredients.Any())
                {
                    Console.WriteLine($"  Required creating: {string.Join(", ", createdIngredients)}");
                }
            }
        }
        else
        {
            break;
        }
    }
}

// End Phase 2 section
Console.WriteLine();
Console.WriteLine("</details>");

// Start hidden section for Optimization Complete
Console.WriteLine("<details>");
Console.WriteLine("<summary>✅ OPTIMIZATION COMPLETE</summary>");
Console.WriteLine();
Console.WriteLine($"**Total {targetedReward} obtained: {totalRewards}**");
Console.WriteLine();
Console.WriteLine($"Remaining inventory: {string.Join(", ", workingInventory.Where(kv => kv.Value > 0).Select(kv => $"{kv.Value} x {kv.Key}"))}");

// End Optimization Complete section
Console.WriteLine();
Console.WriteLine("</details>");

Console.WriteLine();
Console.WriteLine("## 📋 DETAILED CRAFTING PLAN");
Console.WriteLine();
var stepNumber = 1;
foreach (var (action, ing1, ing2, qty, details) in craftingPlan)
{
    if (action == "CREATE")
    {
        Console.WriteLine($"{stepNumber}. Combine **{ing1}** + **{ing2}** to create intermediate ingredient - {details}");
    }
    else
    {
        Console.WriteLine($"{stepNumber}. Combine **{ing1}** + **{ing2}** => {qty} x {targetedReward}");
    }
    stepNumber++;
}

Console.WriteLine();
Console.WriteLine("## 📊 RECIPE SUMMARY");
Console.WriteLine();
var recipeCounts = new Dictionary<(string, string), int>();

foreach (var (action, ing1, ing2, qty, details) in craftingPlan)
{
    var key = (ing1, ing2);
    recipeCounts[key] = recipeCounts.GetValueOrDefault(key, 0) + 1;
}

foreach (var ((ing1, ing2), count) in recipeCounts.OrderByDescending(kv => kv.Value))
{
    var recipeKey = (ing1, ing2);
    if (recipes.TryGetValue(recipeKey, out var recipeResult))
    {
        Console.WriteLine($"- **({ing1} + {ing2})** => {recipeResult.Quantity} x {recipeResult.Result}: Execute {count} time(s)");
    }
    else
    {
        Console.WriteLine($"- **({ing1} + {ing2})**: Execute {count} time(s)");
    }
}

// Start hidden section for Remaining Inventory
Console.WriteLine();
Console.WriteLine("<details>");
Console.WriteLine("<summary>📦 REMAINING INVENTORY (CSV FORMAT)</summary>");
Console.WriteLine();
Console.WriteLine("```csv");
Console.WriteLine("Ingredient,Quantity");
foreach (var ingredient in ingredients)
{
    Console.WriteLine($"{ingredient},{workingInventory.GetValueOrDefault(ingredient, 0)}");
}
Console.WriteLine("```");

// End Remaining Inventory section
Console.WriteLine();
Console.WriteLine("</details>");