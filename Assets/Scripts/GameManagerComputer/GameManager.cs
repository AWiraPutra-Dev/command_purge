using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TerminalController terminalController;
    [SerializeField] private CaseManager caseManager;

    [Header("Case List")]
    [SerializeField] private CaseDefinition[] caseDefinitions;

    [Header("DEBUG / Testing — JANGAN aktif pas build final")]
    [Tooltip("Centang ini buat lompat langsung ke case-case, skip OpeningSequence sepenuhnya. Berguna buat ngetes CaseDefinition/SubjectDefinition tanpa ngulang tutorial tiap Play.")]
    [SerializeField] private bool skipOpeningSequenceOnPlay = false;

    public bool IsTutorialComplete { get; private set; }
    public int CurrentCaseIndex { get; private set; } = -1;

    public System.Action OnAllCasesComplete;

    // Dipanggil otomatis begitu dokumen selesai dicetak (lihat CaseManager.RunPrintAnimation).
    // Subscribe ke event ini di script yang megang kamera/movement player buat keluar dari komputer,
    // misal: GameManager.Instance.OnPlayerShouldExitComputer += ExitComputerView;
    public System.Action OnPlayerShouldExitComputer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (!skipOpeningSequenceOnPlay) return;

        // Matiin OpeningSequence kalau ada di scene, biar dia gak jalan barengan/rebutan input.
        OpeningSequence openingSequence = FindFirstObjectByType<OpeningSequence>();
        if (openingSequence != null)
            openingSequence.gameObject.SetActive(false);

        // Terminal_Panel & input field biasanya baru diaktifin OpeningSequence — kita nyalain manual.
        if (terminalController != null)
        {
            terminalController.gameObject.SetActive(true);
            terminalController.HideAllTutorialPanels();
        }

        OnTutorialComplete();
    }

    private void OnDestroy()
    {
        if (caseManager != null)
            caseManager.OnRequestExitComputer -= HandleRequestExitComputer;
    }

    public void OnTutorialComplete()
    {
        IsTutorialComplete = true;
        CurrentCaseIndex = -1;

        terminalController.AddLine("SISTEM VERIFIKASI AKTIF.", TerminalLineType.Response);
        terminalController.AddLine("Ketik 'help' untuk melihat daftar perintah.", TerminalLineType.System);

        terminalController.inputBlocked = false;
        terminalController.FocusInput();

        caseManager.Initialize(this, caseDefinitions, terminalController);
        caseManager.OnRequestExitComputer += HandleRequestExitComputer;

        StartNextCase();
    }

    private void HandleRequestExitComputer()
    {
        // Diteruskan ke siapa pun yang subscribe (biasanya script movement/kamera player).
        OnPlayerShouldExitComputer?.Invoke();
    }

    public void StartNextCase()
    {
        CurrentCaseIndex++;

        if (CurrentCaseIndex >= caseDefinitions.Length)
        {
            terminalController.AddLine("Semua kasus selesai. Menunggu perintah...", TerminalLineType.System);
            OnAllCasesComplete?.Invoke();
            return;
        }

        CaseDefinition caseDef = caseDefinitions[CurrentCaseIndex];
        caseManager.StartCase(caseDef);
    }

    public void AdvanceAfterPrint()
    {
        if (caseManager != null && caseManager.CurrentCase != null &&
            caseManager.CurrentCase.caseType == CaseType.Climax)
        {
            terminalController.AddLine("SEMUA KASUS SELESAI. TERIMA KASIH, VERIFIER.", TerminalLineType.System);
            OnAllCasesComplete?.Invoke();
            return;
        }

        StartNextCase();
    }
}