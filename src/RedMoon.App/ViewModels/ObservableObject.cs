using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Basisklasse der giver INotifyPropertyChanged, så XAML-bindinger opdateres automatisk.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Sætter et felt og giver besked til brugerfladen hvis værdien ændrede sig.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
