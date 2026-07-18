using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TerminalController terminalController;
    [SerializeField] private CaseManager caseManager;

    [Header("Case List")]
    [SerializeField] private CaseDefinition[] caseDefinitions;

    public bool IsTutorialComplete { get; private set; }
    public int CurrentCaseIndex { get; private set; } = -1;

    public System.Action OnAllCasesComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        StartNextCase();
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
