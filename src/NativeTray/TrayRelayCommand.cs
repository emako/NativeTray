namespace System.NativeTray;

/// <summary>
/// Simple relay command implementation for tray actions.
/// Wraps an Action and optional predicate to control executability.
/// </summary>
/// <param name="execute">Action to execute when the command runs.</param>
/// <param name="canExecute">
/// Optional predicate that determines whether the command can currently execute.
/// If <c>null</c>, the command is always considered executable.
/// </param>
public class TrayRelayCommand(Action<object?> execute, Func<bool>? canExecute = null) : ITrayCommand
{
    private readonly Action<object?> _execute =
        execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute()
    {
        return canExecute?.Invoke() ?? true;
    }

    public void Execute(object? commandParameter)
    {
        if (CanExecute())
        {
            _execute?.Invoke(commandParameter);
        }
    }
}

/// <summary>
/// Basic tray command abstraction used to decouple tray menu items from their logic.
/// </summary>
public interface ITrayCommand
{
    // Returns true if the command can currently be executed.
    public bool CanExecute();

    // Executes the command's action.
    public void Execute(object? commandParameter);
}
