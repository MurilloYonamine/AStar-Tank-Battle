using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Shows the group's five cosmetic characters before combat, using an isolated rotating 3D preview.
/// The existing name/menu/backend flow is retained by BattleSessionController.
/// </summary>
public sealed class CharacterSelectionController : MonoBehaviour
{
    [Serializable]
    public sealed class CharacterOption
    {
        public string DisplayName;
        public GameObject ModelPrefab;
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
    private VisualElement previewImage;
    private Label nameLabel;
    private Label indexLabel;
    private Button previousButton;
    private Button nextButton;
    private Button confirmButton;
    private Button backButton;
    private GameObject previewModel;
    private RenderTexture previewTexture;
    private Color previewBackground;
    private CameraClearFlags previewClearFlags;
    private int selectedIndex;

    public bool IsOpen { get; private set; }
    public int CharacterCount => characters == null ? 0 : characters.Length;
    public event Action<int> Selected;
    public event Action Cancelled;

    public CharacterOption GetCharacter(int index) => characters[index];

    public void Open()
    {
        EnsureUI();
        SetControlsEnabled(true);
        IsOpen = true;
        document.rootVisualElement.pickingMode = PickingMode.Position;
        screen.style.display = DisplayStyle.Flex;
        previewCamera.enabled = true;
        ShowCharacter(selectedIndex);
        BattleAudioController.PlayCharacterVoice(characters[selectedIndex].VoiceClip);
        confirmButton.Focus();
    }

    public void Close()
    {
        BattleAudioController.StopCharacterVoice();
        IsOpen = false;
        if (screen != null) screen.style.display = DisplayStyle.None;
        if (document != null) document.rootVisualElement.pickingMode = PickingMode.Ignore;
        if (previewCamera != null) previewCamera.enabled = false;
        if (previewModel != null) previewModel.SetActive(false);
    }

    public void ShowResultPreview(int index, VisualElement resultImage)
    {
        if (resultImage == null || CharacterCount == 0) return;
        EnsureUI();
        Close();
        // A separate, collision-free preview dances. The dead gameplay actor stays dead.
        ShowCharacter(Mathf.Clamp(index, 0, CharacterCount - 1), true);
        previewModel.GetComponentInChildren<TankCharacterAnimation>(true)?.PlayDance();
        resultImage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(previewTexture));
        previewCamera.enabled = true;
    }

    public void StepSelection(int direction)
    {
        if (!IsOpen || CharacterCount == 0) return;
        selectedIndex = (selectedIndex + direction % CharacterCount + CharacterCount) % CharacterCount;
        ShowCharacter(selectedIndex);
        BattleAudioController.PlaySelectionChange();
        BattleAudioController.PlayCharacterVoice(characters[selectedIndex].VoiceClip);
    }

    public void ConfirmSelection()
    {
        if (!IsOpen) return;
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
        previewImage = root.Q<VisualElement>("CharacterPreview");
        nameLabel = root.Q<Label>("CharacterName");
        indexLabel = root.Q<Label>("CharacterIndex");
        previousButton = root.Q<Button>("PreviousCharacterButton");
        nextButton = root.Q<Button>("NextCharacterButton");
        confirmButton = root.Q<Button>("ConfirmCharacterButton");
        backButton = root.Q<Button>("CharacterBackButton");
        previousButton.clicked += PreviousCharacter;
        nextButton.clicked += NextCharacter;
        confirmButton.clicked += ConfirmSelection;
        backButton.clicked += CancelSelection;

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
            Animator animator = previewModel.GetComponentInChildren<TankCharacterAnimation>(true)?.Animator;
            if (animator != null)
            {
                // Hide the tank only in this disposable preview; keep the original prefab and battle actor intact.
                foreach (Renderer renderer in previewModel.GetComponentsInChildren<Renderer>(true))
                    if (!renderer.transform.IsChildOf(animator.transform)) renderer.enabled = false;
            }
        }

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
        previewCamera.transform.position = bounds.center + direction * 8f;
        previewCamera.transform.LookAt(bounds.center);
        previewCamera.orthographicSize = characterOnly
            ? Mathf.Max(0.75f, Mathf.Max(bounds.extents.y, bounds.extents.x / previewCamera.aspect) * 1.05f)
            : Mathf.Max(1.8f, bounds.extents.magnitude * 1.15f);
        nameLabel.text = option.DisplayName.ToUpperInvariant();
        indexLabel.text = $"{index + 1} / {CharacterCount}";
    }

    private void OnDestroy()
    {
        if (IsOpen) BattleAudioController.StopCharacterVoice();
        if (previousButton != null) previousButton.clicked -= PreviousCharacter;
        if (nextButton != null) nextButton.clicked -= NextCharacter;
        if (confirmButton != null) confirmButton.clicked -= ConfirmSelection;
        if (backButton != null) backButton.clicked -= CancelSelection;
        if (previewCamera != null) previewCamera.targetTexture = null;
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
    }
}
