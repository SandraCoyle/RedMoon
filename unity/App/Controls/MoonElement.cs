using RedMoon.UnityApp.Graphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Controls
{
    /// <summary>
    /// Viser månen + ringen af cyklusdage (tegnet af MoonRenderer).
    /// Tegnes kun igen når tallene ændrer sig; den gamle tekstur frigives.
    /// </summary>
    internal sealed class MoonElement : VisualElement
    {
        private const int TextureSize = 768;
        private Texture2D? _texture;
        private (int Cycle, int Period, int Day) _drawn = (-1, -1, -1);

        public MoonElement(float size)
        {
            AddToClassList("rm-moon");
            style.width = size;
            style.height = size;
            style.flexShrink = 0;
            pickingMode = PickingMode.Ignore;
            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseTexture());
        }

        /// <summary>Opdaterer månen. <paramref name="dayOfCycle"/> 0 = ukendt.</summary>
        public void SetCycle(int cycleLength, int periodLength, int dayOfCycle)
        {
            var wanted = (cycleLength, periodLength, dayOfCycle);
            if (_texture != null && wanted == _drawn) return;

            ReleaseTexture();
            _texture = MoonRenderer.RenderCycle(TextureSize, cycleLength, periodLength, dayOfCycle);
            _drawn = wanted;
            style.backgroundImage = new StyleBackground(_texture);
        }

        private void ReleaseTexture()
        {
            if (_texture == null) return;
            style.backgroundImage = StyleKeyword.None;
            Object.Destroy(_texture);
            _texture = null;
            _drawn = (-1, -1, -1);
        }
    }
}
