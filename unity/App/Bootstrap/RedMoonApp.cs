using System;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.Platform;
using RedMoon.UnityApp.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Bootstrap
{
    /// <summary>
    /// Starter Rød Måne automatisk, når du trykker Play i Unity – i hvilken som helst scene.
    /// Der skal ikke sættes noget op i scenen; alt bygges i kode.
    /// </summary>
    internal static class RedMoonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartApp()
        {
            if (RedMoonApp.Instance != null) return;
            var host = new GameObject("Rød Måne");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<RedMoonApp>();
        }
    }

    /// <summary>
    /// Appens vært: opretter services (Core), brugerfladen (UI Toolkit), navigation og privatlivsbeskyttelse.
    /// </summary>
    internal sealed class RedMoonApp : MonoBehaviour
    {
        private VisualElement? _root;
        private VisualElement? _cover;
        private Navigator? _navigator;
        private Rect _lastSafeArea;
        private bool _legacyInputAvailable = true;
        private string _lastDay = string.Empty;

        /// <summary>Den kørende app (der er kun én).</summary>
        public static RedMoonApp? Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            MainThread.Capture();

            if (Application.isMobilePlatform)
            {
                Screen.orientation = ScreenOrientation.Portrait;
                Application.targetFrameRate = 60;
            }
            PrivacyGuard.EnableSecureWindow();
        }

        private async void Start()
        {
            try
            {
                var services = new AppServices();
                _root = CreateUiRoot();

                var column = Ui.Box("rm-column");
                _root.Add(column);
                var dialogLayer = Ui.Box("rm-layer");
                dialogLayer.pickingMode = PickingMode.Ignore;
                _root.Add(dialogLayer);

                _cover = Ui.Box("rm-overlay", "rm-cover");
                _cover.Add(Ui.Label("Rød Måne", "rm-title", "rm-center"));
                Ui.SetVisible(_cover, false);
                _root.Add(_cover);

                _navigator = new Navigator(column);
                var dialogs = new Dialogs(dialogLayer);
                var context = new ScreenContext(services, _navigator, dialogs);
                _navigator.Attach(context);
                _lastDay = services.Clock.Today.ToString();

                var restored = false;
                try
                {
                    restored = await services.Accounts.TryRestoreSessionAsync();
                }
                catch (Exception ex)
                {
                    // Fx utilgængeligt nøglelager: vis login i stedet for at crashe.
                    Debug.LogWarning("Kunne ikke gendanne login: " + ex.Message);
                }

                if (restored) _navigator.ShowMain(); else _navigator.ShowLogin();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void Update()
        {
            ApplySafeArea();
            HandleBackButton();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Dækket bruges kun på telefoner (i Editoren ville det dække appen, hver gang man klikker i et andet vindue).
            if (_cover != null && Application.isMobilePlatform) Ui.SetVisible(_cover, !hasFocus);
            if (hasFocus) RefreshIfDayChanged();
        }

        private void OnApplicationPause(bool paused)
        {
            if (_cover != null && Application.isMobilePlatform) Ui.SetVisible(_cover, paused);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Opretter UI Toolkit-panelet i kode (ingen scene- eller asset-opsætning nødvendig).</summary>
        private VisualElement CreateUiRoot()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "RedMoon.PanelSettings";
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("RedMoon/RedMoonTheme");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(430, 932);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 1f; // Skalér efter højden: ens størrelse på telefoner, centreret kolonne på brede skærme.
            settings.clearColor = true;
            settings.colorClearValue = Palette.Background;

            var uiObject = new GameObject("UI");
            uiObject.SetActive(false);
            uiObject.transform.SetParent(transform, false);
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            uiObject.SetActive(true);

            var root = document.rootVisualElement;
            var styles = Resources.Load<StyleSheet>("RedMoon/RedMoonStyles");
            if (styles != null) root.styleSheets.Add(styles);
            else Debug.LogError("RedMoonStyles.uss blev ikke fundet i Resources/RedMoon.");
            root.AddToClassList("rm-root");
            return root;
        }

        /// <summary>Holder indholdet væk fra kameraets "hak" og bundlinjen på telefoner.</summary>
        private void ApplySafeArea()
        {
            if (_root == null) return;
            var safe = Screen.safeArea;
            var height = _root.layout.height;
            if (safe == _lastSafeArea || float.IsNaN(height) || height <= 0) return;
            _lastSafeArea = safe;

            var scale = Screen.height / height;
            _root.style.paddingTop = (Screen.height - safe.yMax) / scale;
            _root.style.paddingBottom = safe.yMin / scale;
            _root.style.paddingLeft = safe.xMin / scale;
            _root.style.paddingRight = (Screen.width - safe.xMax) / scale;
        }

        /// <summary>Androids tilbage-knap (og Esc i Editor) lukker en åben underside.</summary>
        private void HandleBackButton()
        {
            if (!_legacyInputAvailable || _navigator == null) return;
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape) && _navigator.CanGoBack) _navigator.Back();
            }
            catch (InvalidOperationException)
            {
                // Projektet bruger kun det nye Input System; tilbage-knappen håndteres så ikke her.
                _legacyInputAvailable = false;
            }
        }

        private void RefreshIfDayChanged()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_navigator == null || today == _lastDay) return;
            _lastDay = today;
            _navigator.RefreshVisible();
        }
    }
}
