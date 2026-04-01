namespace System.NativeTray;

/// <summary>
/// Simple relay command implementation for tray actions.
/// Wraps an Action and optional predicate to control executability.
/// </summary>
/// <param name="execute">Action to execute when the command runs.</param>
/// <param name="canExecute">
/// Optional predicate that determines whether the command can currently execute.
/// If <c>null</c>, the command is always considered executable.
/// When used by tray menu items, returning <c>false</c> will also cause the menu item
/// to be shown as disabled (grayed out).
/// </param>
public class TrayCommand(Action<object?> execute, Func<bool>? canExecute = null) : ITrayCommand
{
    private readonly Action<object?> _execute =
        execute ?? throw new ArgumentNullException(nameof(execute));

    /// <inheritdoc/>
    public bool CanExecute()
    {
        return canExecute?.Invoke() ?? true;
    }

    /// <inheritdoc/>
    public void Execute(object? commandParameter)
    {
        if (CanExecute())
        {
            _execute?.Invoke(commandParameter);
        }
    }

    /// <summary>
    /// Allows an <see cref="Action{Object}"/> to be implicitly converted to a <see cref="TrayCommand"/>.
    /// This enables assigning an <see cref="Action{Object}"/> directly to variables or parameters
    /// of type <see cref="TrayCommand"/>.
    /// </summary>
    /// <param name="execute">The action to wrap in a tray command.</param>
    /// <returns>A new <see cref="TrayCommand"/> instance wrapping the provided action.</returns>
    public static implicit operator TrayCommand(Action<object?> execute)
    {
        return new TrayCommand(execute);
    }
}

/// <summary>
/// Basic tray command abstraction used to decouple tray menu items from their logic.
/// </summary>
public interface ITrayCommand
{
    /// <summary>
    /// Returns true if the command can currently be executed.
    /// In tray menus, this affects both execution and presentation: if this method
    /// returns false, the menu item will not execute and will be shown as disabled.
    /// </summary>
    public bool CanExecute();

    /// <summary>
    /// Executes the command's action.
    /// </summary>
    /// <param name="commandParameter">command's parameter</param>
    public void Execute(object? commandParameter);
}
