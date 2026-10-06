using System;
using System.Collections.Generic;
using RedMoon.Core.Security;
using RedMoon.UnityApp.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Controls
{
    /// <summary>
    /// Mønsterlås: 3 x 3 punkter (1-9) som brugeren forbinder ved at trække fingeren (eller musen i Unity Editor).
    /// Reglerne for hvilke punkter der rammes, ligger i Core (<see cref="PatternGrid"/>), så de er de samme som i mobilappen.
    /// Mønsteret ryddes automatisk kort efter, så det ikke bliver stående synligt.
    /// </summary>
    internal sealed class PatternLockElement : VisualElement
    {
        private const float Side = 280f;
        private const float LineThickness = 7f;

        private readonly VisualElement _lines;
        private readonly VisualElement[] _dots = new VisualElement[9];
        private readonly List<int> _points = new List<int>();
        private Vector2? _finger;
        private int _pointerId = -1;
        private bool _error;
        private IVisualElementScheduledItem? _clearTimer;

        public PatternLockElement()
        {
            AddToClassList("rm-pattern");
            style.width = Side;
            style.height = Side;
            style.flexShrink = 0;

            _lines = new VisualElement { pickingMode = PickingMode.Ignore };
            _lines.style.position = Position.Absolute;
            _lines.style.left = 0;
            _lines.style.top = 0;
            _lines.style.right = 0;
            _lines.style.bottom = 0;
            Add(_lines);

            var dotSize = Side / PatternGrid.Size * 0.36f;
            for (var point = 1; point <= 9; point++)
            {
                var (cx, cy) = PatternGrid.Center(point, Side);
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("rm-pattern-dot");
                dot.style.position = Position.Absolute;
                dot.style.left = cx - dotSize / 2;
                dot.style.top = cy - dotSize / 2;
                dot.style.width = dotSize;
                dot.style.height = dotSize;
                SetRadius(dot, dotSize / 2);

                var number = new Label(point.ToString()) { pickingMode = PickingMode.Ignore };
                number.AddToClassList("rm-pattern-number");
                dot.Add(number);

                _dots[point - 1] = dot;
                Add(dot);
            }

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => Cancel());
            Redraw();
        }

        /// <summary>Udløses med punkterne (1-9) når fingeren løftes.</summary>
        public event Action<IReadOnlyList<int>>? Completed;

        /// <summary>Viser det tegnede mønster i fejlfarve (fx efter forkert login).</summary>
        public void ShowError()
        {
            _error = true;
            Redraw();
        }

        /// <summary>Rydder mønsteret med det samme.</summary>
        public void ResetPattern()
        {
            _clearTimer?.Pause();
            _points.Clear();
            _finger = null;
            _error = false;
            Redraw();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_pointerId != -1) return;
            ResetPattern();
            _pointerId = evt.pointerId;
            this.CapturePointer(evt.pointerId);

            var p = this.WorldToLocal(evt.position);
            _finger = p;
            PatternGrid.AddPointsAlong(_points, p.x, p.y, p.x, p.y, Side);
            Redraw();
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId || !_finger.HasValue) return;
            var from = _finger.Value;
            var to = (Vector2)this.WorldToLocal(evt.position);
            PatternGrid.AddPointsAlong(_points, from.x, from.y, to.x, to.y, Side);
            _finger = to;
            Redraw();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId) return;
            var id = _pointerId;
            _pointerId = -1;
            this.ReleasePointer(id);
            _finger = null;
            Redraw();
            evt.StopPropagation();

            var result = _points.ToArray();
            if (result.Length > 0) Completed?.Invoke(result);
            _clearTimer = schedule.Execute(ResetPattern).StartingIn(700);
        }

        private void Cancel()
        {
            if (_pointerId == -1) return;
            _pointerId = -1;
            ResetPattern();
        }

        private void Redraw()
        {
            var color = _error ? Palette.Error : Palette.Accent;

            for (var i = 0; i < _dots.Length; i++)
            {
                var selected = _points.Contains(i + 1);
                var dot = _dots[i];
                dot.style.backgroundColor = selected ? color : Palette.SurfaceAlt;
                var border = selected ? color : Palette.Border;
                dot.style.borderTopColor = border;
                dot.style.borderBottomColor = border;
                dot.style.borderLeftColor = border;
                dot.style.borderRightColor = border;
                Ui.SetClass(dot, "rm-pattern-dot--selected", selected);
            }

            _lines.Clear();
            var lineColor = new Color(color.r, color.g, color.b, 0.8f);
            for (var i = 1; i < _points.Count; i++)
            {
                _lines.Add(Line(CenterOf(_points[i - 1]), CenterOf(_points[i]), lineColor));
            }
            if (_finger.HasValue && _points.Count > 0)
            {
                _lines.Add(Line(CenterOf(_points[_points.Count - 1]), _finger.Value, lineColor));
            }
        }

        private static Vector2 CenterOf(int point)
        {
            var (x, y) = PatternGrid.Center(point, Side);
            return new Vector2(x, y);
        }

        /// <summary>En streg som et smalt, roteret rektangel fra a til b.</summary>
        private static VisualElement Line(Vector2 a, Vector2 b, Color color)
        {
            var delta = b - a;
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.style.position = Position.Absolute;
            line.style.left = a.x;
            line.style.top = a.y - LineThickness / 2;
            line.style.width = delta.magnitude;
            line.style.height = LineThickness;
            line.style.backgroundColor = color;
            SetRadius(line, LineThickness / 2);
            line.style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
            line.style.rotate = new Rotate(new Angle(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, AngleUnit.Degree));
            return line;
        }

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
