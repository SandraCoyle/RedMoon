using System.Diagnostics;
using RedMoon.Core.Common;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Fælles basis for alle skærmes ViewModels: optaget-indikator, fejlbesked og sikker kørsel af handlinger.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    /// <summary>Sand mens en handling kører (viser spinner, deaktiverer knapper).</summary>
    public bool IsBusy
    {
        get => _isBusy;
        protected set
        {
            if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    /// <summary>Fejlbesked der vises på skærmen (tom = ingen fejl).</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        protected set
        {
            if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Kører en handling med fejlhåndtering:
    /// forventede fejl (RedMoonException) vises med deres danske besked,
    /// uventede fejl vises som en generel besked uden tekniske detaljer eller brugerdata.
    /// Returnerer sand hvis handlingen lykkedes.
    /// </summary>
    protected async Task<bool> RunAsync(Func<Task> action, bool showBusy = true)
    {
        if (showBusy) IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await action();
            return true;
        }
        catch (RedMoonException ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            // Kun fejltypen logges (i Debug) – aldrig indhold, da det kan være følsomt.
            Debug.WriteLine($"RedMoon: uventet fejl af typen {ex.GetType().Name}");
            ErrorMessage = "Noget gik galt. Prøv igen.";
            return false;
        }
        finally
        {
            if (showBusy) IsBusy = false;
        }
    }
}
