using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrameTheory.AI;

public sealed record AiRecipe(string Name, string PlanJson);

public sealed class AiRecipeStore
{
    private const int CurrentSchemaVersion = 1;
    private const long MaximumRecipeBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task SaveAtomicAsync(string path, string name, string planJson, CancellationToken cancellationToken)
    {
        ValidateName(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(planJson);
        AiEditPlanParser.Parse(planJson);
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A recipe directory is required.", nameof(path));
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(directory);
        }

        EnsureRegularPath(fullPath, directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            }))
            {
                await JsonSerializer.SerializeAsync(stream, new RecipeFile(CurrentSchemaVersion, name.Trim(), planJson), Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    public async Task<AiRecipe> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = GetFullPath(path);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("The AI recipe was not found.", fullPath);
        }

        EnsureRegularPath(fullPath, fileInfo.DirectoryName
            ?? throw new ArgumentException("A recipe directory is required.", nameof(path)));

        if (fileInfo.Length > MaximumRecipeBytes)
        {
            throw new InvalidDataException("The AI recipe exceeds the supported size limit.");
        }

        await using var stream = new FileStream(fullPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        try
        {
            var recipe = await JsonSerializer.DeserializeAsync<RecipeFile>(stream, Options, cancellationToken)
                ?? throw new InvalidDataException("The AI recipe is empty.");
            if (recipe.SchemaVersion != CurrentSchemaVersion)
            {
                throw new InvalidDataException($"Unsupported AI recipe schema version {recipe.SchemaVersion}.");
            }

            ValidateName(recipe.Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(recipe.PlanJson);
            AiEditPlanParser.Parse(recipe.PlanJson);
            return new AiRecipe(recipe.Name.Trim(), recipe.PlanJson);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The AI recipe JSON is malformed.", exception);
        }
    }

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
        {
            throw new ArgumentException("An AI recipe name from 1 to 80 characters is required.", nameof(name));
        }
    }

    private static string GetFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("An AI recipe path is required.", nameof(path));
        }

        return Path.GetFullPath(path);
    }

    private static void EnsureRegularPath(string fullPath, string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("The AI recipe directory must not be a reparse point.");
        }

        if (File.Exists(fullPath) && (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("The AI recipe file must not be a reparse point.");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record RecipeFile(int SchemaVersion, string Name, string PlanJson);
}
