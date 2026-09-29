using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.UI
{
    /// <summary>
    /// Programmatically builds the entire combat scene at runtime: Canvas, UI hierarchy,
    /// combat grid, stick figures, and wires all component references. Attach to an
    /// empty GameObject in a minimal scene — everything else is created here.
    /// </summary>
    public class CombatBootstrapper : MonoBehaviour
    {
        [Header("Content Data")]
        [SerializeField] private EncounterSO _encounter;
        [SerializeField] private CharacterClassSO[] _partyClasses;
        [SerializeField] private string[] _partyNames;
        [SerializeField] private int _partyLevel = 1;

        [Header("Starting Items")]
        [SerializeField] private ItemSO[] _starterItems;
        [SerializeField] private int[] _starterItemQuantities;

        // Screen-space band (fractions of the view) the grid may occupy, clear of
        // the narrator (top), action panel (bottom), and HP bars (sides).
        private const float GRID_AREA_BOTTOM = 0.19f;
        private const float GRID_AREA_TOP = 0.78f;
        private const float GRID_AREA_WIDTH = 0.72f;
        private const float MAX_TILE_SIZE = 1.2f;

        // Stick figure height as a fraction of tile size, and hip offset so the
        // figure (legs below hip, head above shoulder) looks centered on its tile.
        private const float FIGURE_TILE_RATIO = 0.65f;
        private const float FIGURE_HIP_OFFSET_RATIO = 0.1f;
        private const float MOVE_ANIMATION_SECONDS = 0.25f;

        private CombatStateMachine _stateMachine;
        private CombatUIController _uiController;
        private GridRenderer _gridRenderer;
        private Camera _mainCamera;

        private void Start()
        {
            BuildScene();
        }

        private void BuildScene()
        {
            SetupCamera();
            CreateCombatStateMachine();
            var canvas = CreateCanvas();
            CreateUIHierarchy(canvas);
            StartCombat();
        }

        // ================================================================
        // Camera
        // ================================================================

        private void SetupCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _mainCamera = camGo.AddComponent<Camera>();
                _mainCamera.orthographic = true;
                _mainCamera.orthographicSize = 5f;
                _mainCamera.backgroundColor = new Color(0.95f, 0.93f, 0.88f); // Notebook paper
                _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                camGo.AddComponent<AudioListener>();
            }

            _mainCamera.transform.position = new Vector3(0, 0, -10);
        }

        // ================================================================
        // Combat State Machine
        // ================================================================

        private void CreateCombatStateMachine()
        {
            var go = new GameObject("CombatStateMachine");
            _stateMachine = go.AddComponent<CombatStateMachine>();
        }

        // ================================================================
        // Canvas & UI
        // ================================================================

        private Canvas CreateCanvas()
        {
            var canvasGo = new GameObject("CombatCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem for button clicks
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvas;
        }

        private void CreateUIHierarchy(Canvas canvas)
        {
            var canvasTransform = canvas.transform;

            // Narrator — top of screen
            var narratorDisplay = CreateNarratorDisplay(canvasTransform);

            // Turn Order — top-right
            var turnOrderDisplay = CreateTurnOrderDisplay(canvasTransform);

            // Action Panel — bottom-center
            var actionPanel = CreateActionPanel(canvasTransform);

            // Target Panel — bottom-center (overlaps action panel, only one visible at a time)
            var targetPanel = CreateTargetPanel(canvasTransform);

            // Movement Panel — bottom-center (shown only during the movement phase)
            var movementPanel = CreateMovementPanel(canvasTransform);

            // Result Panel — center overlay
            var resultPanel = CreateResultPanel(canvasTransform);

            // Grid — world-space tiles behind the stick figures
            _gridRenderer = new GameObject("CombatGrid").AddComponent<GridRenderer>();

            // HP Bar containers — left side (players) and right side (enemies)
            var playerHPContainer = CreatePanel(canvasTransform, "PlayerHPBars",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, -60),
                new Vector2(220, 300));
            var enemyHPContainer = CreatePanel(canvasTransform, "EnemyHPBars",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-230, -60),
                new Vector2(220, 300));

            // Popup canvas (same canvas)
            var popupContainer = CreatePanel(canvasTransform, "PopupCanvas",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            popupContainer.anchorMin = Vector2.zero;
            popupContainer.anchorMax = Vector2.one;
            popupContainer.offsetMin = Vector2.zero;
            popupContainer.offsetMax = Vector2.zero;

            // Create prefabs (template GameObjects stored inactive)
            var hpBarPrefab = CreateHPBarPrefab();
            var damagePopupPrefab = CreateDamagePopupPrefab();
            var abilityButtonPrefab = CreateButtonPrefab("AbilityButtonPrefab");
            var targetButtonPrefab = CreateButtonPrefab("TargetButtonPrefab");
            var turnEntryPrefab = CreateTurnEntryPrefab();

            // Wire ActionPanel
            SetField(actionPanel, "_buttonContainer", actionPanel.transform.Find("ButtonContainer"));
            SetField(actionPanel, "_abilityButtonPrefab", abilityButtonPrefab);
            SetField(actionPanel, "_defendButton", actionPanel.transform.Find("DefendButton").GetComponent<Button>());

            // Wire TargetPanel
            SetField(targetPanel, "_buttonContainer", targetPanel.transform.Find("ButtonContainer"));
            SetField(targetPanel, "_targetButtonPrefab", targetButtonPrefab);
            SetField(targetPanel, "_cancelButton", targetPanel.transform.Find("CancelButton").GetComponent<Button>());

            // Wire TurnOrderDisplay
            SetField(turnOrderDisplay, "_entryContainer", turnOrderDisplay.transform.Find("EntryContainer"));
            SetField(turnOrderDisplay, "_entryPrefab", turnEntryPrefab);

            // Wire NarratorDisplay
            SetField(narratorDisplay, "_narratorText", narratorDisplay.GetComponentInChildren<Text>());
            SetField(narratorDisplay, "_canvasGroup", narratorDisplay.GetComponent<CanvasGroup>());

            // Wire MovementPanel
            SetField(movementPanel, "_promptText", movementPanel.transform.Find("Prompt").GetComponent<Text>());
            SetField(movementPanel, "_skipButton", movementPanel.transform.Find("SkipButton").GetComponent<Button>());

            // Wire CombatResultPanel
            SetField(resultPanel, "_titleText", resultPanel.transform.Find("Title").GetComponent<Text>());
            SetField(resultPanel, "_subtitleText", resultPanel.transform.Find("Subtitle").GetComponent<Text>());
            SetField(resultPanel, "_backgroundImage", resultPanel.GetComponent<Image>());

            // Create and wire CombatUIController
            // IMPORTANT: Disable the GameObject BEFORE adding the component so that
            // OnEnable does not fire until all [SerializeField] references are set.
            var uiControllerGo = new GameObject("CombatUIController");
            uiControllerGo.SetActive(false);
            _uiController = uiControllerGo.AddComponent<CombatUIController>();

            SetField(_uiController, "_combatStateMachine", _stateMachine);
            SetField(_uiController, "_narratorDisplay", narratorDisplay);
            SetField(_uiController, "_turnOrderDisplay", turnOrderDisplay);
            SetField(_uiController, "_actionPanel", actionPanel);
            SetField(_uiController, "_targetPanel", targetPanel);
            SetField(_uiController, "_resultPanel", resultPanel);
            SetField(_uiController, "_movementPanel", movementPanel);
            SetField(_uiController, "_gridRenderer", _gridRenderer);
            SetField(_uiController, "_playerHPBarContainer", playerHPContainer);
            SetField(_uiController, "_enemyHPBarContainer", enemyHPContainer);
            SetField(_uiController, "_hpBarPrefab", hpBarPrefab);
            SetField(_uiController, "_damagePopupPrefab", damagePopupPrefab);
            SetField(_uiController, "_popupCanvas", canvas);
            SetField(_uiController, "_mainCamera", _mainCamera);

            // Now that all fields are wired, enable the GameObject so OnEnable subscribes to events
            uiControllerGo.SetActive(true);
        }

        // ================================================================
        // Start Combat
        // ================================================================

        private void StartCombat()
        {
            if (_encounter == null || _partyClasses == null || _partyClasses.Length == 0)
            {
                Debug.LogError("CombatBootstrapper: Missing encounter or party class data!");
                return;
            }

            // Build the full party
            var playerUnits = new List<CombatUnit>();
            for (int i = 0; i < _partyClasses.Length; i++)
            {
                string name = (_partyNames != null && i < _partyNames.Length)
                    ? _partyNames[i]
                    : _partyClasses[i].ClassName;
                playerUnits.Add(CombatUnit.FromPlayerCharacter(name, _partyClasses[i], _partyLevel));
            }

            // Set up party inventory
            var inventory = new Inventory();
            if (_starterItems != null)
            {
                for (int i = 0; i < _starterItems.Length; i++)
                {
                    if (_starterItems[i] == null) continue;
                    int qty = (_starterItemQuantities != null && i < _starterItemQuantities.Length)
                        ? _starterItemQuantities[i] : 1;
                    inventory.Add(_starterItems[i], qty);
                }
            }
            _stateMachine.SetInventory(inventory);

            // Subscribe to stick figure events
            _stateMachine.OnCombatStarted += () =>
            {
                BuildGrid();
                SpawnStickFigures();
            };
            _stateMachine.OnUnitMoved += HandleUnitMoved;
            _stateMachine.OnAttackResolved += HandleAttackAnimation;
            _stateMachine.OnUnitDied += HandleDeathAnimation;
            _stateMachine.OnTurnStarted += HandleTurnAnimation;

            // Start combat (this triggers OnCombatStarted, which triggers UI setup)
            _stateMachine.StartCombat(playerUnits, _encounter);
        }

        // ================================================================
        // Stick Figure Management
        // ================================================================

        private readonly Dictionary<CombatUnit, StickFigureRenderer> _unitFigureMap
            = new Dictionary<CombatUnit, StickFigureRenderer>();

        /// <summary>
        /// Size the grid to fit the free screen area and draw its tiles. Runs on
        /// OnCombatStarted, after the state machine has built the grid and placed units.
        /// </summary>
        private void BuildGrid()
        {
            var grid = _stateMachine.Grid;
            float viewHeight = _mainCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * _mainCamera.aspect;
            float viewBottom = _mainCamera.transform.position.y - _mainCamera.orthographicSize;

            float areaHeight = (GRID_AREA_TOP - GRID_AREA_BOTTOM) * viewHeight;
            float areaWidth = GRID_AREA_WIDTH * viewWidth;
            float centerY = viewBottom + (GRID_AREA_BOTTOM + GRID_AREA_TOP) / 2f * viewHeight;

            float tileSize = GridCoordinateMapper.FitTileSize(grid.Width, grid.Height,
                areaWidth, areaHeight, MAX_TILE_SIZE);
            var mapper = new GridCoordinateMapper(grid.Width, grid.Height, tileSize,
                new Vector3(_mainCamera.transform.position.x, centerY, 0f));

            _gridRenderer.Build(grid, mapper, _mainCamera);
        }

        private void SpawnStickFigures()
        {
            // Players face right, enemies face left
            foreach (var unit in _stateMachine.PlayerUnits)
            {
                SpawnFigure(unit, GetFigurePosition(unit.Position), true, GetFigureHeight());
            }
            foreach (var unit in _stateMachine.EnemyUnits)
            {
                SpawnFigure(unit, GetFigurePosition(unit.Position), false, GetFigureHeight());
            }
        }

        private float GetFigureHeight()
        {
            return _gridRenderer.Mapper.TileSize * FIGURE_TILE_RATIO;
        }

        private Vector3 GetFigurePosition(GridPosition tile)
        {
            return _gridRenderer.GetWorldPosition(tile)
                + Vector3.down * (GetFigureHeight() * FIGURE_HIP_OFFSET_RATIO);
        }

        private void HandleUnitMoved(CombatUnit unit, GridPosition from, GridPosition to)
        {
            if (_unitFigureMap.TryGetValue(unit, out var figure))
            {
                StartCoroutine(AnimateMove(figure.transform, GetFigurePosition(to)));
            }
        }

        private static IEnumerator AnimateMove(Transform figure, Vector3 destination)
        {
            Vector3 start = figure.position;
            float elapsed = 0f;
            while (elapsed < MOVE_ANIMATION_SECONDS)
            {
                elapsed += Time.deltaTime;
                figure.position = Vector3.Lerp(start, destination, elapsed / MOVE_ANIMATION_SECONDS);
                yield return null;
            }
            figure.position = destination;
        }

        private void SpawnFigure(CombatUnit unit, Vector3 position, bool facingRight, float height)
        {
            var go = new GameObject($"StickFigure_{unit.Name}");
            go.transform.position = position;
            var renderer = go.AddComponent<StickFigureRenderer>();
            renderer.Bind(unit, facingRight, height);
            _unitFigureMap[unit] = renderer;
        }

        private void HandleAttackAnimation(CombatUnit attacker, CombatUnit target, AttackResult result)
        {
            if (_unitFigureMap.TryGetValue(attacker, out var attackerFigure))
            {
                attackerFigure.SetPose(StickFigureRenderer.StickPose.Attack);
            }
            if (result.IsHit && _unitFigureMap.TryGetValue(target, out var targetFigure))
            {
                targetFigure.SetPose(StickFigureRenderer.StickPose.Hit);
            }
        }

        private void HandleDeathAnimation(CombatUnit unit)
        {
            if (_unitFigureMap.TryGetValue(unit, out var figure))
            {
                figure.ShowDeath();
            }
        }

        private void HandleTurnAnimation(CombatUnit unit)
        {
            // Reset all figures to idle, then highlight active
            foreach (var kvp in _unitFigureMap)
            {
                if (kvp.Key.IsAlive && kvp.Key.IsConscious)
                {
                    kvp.Value.SetPose(StickFigureRenderer.StickPose.Idle);
                }
            }
        }

        // ================================================================
        // UI Factory Methods
        // ================================================================

        private NarratorDisplay CreateNarratorDisplay(Transform parent)
        {
            var go = CreateUIObject("NarratorDisplay", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.85f);
            rect.anchorMax = new Vector2(0.85f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var textGo = CreateUIObject("NarratorText", go.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 5);
            textRect.offsetMax = new Vector2(-15, -5);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.color = new Color(0.95f, 0.9f, 0.7f);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Italic;

            return go.AddComponent<NarratorDisplay>();
        }

        private TurnOrderDisplay CreateTurnOrderDisplay(Transform parent)
        {
            var go = CreateUIObject("TurnOrderDisplay", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.7f, 0.72f);
            rect.anchorMax = new Vector2(0.98f, 0.84f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.7f);

            // Label
            var labelGo = CreateUIObject("Label", go.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.7f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(5, 0);
            labelRect.offsetMax = new Vector2(-5, 0);
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "Turn Order";
            label.fontSize = 14;
            label.color = Color.gray;
            label.alignment = TextAnchor.MiddleCenter;

            // Entry container with vertical layout
            var containerGo = CreateUIObject("EntryContainer", go.transform);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0.7f);
            containerRect.offsetMin = new Vector2(5, 2);
            containerRect.offsetMax = new Vector2(-5, 0);
            var layout = containerGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 1;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return go.AddComponent<TurnOrderDisplay>();
        }

        private ActionPanel CreateActionPanel(Transform parent)
        {
            var go = CreateUIObject("ActionPanel", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.02f);
            rect.anchorMax = new Vector2(0.8f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

            // Button container with horizontal layout
            var containerGo = CreateUIObject("ButtonContainer", go.transform);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0.8f, 1);
            containerRect.offsetMin = new Vector2(10, 5);
            containerRect.offsetMax = new Vector2(-5, -5);
            var layout = containerGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Defend button
            var defendGo = CreateUIObject("DefendButton", go.transform);
            var defendRect = defendGo.GetComponent<RectTransform>();
            defendRect.anchorMin = new Vector2(0.82f, 0.15f);
            defendRect.anchorMax = new Vector2(0.98f, 0.85f);
            defendRect.offsetMin = Vector2.zero;
            defendRect.offsetMax = Vector2.zero;
            var defendImg = defendGo.AddComponent<Image>();
            defendImg.color = new Color(0.3f, 0.3f, 0.5f);
            var defendBtn = defendGo.AddComponent<Button>();
            defendBtn.targetGraphic = defendImg;
            var defendTextGo = CreateUIObject("Text", defendGo.transform);
            FillParent(defendTextGo.GetComponent<RectTransform>());
            var defendText = defendTextGo.AddComponent<Text>();
            defendText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            defendText.text = "Defend";
            defendText.fontSize = 16;
            defendText.color = Color.white;
            defendText.alignment = TextAnchor.MiddleCenter;

            go.SetActive(false);
            return go.AddComponent<ActionPanel>();
        }

        private TargetPanel CreateTargetPanel(Transform parent)
        {
            var go = CreateUIObject("TargetPanel", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.02f);
            rect.anchorMax = new Vector2(0.8f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.12f, 0.12f, 0.9f);

            // Button container
            var containerGo = CreateUIObject("ButtonContainer", go.transform);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0.8f, 1);
            containerRect.offsetMin = new Vector2(10, 5);
            containerRect.offsetMax = new Vector2(-5, -5);
            var layout = containerGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Cancel button
            var cancelGo = CreateUIObject("CancelButton", go.transform);
            var cancelRect = cancelGo.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.82f, 0.15f);
            cancelRect.anchorMax = new Vector2(0.98f, 0.85f);
            cancelRect.offsetMin = Vector2.zero;
            cancelRect.offsetMax = Vector2.zero;
            var cancelImg = cancelGo.AddComponent<Image>();
            cancelImg.color = new Color(0.5f, 0.2f, 0.2f);
            var cancelBtn = cancelGo.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelImg;
            var cancelTextGo = CreateUIObject("Text", cancelGo.transform);
            FillParent(cancelTextGo.GetComponent<RectTransform>());
            var cancelText = cancelTextGo.AddComponent<Text>();
            cancelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cancelText.text = "Cancel";
            cancelText.fontSize = 16;
            cancelText.color = Color.white;
            cancelText.alignment = TextAnchor.MiddleCenter;

            go.SetActive(false);
            return go.AddComponent<TargetPanel>();
        }

        private MovementPanel CreateMovementPanel(Transform parent)
        {
            var go = CreateUIObject("MovementPanel", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.02f);
            rect.anchorMax = new Vector2(0.8f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.22f, 0.9f);

            // Prompt text
            var promptGo = CreateUIObject("Prompt", go.transform);
            var promptRect = promptGo.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0, 0);
            promptRect.anchorMax = new Vector2(0.8f, 1);
            promptRect.offsetMin = new Vector2(15, 5);
            promptRect.offsetMax = new Vector2(-5, -5);
            var prompt = promptGo.AddComponent<Text>();
            prompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            prompt.fontSize = 18;
            prompt.color = Color.white;
            prompt.alignment = TextAnchor.MiddleLeft;

            // Skip button
            var skipGo = CreateUIObject("SkipButton", go.transform);
            var skipRect = skipGo.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.82f, 0.15f);
            skipRect.anchorMax = new Vector2(0.98f, 0.85f);
            skipRect.offsetMin = Vector2.zero;
            skipRect.offsetMax = Vector2.zero;
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(0.3f, 0.3f, 0.5f);
            var skipBtn = skipGo.AddComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            var skipTextGo = CreateUIObject("Text", skipGo.transform);
            FillParent(skipTextGo.GetComponent<RectTransform>());
            var skipText = skipTextGo.AddComponent<Text>();
            skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipText.text = "Don't Move";
            skipText.fontSize = 16;
            skipText.color = Color.white;
            skipText.alignment = TextAnchor.MiddleCenter;

            go.SetActive(false);
            return go.AddComponent<MovementPanel>();
        }

        private CombatResultPanel CreateResultPanel(Transform parent)
        {
            var go = CreateUIObject("CombatResultPanel", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.25f);
            rect.anchorMax = new Vector2(0.8f, 0.75f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            var titleGo = CreateUIObject("Title", go.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.5f);
            titleRect.anchorMax = new Vector2(1, 0.9f);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, -10);
            var title = titleGo.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 48;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;

            var subGo = CreateUIObject("Subtitle", go.transform);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0.1f);
            subRect.anchorMax = new Vector2(1, 0.5f);
            subRect.offsetMin = new Vector2(20, 0);
            subRect.offsetMax = new Vector2(-20, 0);
            var sub = subGo.AddComponent<Text>();
            sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sub.fontSize = 22;
            sub.color = new Color(0.8f, 0.8f, 0.8f);
            sub.alignment = TextAnchor.MiddleCenter;

            go.SetActive(false);
            return go.AddComponent<CombatResultPanel>();
        }

        // ================================================================
        // Prefab Factory Methods (inactive template GameObjects)
        // ================================================================

        private GameObject CreateHPBarPrefab()
        {
            var go = new GameObject("HPBarPrefab");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 40);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 1;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 4, 2, 2);

            // Background image
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.4f, 0.8f);

            // Name text
            var nameGo = CreateUIObject("NameText", go.transform);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 13;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            var nameLayout = nameGo.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 16;

            // Bar background
            var barBgGo = CreateUIObject("BarBackground", go.transform);
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f);
            var barBgLayout = barBgGo.AddComponent<LayoutElement>();
            barBgLayout.preferredHeight = 12;

            // Fill image (child of bar bg)
            var fillGo = CreateUIObject("Fill", barBgGo.transform);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.8f, 0.2f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            // HP text (overlaid on bar)
            var hpTextGo = CreateUIObject("HPText", barBgGo.transform);
            var hpTextRect = hpTextGo.GetComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;
            var hpText = hpTextGo.AddComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 11;
            hpText.color = Color.white;
            hpText.alignment = TextAnchor.MiddleCenter;

            // Add UnitHPBar component and wire its fields
            var hpBar = go.AddComponent<UnitHPBar>();
            SetField(hpBar, "_fillImage", fillImg);
            SetField(hpBar, "_nameText", nameText);
            SetField(hpBar, "_hpText", hpText);
            SetField(hpBar, "_backgroundImage", bg);

            go.SetActive(false);
            DontDestroyOnLoad(go);
            return go;
        }

        private GameObject CreateDamagePopupPrefab()
        {
            var go = new GameObject("DamagePopupPrefab");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 40);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = Color.red;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            var popup = go.AddComponent<DamagePopup>();
            SetField(popup, "_text", text);

            go.SetActive(false);
            DontDestroyOnLoad(go);
            return go;
        }

        private GameObject CreateButtonPrefab(string name)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 40);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = CreateUIObject("Text", go.transform);
            FillParent(textGo.GetComponent<RectTransform>());
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            go.SetActive(false);
            DontDestroyOnLoad(go);
            return go;
        }

        private GameObject CreateTurnEntryPrefab()
        {
            var go = new GameObject("TurnEntryPrefab");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 16);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 13;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;

            var layoutEl = go.AddComponent<LayoutElement>();
            layoutEl.preferredHeight = 16;

            go.SetActive(false);
            DontDestroyOnLoad(go);
            return go;
        }

        // ================================================================
        // Utility
        // ================================================================

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static void FillParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogWarning($"CombatBootstrapper: Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
