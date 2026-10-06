using System;
using System.Collections.Generic;
using RedMoon.UnityApp.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Controls
{
    /// <summary>Én valgmulighed: værdi, tekst og et tegnet ikon.</summary>
    internal readonly struct Choice<T> where T : struct, Enum
    {
        public Choice(T value, string text, Texture2D icon)
        {
            Value = value;
            Text = text;
            Icon = icon;
        }

        public T Value { get; }
        public string Text { get; }
        public Texture2D Icon { get; }
    }

    /// <summary>
    /// En række ikon-knapper hvor én kan være valgt (humør, menstruationsstatus, intensitet).
    /// Genbruges på kalender- og profilskærmen.
    /// </summary>
    internal sealed class ChoiceRow<T> : VisualElement where T : struct, Enum
    {
        private readonly Dictionary<T, Button> _buttons = new Dictionary<T, Button>();

        public ChoiceRow(IEnumerable<Choice<T>> choices, float iconSize = 34f)
        {
            AddToClassList("rm-chip-row");
            foreach (var choice in choices)
            {
                var value = choice.Value;
                var button = new Button(() => Selected?.Invoke(value)) { text = string.Empty };
                button.AddToClassList("rm-chip");
                button.Add(Ui.Icon(choice.Icon, iconSize));
                button.Add(Ui.Label(choice.Text, "rm-chip-label"));
                _buttons[value] = button;
                Add(button);
            }
        }

        /// <summary>Udløses når brugeren trykker på en valgmulighed.</summary>
        public event Action<T>? Selected;

        /// <summary>Markerer den valgte værdi (null = ingen valgt).</summary>
        public void SetSelected(T? value)
        {
            foreach (var pair in _buttons)
            {
                var isSelected = value.HasValue && EqualityComparer<T>.Default.Equals(pair.Key, value.Value);
                Ui.SetClass(pair.Value, "rm-chip--selected", isSelected);
            }
        }
    }
}
