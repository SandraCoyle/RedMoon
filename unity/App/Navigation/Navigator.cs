using System;
using System.Collections.Generic;
using RedMoon.UnityApp.Graphics;
using RedMoon.UnityApp.Screens;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Navigation
{
    /// <summary>
    /// Styrer hvilken skærm der vises.
    /// - Logget ud: login-skærmen er "bunden"; opret bruger/import lægges ovenpå.
    /// - Logget ind: den valgte fane i bundmenuen er "bunden"; undersider (fx "Skift mønster") lægges ovenpå.
    /// Undersider får en toplinje med "Tilbage"; bundmenuen vises kun når ingen underside er åben.
    /// </summary>
    internal sealed class Navigator
    {
        private readonly VisualElement _host;
        private readonly VisualElement _header;
        private readonly Label _headerTitle;
        private readonly VisualElement _content;
        private readonly VisualElement _tabBar;
        private readonly List<ScreenBase> _stack = new List<ScreenBase>();
        private readonly Dictionary<string, ScreenBase> _tabScreens = new Dictionary<string, ScreenBase>();
        private readonly Dictionary<string, Button> _tabButtons = new Dictionary<string, Button>();
        private ScreenContext? _context;
        private ScreenBase? _base;
        private ScreenBase? _visible;
        private string _currentTab = TabRegistry.Home;
        private bool _loggedIn;

        public Navigator(VisualElement host)
        {
            _host = host;

            _header = Ui.Box("rm-header");
            _header.Add(Ui.Button("‹ Tilbage", Back, "rm-btn--link", "rm-header-back"));
            _headerTitle = Ui.Label(string.Empty, "rm-header-title");
            _header.Add(_headerTitle);

            _content = Ui.Box("rm-content");
            _tabBar = Ui.Box("rm-tabbar");

            _host.Add(_header);
            _host.Add(_content);
            _host.Add(_tabBar);
        }

        /// <summary>Skal kaldes én gang, når ScreenContext er oprettet.</summary>
        public void Attach(ScreenContext context)
        {
            _context = context;
            BuildTabBar();
        }

        /// <summary>Viser login (og glemmer alle skærme fra den indloggede del).</summary>
        public void ShowLogin()
        {
            _loggedIn = false;
            ClearMain();
            _base = new LoginScreen(Context);
            Render();
        }

        /// <summary>Viser hovedappen med bundmenu, startende på "Hjem".</summary>
        public void ShowMain()
        {
            _loggedIn = true;
            ClearMain();
            _currentTab = TabRegistry.Home;
            _base = GetTabScreen(_currentTab);
            Render();
        }

        /// <summary>Åbner en underside oven på den aktuelle skærm.</summary>
        public void Push(ScreenBase screen)
        {
            _stack.Add(screen ?? throw new ArgumentNullException(nameof(screen)));
            Render();
        }

        /// <summary>Lukker den øverste underside. Gør intet hvis der ingen er.</summary>
        public void Back()
        {
            if (_stack.Count == 0) return;
            _stack.RemoveAt(_stack.Count - 1);
            Render();
        }

        /// <summary>Sand hvis der er en underside, der kan lukkes (bruges af Androids tilbage-knap).</summary>
        public bool CanGoBack => _stack.Count > 0;

        /// <summary>Skifter fane i bundmenuen (kun når brugeren er logget ind).</summary>
        public void GoToTab(string id)
        {
            if (!_loggedIn) return;
            _stack.Clear();
            _currentTab = id;
            _base = GetTabScreen(id);
            Render();
        }

        /// <summary>Opdaterer den synlige skærm (fx når datoen er skiftet mens appen var i baggrunden).</summary>
        public void RefreshVisible() => _visible?.OnShow();

        private ScreenContext Context => _context ?? throw new InvalidOperationException("Navigator er ikke koblet til ScreenContext.");

        private void ClearMain()
        {
            _visible?.OnHide();
            _visible = null;
            _stack.Clear();
            _tabScreens.Clear();
            _content.Clear();
        }

        private ScreenBase GetTabScreen(string id)
        {
            if (_tabScreens.TryGetValue(id, out var screen)) return screen;
            foreach (var tab in TabRegistry.Tabs)
            {
                if (tab.Id != id) continue;
                screen = tab.Create(Context);
                _tabScreens[id] = screen;
                return screen;
            }
            throw new ArgumentException("Ukendt fane: " + id, nameof(id));
        }

        private void Render()
        {
            var top = _stack.Count > 0 ? _stack[_stack.Count - 1] : _base;
            if (top == null) return;

            if (!ReferenceEquals(top, _visible))
            {
                _visible?.OnHide();
                _content.Clear();
                _content.Add(top.Root);
                _visible = top;
                top.OnShow();
            }

            var isSubpage = _stack.Count > 0;
            Ui.SetVisible(_header, isSubpage);
            _headerTitle.text = isSubpage ? top.Title : string.Empty;
            Ui.SetVisible(_tabBar, _loggedIn && !isSubpage);

            foreach (var pair in _tabButtons)
            {
                Ui.SetClass(pair.Value, "rm-tab--selected", pair.Key == _currentTab);
            }
            UpdateTabIcons();
        }

        private void BuildTabBar()
        {
            _tabBar.Clear();
            _tabButtons.Clear();
            foreach (var tab in TabRegistry.Tabs)
            {
                var id = tab.Id;
                var button = new Button(() => GoToTab(id)) { text = string.Empty };
                button.AddToClassList("rm-tab");
                var icon = Ui.Icon(Icons.Tab(tab.Icon, Palette.TextSecondary), 26);
                icon.name = "icon";
                button.Add(icon);
                button.Add(Ui.Label(tab.Title, "rm-tab-label"));
                _tabBar.Add(button);
                _tabButtons[id] = button;
            }
        }

        private void UpdateTabIcons()
        {
            foreach (var tab in TabRegistry.Tabs)
            {
                if (!_tabButtons.TryGetValue(tab.Id, out var button)) continue;
                var icon = button.Q<Image>("icon");
                if (icon == null) continue;
                icon.image = Icons.Tab(tab.Icon, tab.Id == _currentTab ? Palette.Accent : Palette.TextSecondary);
            }
        }
    }
}
