using System;
using System.Threading.Tasks;
using RedMoon.UnityApp.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Navigation
{
    /// <summary>
    /// Basisklasse for alle skærme. En skærm bygger sit indhold i konstruktøren og opdaterer data i OnShow.
    /// Ny skærm: arv fra ScreenBase, og vis den med Navigator.Push(...) eller tilføj den i TabRegistry.
    /// </summary>
    internal abstract class ScreenBase
    {
        private Label? _error;

        protected ScreenBase(ScreenContext context, string title)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Title = title;
            Root = Ui.Box("rm-screen");
        }

        /// <summary>Skærmens rod-element, som Navigator viser.</summary>
        public VisualElement Root { get; }

        /// <summary>Titel i toplinjen, når skærmen er åbnet som underside.</summary>
        public string Title { get; }

        protected ScreenContext Context { get; }

        /// <summary>Kaldes hver gang skærmen bliver synlig. Opdater data her.</summary>
        public virtual void OnShow()
        {
        }

        /// <summary>Kaldes når skærmen skjules. Glem fx følsomme midlertidige værdier her.</summary>
        public virtual void OnHide()
        {
        }

        /// <summary>Opretter skærmens fejltekst (rød) i <paramref name="parent"/>.</summary>
        protected Label CreateErrorLabel(VisualElement parent)
        {
            _error = Ui.Label(string.Empty, "rm-error");
            parent.Add(_error);
            return _error;
        }

        protected void ShowError(string message)
        {
            if (_error != null) _error.text = message;
        }

        protected void ClearError() => ShowError(string.Empty);

        /// <summary>Kører en handling med fejlhåndtering; fejl vises i skærmens fejltekst.</summary>
        protected Task<bool> RunAsync(Func<Task> action, bool showBusy = true) => Context.RunAsync(action, ShowError, showBusy);

        /// <summary>Starter en async handling fra en knap (fejl logges i stedet for at gå tabt).</summary>
        protected static void Fire(Func<Task> action)
        {
            _ = FireAsync(action);
        }

        private static async Task FireAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
