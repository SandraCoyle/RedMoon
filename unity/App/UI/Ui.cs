using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.UI
{
    /// <summary>
    /// Genbrugelige byggeklodser til brugerfladen. Udseendet styres af CSS-klasserne i
    /// Resources/RedMoon/RedMoonStyles.uss, så skærmene kun beskriver indhold og struktur.
    /// </summary>
    internal static class Ui
    {
        public static Label Label(string text, params string[] classes)
        {
            var label = new Label(text);
            AddClasses(label, classes);
            return label;
        }

        /// <summary>Knap. Standard er den store, lyserøde knap; tilføj fx "rm-btn--secondary" for andre varianter.</summary>
        public static Button Button(string text, Action onClick, params string[] classes)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("rm-btn");
            AddClasses(button, classes);
            return button;
        }

        public static VisualElement Box(params string[] classes)
        {
            var box = new VisualElement();
            AddClasses(box, classes);
            return box;
        }

        /// <summary>Et tegnet ikon (Texture2D) i fast størrelse.</summary>
        public static Image Icon(Texture2D texture, float size)
        {
            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.width = size;
            image.style.height = size;
            return image;
        }

        /// <summary>Tekstfelt med overskrift ovenover (Unity 2021 har ikke pladsholdertekst).</summary>
        public static TextField TextField(VisualElement parent, string caption, int maxLength)
        {
            parent.Add(Label(caption, "rm-field-caption"));
            var field = new TextField { maxLength = maxLength };
            field.AddToClassList("rm-field");
            parent.Add(field);
            return field;
        }

        public static VisualElement Spacer(float height)
        {
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.style.height = height;
            spacer.style.flexShrink = 0;
            return spacer;
        }

        public static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (enabled) element.AddToClassList(className); else element.RemoveFromClassList(className);
        }

        private static void AddClasses(VisualElement element, string[] classes)
        {
            foreach (var name in classes)
            {
                if (!string.IsNullOrEmpty(name)) element.AddToClassList(name);
            }
        }
    }
}
