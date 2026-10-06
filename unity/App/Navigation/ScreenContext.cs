using System;
using System.Threading.Tasks;
using RedMoon.Core.Common;
using RedMoon.UnityApp.Bootstrap;
using RedMoon.UnityApp.UI;
using UnityEngine;

namespace RedMoon.UnityApp.Navigation
{
    /// <summary>
    /// Det alle skærme har brug for: services (Core), navigation og dialoger.
    /// Gives videre til hver skærm i konstruktøren (enkel dependency injection).
    /// </summary>
    internal sealed class ScreenContext
    {
        public ScreenContext(AppServices services, Navigator navigator, Dialogs dialogs)
        {
            Services = services;
            Navigator = navigator;
            Dialogs = dialogs;
        }

        public AppServices Services { get; }
        public Navigator Navigator { get; }
        public Dialogs Dialogs { get; }

        /// <summary>
        /// Kører en handling med fejlhåndtering. Forventede fejl (fx forkert input) vises til brugeren via
        /// <paramref name="onError"/>; uventede fejl logges uden brugerdata. Returnerer sand ved succes.
        /// </summary>
        public async Task<bool> RunAsync(Func<Task> action, Action<string>? onError, bool showBusy = true)
        {
            if (showBusy) Dialogs.ShowBusy();
            try
            {
                await action();
                return true;
            }
            catch (RedMoonException ex)
            {
                onError?.Invoke(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                onError?.Invoke("Noget gik galt. Prøv igen.");
                return false;
            }
            finally
            {
                if (showBusy) Dialogs.HideBusy();
            }
        }
    }
}
