using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FrameTheory.AI;

namespace FrameTheory.Desktop;

public sealed class AiProposalActionViewModel : INotifyPropertyChanged
{
    private bool _isApproved;

    public AiProposalActionViewModel(AiEditAction action)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AiEditAction Action { get; }

    public string Summary =>
        $"{Action.Id} · {Action.Type} · target={Action.TargetId} · frames={Action.StartFrame}..{Action.StartFrame + Action.DurationFrames - 1} · params={JsonSerializer.Serialize(Action.Params)}";

    public bool IsApproved
    {
        get => _isApproved;
        set
        {
            if (value == _isApproved)
            {
                return;
            }

            _isApproved = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsApproved)));
        }
    }
}

public sealed class AiProposalViewModel : INotifyPropertyChanged
{
    private string _statusText = "No AI edit plan has been reviewed.";
    private IReadOnlyList<AiProposalActionViewModel> _actions = [];
    private string? _validatedJson;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<AiProposalActionViewModel> Actions
    {
        get => _actions;
        private set
        {
            foreach (var action in _actions)
            {
                action.PropertyChanged -= OnActionPropertyChanged;
            }

            _actions = value;
            foreach (var action in _actions)
            {
                action.PropertyChanged += OnActionPropertyChanged;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ApprovedCount));
            OnPropertyChanged(nameof(ApprovalSummary));
            OnPropertyChanged(nameof(CanApply));
        }
    }

    public int ApprovedCount => Actions.Count(action => action.IsApproved);

    public bool CanApply => Actions.Any(action => action.IsApproved);

    public string ApprovalSummary => Actions.Count == 0
        ? "No proposal actions to approve."
        : $"{ApprovedCount} of {Actions.Count} proposal action(s) approved.";

    public string? ValidatedJson => _validatedJson;

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (value == _statusText)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public void Validate(string json, AiPlanValidationContext context)
    {
        try
        {
            var plan = AiEditPlanParser.Parse(json);
            var validation = AiEditPlanValidator.Validate(plan, context);
            if (!validation.IsValid)
            {
                Actions = [];
                _validatedJson = null;
                OnPropertyChanged(nameof(ValidatedJson));
                StatusText = $"Plan rejected: {string.Join(" ", validation.Errors)}";
                return;
            }

            Actions = plan.Actions.Select(action => new AiProposalActionViewModel(action)).ToArray();
            _validatedJson = json;
            OnPropertyChanged(nameof(ValidatedJson));
            StatusText = $"Validated {Actions.Count} action(s). No project mutation occurred; review each action before approval.";
        }
        catch (AiPlanFormatException exception)
        {
            Actions = [];
            _validatedJson = null;
            OnPropertyChanged(nameof(ValidatedJson));
            StatusText = $"Plan rejected: {exception.Message}";
        }
    }

    public void RecordApprovals()
    {
        StatusText = $"Recorded approval for {ApprovedCount} of {Actions.Count} action(s). No project mutation occurred.";
    }

    public void ApplyApproved(Func<IReadOnlyList<AiEditAction>, AiPlanExecutionResult> executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        if (Actions.Count == 0)
        {
            StatusText = "No validated actions are available to apply.";
            return;
        }

        var result = executor(Actions.Where(action => action.IsApproved).Select(action => action.Action).ToArray());
        StatusText = result.Message;
    }

    public void Clear()
    {
        Actions = [];
        _validatedJson = null;
        OnPropertyChanged(nameof(ValidatedJson));
        StatusText = "AI proposal cleared; no project mutation occurred.";
    }

    public void SetUndoStatus(bool undone)
    {
        StatusText = undone
            ? "The last AI change group was undone; the proposal remains available for review."
            : "No AI change group is available to undo.";
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(AiProposalActionViewModel.IsApproved))
        {
            OnPropertyChanged(nameof(ApprovedCount));
            OnPropertyChanged(nameof(ApprovalSummary));
            OnPropertyChanged(nameof(CanApply));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
