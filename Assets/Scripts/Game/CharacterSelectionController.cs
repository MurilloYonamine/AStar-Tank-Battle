using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Shows character families, skins and class availability in an isolated rotating 3D preview.
/// The existing name/menu/backend flow is retained by BattleSessionController.
/// CharacterId is the stable gameplay family, separate from display name and prefab nickname;
/// skins share a class and the enemy roster excludes the whole selected family.
/// UI structure/styles are authored in UXML/USS; disposable model/RenderTexture previews are
/// created at runtime without colliders. VoiceClip supplies selection and optional kill feedback.
/// </summary>
public sealed class CharacterSelectionController : MonoBehaviour
{
    [Serializable]
    public sealed class CharacterOption
    {
        public string DisplayName;
        [Tooltip("Stable character identity shared by skins; not a prefab or file name.")]
        public string CharacterId;
        public string AppearanceName;
        public GameObject ModelPrefab;
        [Tooltip("Independent class design asset. Does not belong inside the visual prefab.")]
        public CharacterClassDefinition ClassDefinition;
        [Tooltip("Spoken cue played when this character is shown in selection, not during combat or results.")]
        public AudioClip VoiceClip;
    }

    [SerializeField] private UIDocument document;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private CharacterOption[] characters;
    [SerializeField] private Transform previewRoot;
    [SerializeField] private Camera previewCamera;
    [SerializeField, Min(1f)] private float rotationSpeed = 15f;
    [Tooltip("Raises only the character in the selection preview, not the tank or battle actor.")]
    [SerializeField, Min(0f)] private float selectionCharacterHeightOffset = 0.15f;

    private VisualElement screen;
    private VisualElement infoScreen;
    private Button infoContinueButton;
    private Button infoBackButton;
    private bool showingInstructions;
    private VisualElement previewImage;
    private Label nameLabel;
    private Label indexLabel;
    private VisualElement classDetails;
    private Label classNameLabel;
    private Label classDescriptionLabel;
    private Label classAttributesLabel;
    private VisualElement healthStars, speedStars, damageStars, cadenceStars;
    private Label passiveLabel;
    private Label ultimateLabel;
    private Label classNoticeLabel;
    private Button previousButton;
    private Button nextButton;
    private Button confirmButton;
    private Button backButton;
    private GameObject previewModel;
    private RenderTexture previewTexture;
    private Color previewBackground;
    private CameraClearFlags previewClearFlags;
    private int selectedIndex;
    private bool showingResult;

    public bool IsOpen { get; private set; }
    public int CharacterCount => characters == null ? 0 : characters.Length;
    public event Action<int> Selected;
    public event Action Cancelled;

    public CharacterOption GetCharacter(int index) => characters[index];

    public string GetCharacterIdentity(int index)
    {
        CharacterOption option = characters[index];
        return !string.IsNullOrEmpty(option.CharacterId) ? option.CharacterId :
            option.ModelPrefab != null ? option.ModelPrefab.GetInstanceID().ToString() : index.ToString();
    }

