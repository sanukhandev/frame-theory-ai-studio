namespace FrameTheory.Domain;

public interface IEditCommand
{
    string Name { get; }

    ProjectSnapshot Apply(ProjectSnapshot source);

    IEditCommand Inverse(ProjectSnapshot before);
}

public sealed class SetCanvasCommand : IEditCommand
{
    public SetCanvasCommand(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public string Name => "Set canvas";

    public ProjectSnapshot Apply(ProjectSnapshot source) => source.WithCanvas(Width, Height);

    public IEditCommand Inverse(ProjectSnapshot before) => new SetCanvasCommand(before.CanvasWidth, before.CanvasHeight);
}

public sealed class AddMediaReferenceCommand : IEditCommand
{
    public AddMediaReferenceCommand(MediaReference mediaReference) => MediaReference = mediaReference ?? throw new ArgumentNullException(nameof(mediaReference));

    public MediaReference MediaReference { get; }

    public string Name => "Add media reference";

    public ProjectSnapshot Apply(ProjectSnapshot source)
    {
        if (source.MediaReferences.Any(reference => reference.Id == MediaReference.Id))
        {
            throw new InvalidOperationException("The media reference already exists.");
        }

        return source.WithMediaReferences(source.MediaReferences.Append(MediaReference));
    }

    public IEditCommand Inverse(ProjectSnapshot before) => new RemoveMediaReferenceCommand(MediaReference);
}

public sealed class SetColorPresetCommand : IEditCommand
{
    public SetColorPresetCommand(string? presetName) => PresetName = string.IsNullOrWhiteSpace(presetName) ? null : presetName.Trim();

    public string Name => PresetName is null ? "Clear color preset" : $"Apply color preset '{PresetName}'";

    public string? PresetName { get; }

    public ProjectSnapshot Apply(ProjectSnapshot source) => source.WithColorPreset(PresetName);

    public IEditCommand Inverse(ProjectSnapshot before) => new SetColorPresetCommand(before.ColorPresetName);
}

public sealed class RemoveMediaReferenceCommand : IEditCommand
{
    public RemoveMediaReferenceCommand(MediaReference mediaReference) => MediaReference = mediaReference ?? throw new ArgumentNullException(nameof(mediaReference));

    public MediaReference MediaReference { get; }

    public string Name => "Remove media reference";

    public ProjectSnapshot Apply(ProjectSnapshot source)
    {
        var remaining = source.MediaReferences.Where(reference => reference.Id != MediaReference.Id).ToArray();
        if (remaining.Length == source.MediaReferences.Count)
        {
            throw new InvalidOperationException("The media reference does not exist.");
        }

        return source.WithMediaReferences(remaining);
    }

    public IEditCommand Inverse(ProjectSnapshot before) => new AddMediaReferenceCommand(MediaReference);
}

public sealed class EditCommandGroup : IEditCommand
{
    private readonly IReadOnlyList<IEditCommand> _commands;

    public EditCommandGroup(string name, IEnumerable<IEditCommand> commands)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A command group name is required.", nameof(name));
        }

        _commands = commands?.ToArray() ?? throw new ArgumentNullException(nameof(commands));
        if (_commands.Count == 0)
        {
            throw new ArgumentException("A command group must contain a command.", nameof(commands));
        }

        if (_commands.Any(command => command is null))
        {
            throw new ArgumentException("A command group cannot contain null commands.", nameof(commands));
        }

        Name = name.Trim();
    }

    public string Name { get; }

    public ProjectSnapshot Apply(ProjectSnapshot source)
    {
        var current = source;
        foreach (var command in _commands)
        {
            current = command.Apply(current);
        }

        return current;
    }

    public IEditCommand Inverse(ProjectSnapshot before)
    {
        var states = new List<ProjectSnapshot>();
        var current = before;
        foreach (var command in _commands)
        {
            states.Add(current);
            current = command.Apply(current);
        }

        var inverses = new List<IEditCommand>(_commands.Count);
        for (var index = _commands.Count - 1; index >= 0; index--)
        {
            inverses.Add(_commands[index].Inverse(states[index]));
        }

        return new EditCommandGroup($"Undo {Name}", inverses);
    }
}

public sealed class EditHistory
{
    private readonly int _maximumDepth;
    private readonly List<HistoryEntry> _undo = [];
    private readonly List<HistoryEntry> _redo = [];

    public EditHistory(ProjectSnapshot initialState, int maximumDepth = 100)
    {
        Current = initialState ?? throw new ArgumentNullException(nameof(initialState));
        if (maximumDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        }

        _maximumDepth = maximumDepth;
    }

    public ProjectSnapshot Current { get; private set; }

    public int UndoCount => _undo.Count;

    public int RedoCount => _redo.Count;

    public string? UndoDescription => _undo.Count == 0 ? null : _undo[^1].Command.Name;

    public void Execute(IEditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var before = Current;
        var inverse = command.Inverse(before);
        var after = command.Apply(before);
        _undo.Add(new HistoryEntry(command, before, after, inverse));
        if (_undo.Count > _maximumDepth)
        {
            _undo.RemoveAt(0);
        }

        _redo.Clear();
        Current = after;
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }

        var entry = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        Current = entry.Before;
        _redo.Add(entry);
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0)
        {
            return false;
        }

        var entry = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        Current = entry.After;
        _undo.Add(entry);
        if (_undo.Count > _maximumDepth)
        {
            _undo.RemoveAt(0);
        }

        return true;
    }

    private sealed record HistoryEntry(
        IEditCommand Command,
        ProjectSnapshot Before,
        ProjectSnapshot After,
        IEditCommand Inverse);
}
