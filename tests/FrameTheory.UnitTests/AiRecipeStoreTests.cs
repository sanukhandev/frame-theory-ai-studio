using FrameTheory.AI;

namespace FrameTheory.UnitTests;

public sealed class AiRecipeStoreTests
{
    [Xunit.Fact]
    public async Task RecipeRoundTripValidatesAndPreservesPlanJson()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-ai-recipe-");
        try
        {
            var path = Path.Combine(directory.FullName, "recipe.ftrecipe.json");
            var json = PlanJson("recipe-plan");
            var store = new AiRecipeStore();

            await store.SaveAtomicAsync(path, "Cinematic grade", json, CancellationToken.None);
            var recipe = await store.LoadAsync(path, CancellationToken.None);

            Xunit.Assert.Equal("Cinematic grade", recipe.Name);
            Xunit.Assert.Equal(json, recipe.PlanJson);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task RecipeStoreRejectsMalformedOrUnallowlistedPlans()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-ai-recipe-");
        try
        {
            var store = new AiRecipeStore();
            await Xunit.Assert.ThrowsAsync<AiPlanFormatException>(() => store.SaveAtomicAsync(
                Path.Combine(directory.FullName, "invalid.ftrecipe.json"),
                "Invalid",
                "{\"schemaVersion\":\"1.0\"}",
                CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task RecipeStoreDoesNotReplaceExistingFileWhenCanceled()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-ai-recipe-");
        try
        {
            var path = Path.Combine(directory.FullName, "recipe.ftrecipe.json");
            var store = new AiRecipeStore();
            var original = PlanJson("original");
            await store.SaveAtomicAsync(path, "Original", original, CancellationToken.None);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAtomicAsync(path, "Changed", PlanJson("changed"), cancellation.Token));
            Xunit.Assert.Contains("Original", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static string PlanJson(string planId) => $$"""
        {
          "schemaVersion": "1.0",
          "planId": "{{planId}}",
          "projectId": "active-project",
          "intent": "Apply a cinematic grade",
          "actions": []
        }
        """;
}