    /// <summary>Distinct character families, not just distinct skins; never the player's family.</summary>
    public List<int> CreateEnemyRoster(int playerIndex, int count)
    {
        count = Mathf.Max(0, count);
        List<int> candidates = new();
        string playerIdentity = GetCharacterIdentity(playerIndex);
        for (int i = 0; i < CharacterCount; i++)
            if (characters[i].ModelPrefab != null &&
                !string.Equals(GetCharacterIdentity(i), playerIdentity, StringComparison.OrdinalIgnoreCase))
                candidates.Add(i);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swap = UnityEngine.Random.Range(0, i + 1);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
        }
        HashSet<string> families = new(StringComparer.OrdinalIgnoreCase);
        List<int> roster = new();
        foreach (int index in candidates)
            if (families.Add(GetCharacterIdentity(index))) roster.Add(index);
        if (roster.Count > count) roster.RemoveRange(count, roster.Count - count);
        return roster;
    }

    /// <summary>
    /// Opens the authored pre-game rules/data notice first; only ContinueToSelection exposes
    /// the carousel. No attempt, timer or save begins until the session's combat boundary.
    /// </summary>
    public void Open()
    {
        EnsureUI();
        SetControlsEnabled(true);
        IsOpen = true;
        showingInstructions = true;
        document.rootVisualElement.pickingMode = PickingMode.Position;
        screen.style.display = DisplayStyle.None;
        infoScreen.style.display = DisplayStyle.Flex;
        previewCamera.enabled = false;
        infoContinueButton.Focus();
    }

    private void ContinueToSelection()
    {
        if (!IsOpen || !showingInstructions) return;
        BattleAudioController.PlayButtonClick();
        showingInstructions = false;
        infoScreen.style.display = DisplayStyle.None;
        screen.style.display = DisplayStyle.Flex;
        previewCamera.enabled = true;
        ShowCharacter(selectedIndex);
        BattleAudioController.PlayCharacterVoice(characters[selectedIndex].VoiceClip);
        confirmButton.Focus();
    }

    public void Close()
    {
        showingResult = false;
        BattleAudioController.StopCharacterVoice();
        IsOpen = false;
        showingInstructions = false;
        if (infoScreen != null) infoScreen.style.display = DisplayStyle.None;
        if (screen != null) screen.style.display = DisplayStyle.None;
        if (document != null) document.rootVisualElement.pickingMode = PickingMode.Ignore;
        if (previewCamera != null) previewCamera.enabled = false;
        if (previewModel != null) previewModel.SetActive(false);
    }

    /// <summary>
    /// Shows a character-only preview: victory dances, loss uses Death, draw uses Idle.
    /// It shares appearance configuration, not the battle actor's HP, collisions or lifecycle.
    /// </summary>
    public void ShowResultPreview(int index, VisualElement resultImage, string outcome = "vitoria")
    {
        if (resultImage == null || CharacterCount == 0) return;
        EnsureUI();
        Close();
        // A separate, collision-free preview reacts. The battle actor is never revived.
        ShowCharacter(Mathf.Clamp(index, 0, CharacterCount - 1), true);
        previewModel.GetComponentInChildren<TankCharacterAnimation>(true)?.PlayResult(outcome);
        showingResult = true;
        resultImage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(previewTexture));
        previewCamera.enabled = true;
    }

    public void StepSelection(int direction)
    {
        if (!IsOpen || showingInstructions || CharacterCount == 0) return;
        selectedIndex = (selectedIndex + direction % CharacterCount + CharacterCount) % CharacterCount;
        ShowCharacter(selectedIndex);
        BattleAudioController.PlaySelectionChange();
        BattleAudioController.PlayCharacterVoice(characters[selectedIndex].VoiceClip);
    }

    public void ConfirmSelection()
    {
        if (!IsOpen || showingInstructions) return;
        BattleAudioController.StopCharacterVoice();
        BattleAudioController.PlayButtonClick();
        // Keep the visual preview visible until the session fade covers it.
        IsOpen = false;
        SetControlsEnabled(false);
        if (Selected != null) Selected.Invoke(selectedIndex);
        else Close();
    }

    private void SetControlsEnabled(bool enabled)
    {
        previousButton.SetEnabled(enabled);
        nextButton.SetEnabled(enabled);
        confirmButton.SetEnabled(enabled);
        backButton.SetEnabled(enabled);
    }

    public void CancelSelection()
    {
        if (!IsOpen) return;
        BattleAudioController.PlayButtonClick();
        Close();
        Cancelled?.Invoke();
    }

    private void Start() => Close();

    private void Update()
    {
        if (!IsOpen) return;
        // PauseManager stops combat, not the cosmetic preview or menu navigation.
        if (previewModel != null)
            previewModel.transform.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime, Space.World);

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (showingInstructions)
        {
            if (keyboard.escapeKey.wasPressedThisFrame) CancelSelection();
            return;
        }
        if (keyboard.leftArrowKey.wasPressedThisFrame) StepSelection(-1);
        else if (keyboard.rightArrowKey.wasPressedThisFrame) StepSelection(1);
        // UI Toolkit handles Enter/Space on focused buttons, avoiding a duplicate submit here.
        else if (keyboard.escapeKey.wasPressedThisFrame) CancelSelection();
    }

    private void EnsureUI()
    {
        if (screen != null) return;
        var root = document.rootVisualElement;
        if (styleSheet != null && !root.styleSheets.Contains(styleSheet)) root.styleSheets.Add(styleSheet);
        screen = root.Q<VisualElement>("CharacterSelection");
        infoScreen = root.Q<VisualElement>("GameInfoScreen");
        infoContinueButton = root.Q<Button>("GameInfoContinueButton");
        infoBackButton = root.Q<Button>("GameInfoBackButton");
        previewImage = root.Q<VisualElement>("CharacterPreview");
        nameLabel = root.Q<Label>("CharacterName");
        indexLabel = root.Q<Label>("CharacterIndex");
        classDetails = root.Q<VisualElement>("CharacterClassDetails");
        classNameLabel = root.Q<Label>("ClassNameLabel");
        classDescriptionLabel = root.Q<Label>("ClassDescriptionLabel");
        classAttributesLabel = root.Q<Label>("ClassAttributesLabel");
        healthStars = root.Q<VisualElement>("HealthStars");
        speedStars = root.Q<VisualElement>("SpeedStars");
        damageStars = root.Q<VisualElement>("DamageStars");
        cadenceStars = root.Q<VisualElement>("CadenceStars");
        passiveLabel = root.Q<Label>("PassiveDescriptionLabel");
        ultimateLabel = root.Q<Label>("UltimateDescriptionLabel");
        classNoticeLabel = root.Q<Label>("ClassNoticeLabel");
        previousButton = root.Q<Button>("PreviousCharacterButton");
        nextButton = root.Q<Button>("NextCharacterButton");
        confirmButton = root.Q<Button>("ConfirmCharacterButton");
        backButton = root.Q<Button>("CharacterBackButton");
        previousButton.clicked += PreviousCharacter;
        nextButton.clicked += NextCharacter;
        confirmButton.clicked += ConfirmSelection;
        backButton.clicked += CancelSelection;
        infoContinueButton.clicked += ContinueToSelection;
        infoBackButton.clicked += CancelSelection;

        previewTexture = new RenderTexture(960, 720, 24, RenderTextureFormat.ARGB32);
        previewTexture.name = "Character Preview";
        previewTexture.Create();
        previewBackground = previewCamera.backgroundColor;
        previewClearFlags = previewCamera.clearFlags;
        previewCamera.targetTexture = previewTexture;
        previewImage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(previewTexture));
    }

    private void PreviousCharacter() => StepSelection(-1);
    private void NextCharacter() => StepSelection(1);

    private void ShowCharacter(int index, bool characterOnly = false)
    {
        if (previewModel != null)
        {
            previewModel.SetActive(false);
            Destroy(previewModel);
        }

        CharacterOption option = characters[index];
        previewModel = Instantiate(option.ModelPrefab, previewRoot);
        previewModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        previewModel.transform.localScale = Vector3.one;
        TankCharacterAnimation previewAnimation = previewModel.GetComponentInChildren<TankCharacterAnimation>(true);
        previewAnimation?.PlayIdle();
        if (!characterOnly) previewAnimation?.SetVisualHeightOffset(selectionCharacterHeightOffset);
        int layer = LayerMask.NameToLayer("CharacterPreview");
        foreach (Transform child in previewModel.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;

        // Preview models must not participate in navigation, collisions or damage.
        foreach (Collider collider in previewModel.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        if (characterOnly)
        {
            Transform characterRoot = TankAppearance.FindCharacterRoot(previewModel.transform);
            if (characterRoot != null)
            {
                // Hide the tank only in this disposable preview; keep the original prefab and battle actor intact.
                foreach (Renderer renderer in previewModel.GetComponentsInChildren<Renderer>(true))
                    if (!renderer.transform.IsChildOf(characterRoot)) renderer.enabled = false;
            }
        }

        FramePreview(characterOnly, false);
        nameLabel.text = option.DisplayName.ToUpperInvariant() +
            (string.IsNullOrEmpty(option.AppearanceName) ? "" : " — " + option.AppearanceName.ToUpperInvariant());
        indexLabel.text = $"{index + 1} / {CharacterCount}";
        UpdateClassDetails(option.ClassDefinition);
    }

    private void LateUpdate()
    {
        if (showingResult && previewModel != null && previewCamera.enabled) FramePreview(true, true);
    }

    private void FramePreview(bool characterOnly, bool smooth)
    {
        Renderer[] renderers = previewModel.GetComponentsInChildren<Renderer>();
        Bounds bounds = new(previewRoot.position, Vector3.one);
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled) continue;
            if (hasBounds) bounds.Encapsulate(renderer.bounds);
            else { bounds = renderer.bounds; hasBounds = true; }
        }

        previewCamera.clearFlags = characterOnly ? CameraClearFlags.SolidColor : previewClearFlags;
        previewCamera.backgroundColor = characterOnly ? Color.clear : previewBackground;
        Vector3 direction = characterOnly
            ? new Vector3(0f, 0.15f, 1f).normalized
            : new Vector3(3f, 2f, 4f).normalized;
        Vector3 cameraPosition = bounds.center + direction * 8f;
        float blend = smooth ? 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime) : 1f;
        previewCamera.transform.position = Vector3.Lerp(previewCamera.transform.position, cameraPosition, blend);
        previewCamera.transform.rotation = Quaternion.Slerp(previewCamera.transform.rotation,
            Quaternion.LookRotation(-direction), blend);
        float size = characterOnly
            ? Mathf.Max(0.75f, Mathf.Max(bounds.extents.y, bounds.extents.x / previewCamera.aspect) * 1.05f)
            : Mathf.Max(1.8f, bounds.extents.magnitude * 1.15f);
        previewCamera.orthographicSize = Mathf.Lerp(previewCamera.orthographicSize, size, blend);
    }

    private void UpdateClassDetails(CharacterClassDefinition definition)
    {
        // Older scenes without this optional panel/profile retain cosmetic selection.
        if (classDetails == null) return;
        classDetails.style.display = DisplayStyle.Flex;
        if (definition == null)
        {
            classNameLabel.text = "Classe básica";
            classDescriptionLabel.text = "Esta personagem ainda usa os atributos básicos de combate.";
            classAttributesLabel.text = "3 estrelas = padrão. Passe o mouse para ver os valores.";
            UpdateStatStars(null);
            passiveLabel.text = "Passiva: em preparação.";
            ultimateLabel.text = "Unique Skill: em preparação.";
            if (classNoticeLabel != null) classNoticeLabel.text = "Habilidades serão adicionadas nas próximas etapas.";
            return;
        }
        classNameLabel.text = definition.ClassName;
        classDescriptionLabel.text = definition.Description;
        classAttributesLabel.text = "3 estrelas = padrão. Passe o mouse para ver os valores.";
        UpdateStatStars(definition);
        passiveLabel.text = $"Passiva: {definition.PassiveName} — {definition.PassiveDescription}";
        ultimateLabel.text = $"Unique Skill: {definition.UltimateName} — {definition.UltimateDescription}";
        if (classNoticeLabel != null) classNoticeLabel.text = definition.GameplayEnabled
            ? "CLASSE ATIVA. Use SHIFT quando a Unique Skill estiver pronta."
            : "PLANEJAMENTO: atributos, passivas e Unique Skill ainda não estão ativos no combate.";
    }

    /// <summary>Three stars represent the existing actor base; each step is approximately 20%.</summary>
    private static int GetStatStars(float multiplier) =>
        Mathf.Clamp(Mathf.RoundToInt(3f + (multiplier - 1f) * 5f), 1, 5);

    private void UpdateStatStars(CharacterClassDefinition definition)
    {
        bool planned = definition != null && !definition.GameplayEnabled;
        SetStatStars(healthStars, definition != null ? definition.HealthMultiplier : 1f, "Vida", planned);
        SetStatStars(speedStars, definition != null ? definition.MovementSpeedMultiplier : 1f, "Velocidade", planned);
        SetStatStars(damageStars, definition != null ? definition.ShotDamageMultiplier : 1f, "Dano", planned);
        // A shorter interval means a better cadence, so stars use its reciprocal.
        SetStatStars(cadenceStars, definition != null ? 1f / Mathf.Max(.1f, definition.ShotIntervalMultiplier) : 1f,
            "Cadência", planned);
    }

    private static void SetStatStars(VisualElement row, float multiplier, string attribute, bool planned)
    {
        if (row == null) return; // Older authored scenes may not include the optional ratings yet.
        int stars = GetStatStars(multiplier);
        int index = 0;
        foreach (VisualElement star in row.Children()) star.EnableInClassList("stat-star-filled", index++ < stars);
        row.tooltip = $"{attribute}: {stars}/5 — {multiplier * 100f:0}% do padrão." +
            (planned ? " Planejado; ainda não aplicado ao combate." : " Atributo ativo no combate.");
    }

    private void OnDestroy()
    {
        if (IsOpen) BattleAudioController.StopCharacterVoice();
        if (previousButton != null) previousButton.clicked -= PreviousCharacter;
        if (nextButton != null) nextButton.clicked -= NextCharacter;
        if (confirmButton != null) confirmButton.clicked -= ConfirmSelection;
        if (backButton != null) backButton.clicked -= CancelSelection;
        if (infoContinueButton != null) infoContinueButton.clicked -= ContinueToSelection;
        if (infoBackButton != null) infoBackButton.clicked -= CancelSelection;
        if (previewCamera != null) previewCamera.targetTexture = null;
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
    }
}
