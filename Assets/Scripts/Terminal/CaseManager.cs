using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CaseManager : MonoBehaviour
{
    private enum InvestState
    {
        Notification,   // folder blink, tunggu "open"
        MainMenu,       // pilih ask / info / check
        AskMenu,        // pilih 1/2/3
        InfoMode,        // lihat data panel, "esc" balik
        CheckMode,       // inspect foto, rotate, "esc" balik
        TypeTestAsk,    // ketik jawaban pertanyaan
        TypeTestPrint,  // ketik jawaban print-phase
        PrintConfirm,   // tunggu "confirm" sebelum cetak
        PrintAnim        // loading, input block
    }

    private GameManager gameManager;
    private TerminalController terminalController;
    private CommandProcessorService commandProcessor;

    private CaseDefinition[] allCases;
    private CaseDefinition currentCase;
    private SubjectDataModel currentSubject;
    private int currentFrameIndex;

    private InvestState currentState = InvestState.Notification;
    private int chancesRemaining;
    private CaseQuestion pendingQuestion;
    private bool isCaseActive;
    private bool glitchActive;

    public bool IsCaseActive => isCaseActive;
    public CaseDefinition CurrentCase => currentCase;
    public SubjectDataModel CurrentSubject => currentSubject;

    public void Initialize(GameManager gm, CaseDefinition[] cases, TerminalController tc)
    {
        gameManager = gm;
        allCases = cases;
        terminalController = tc;
        commandProcessor = tc.GetCommandProcessor();
        commandProcessor.SetCaseManager(this);
    }

    // ═══════════════════════════════════════════════
    // CASE START
    // ═══════════════════════════════════════════════
    public void StartCase(CaseDefinition caseDef)
    {
        currentCase = caseDef;
        currentSubject = terminalController.GetSubjectDataById(caseDef.subjectId);
        isCaseActive = true;

        if (currentSubject == null)
        {
            terminalController.AddLine("ERR: Data subject tidak ditemukan.", TerminalLineType.Error);
            return;
        }

        chancesRemaining = currentCase.maxChances;
        currentFrameIndex = 0;
        glitchActive = false;
        currentState = InvestState.Notification;

        terminalController.HideAllTutorialPanels();
        terminalController.ShowFolderIcon();
        terminalController.ShowFolderNotification();

        terminalController.AddLine("", TerminalLineType.System);
        terminalController.AddLine("===================================", TerminalLineType.System);
        terminalController.AddLine("KASUS #" + currentCase.caseNumber, TerminalLineType.System);
        terminalController.AddLine("===================================", TerminalLineType.System);
        terminalController.AddLine(currentCase.notificationText, TerminalLineType.Warning);
        terminalController.AddLine("Folder berkedip. Ketik 'open' untuk membuka kasus.", TerminalLineType.System);
    }

    public void ProcessInput(string rawInput, string[] parts)
    {
        if (!isCaseActive || glitchActive) return;

        string cmd = rawInput.Trim().ToLower();

        switch (currentState)
        {
            case InvestState.Notification: HandleNotification(cmd); break;
            case InvestState.MainMenu:     HandleMainMenu(cmd); break;
            case InvestState.AskMenu:      HandleAskMenu(cmd); break;
            case InvestState.InfoMode:     HandleInfoMode(cmd); break;
            case InvestState.CheckMode:     HandleCheckMode(cmd, parts); break;
            case InvestState.TypeTestAsk:  HandleTypeTestAsk(cmd); break;
            case InvestState.TypeTestPrint: HandleTypeTestPrint(cmd); break;
            case InvestState.PrintConfirm: HandlePrintConfirm(cmd); break;
            case InvestState.PrintAnim:    break; // input di-block
        }
    }

    // ═══════════════════════════════════════════════
    // NOTIFICATION
    // ═══════════════════════════════════════════════
    private void HandleNotification(string cmd)
    {
        if (cmd == "open")
        {
            terminalController.HideFolderNotification();
            OpenCase();
        }
    }

    private void OpenCase()
    {
        currentState = InvestState.MainMenu;

        terminalController.HideAllTutorialPanels();
        terminalController.AddLine(">>> MEMBUKA KASUS #" + currentCase.caseNumber + " <<<", TerminalLineType.System);
        terminalController.AddLine("SUBJEK: " + currentSubject.fullNameString, TerminalLineType.Response);
        ShowMainPanel();
    }

    private Sprite GetFrontPhoto()
    {
        if (currentSubject?.photoFrames != null && currentSubject.photoFrames.Length > 0)
            return currentSubject.photoFrames[0];
        return currentSubject?.subjectPhoto;
    }

    private void ShowMainPanel()
    {
        terminalController.HideAllTutorialPanels();
        terminalController.ShowAskInfoCheckPanel();
        terminalController.SetAskInfoCheckPhoto(GetFrontPhoto());
        terminalController.AddLine("Sisa kesempatan: " + chancesRemaining, TerminalLineType.System);
    }

    // ═══════════════════════════════════════════════
    // MAIN MENU
    // ═══════════════════════════════════════════════
    private void HandleMainMenu(string cmd)
    {
        if (cmd == "ask")
        {
            if (chancesRemaining <= 0) { NoChance(); return; }
            currentState = InvestState.AskMenu;
            ShowAskQuestionPanel();
        }
        else if (cmd == "info")
        {
            if (chancesRemaining <= 0) { NoChance(); return; }
            chancesRemaining--;
            terminalController.HideAllTutorialPanels();
            terminalController.ShowDataPanel(currentSubject);
            terminalController.AddLine("Ketik 'esc' untuk kembali.", TerminalLineType.System);
            currentState = InvestState.InfoMode;
        }
        else if (cmd == "check")
        {
            if (chancesRemaining <= 0) { NoChance(); return; }
            chancesRemaining--;
            terminalController.ShowInspectPanel(currentSubject.photoFrames, currentFrameIndex);
            terminalController.AddLine("Ketik 'esc' untuk kembali. 'rotate' untuk ubah sudut.", TerminalLineType.System);
            currentState = InvestState.CheckMode;
        }
        else if (cmd == "approved" || cmd == "denied")
        {
            if (currentCase.forceApprovedOnly && cmd != "approved")
            {
                terminalController.AddLine("Kamu DIPAKSA menyetujui subjek ini. Ketik 'approved'.", TerminalLineType.Error);
                return;
            }
            ProcessVerdict(cmd == "approved");
        }
        else if (cmd == "print")
        {
            StartPrintPhase();
        }
        else
        {
            terminalController.AddLine("Perintah tidak dikenal. Ketik ask / info / check / print.", TerminalLineType.Error);
        }
    }

    private void NoChance()
    {
        terminalController.AddLine("Kesempatan habis. Ambil keputusan: approved / denied.", TerminalLineType.Error);
    }

    // ═══════════════════════════════════════════════
    // ASK MENU — daftar 1/2/3 di panel (ID / NAME / REASON)
    // ═══════════════════════════════════════════════
    private void ShowAskQuestionPanel()
    {
        terminalController.HideAllTutorialPanels();
        terminalController.ShowAskQuestionPanel();
        terminalController.AddLine("Ketik ID / NAME / REASON, atau 'esc' untuk batal.", TerminalLineType.System);
    }

    private void HandleAskMenu(string cmd)
    {
        if (cmd == "esc") { currentState = InvestState.MainMenu; ShowMainPanel(); return; }

        int idx = -1;
        if (cmd == "id") idx = 0;
        else if (cmd == "name") idx = 1;
        else if (cmd == "reason") idx = 2;
        else { return; }

        if (currentCase.questions == null || idx < 0 || idx >= currentCase.questions.Count)
        {
            terminalController.AddLine("Pertanyaan tidak valid.", TerminalLineType.Error);
            return;
        }

        if (chancesRemaining <= 0) { NoChance(); return; }
        chancesRemaining--;

        CaseQuestion q = currentCase.questions[idx];
        terminalController.AddLine("Kamu: " + q.questionText, TerminalLineType.Input);

        if (q.useTypeTest)
        {
            pendingQuestion = q;
            terminalController.ShowTypeTestPanel(q.questionText);
            terminalController.AddLine("Ketik ulang pertanyaan di atas untuk memanggil jawaban.", TerminalLineType.Warning);
            currentState = InvestState.TypeTestAsk;
        }
        else
        {
            PrintQuestionResponse(q);
            currentState = InvestState.AskMenu;
            ShowMainPanel();
        }
    }

    private void HandleTypeTestAsk(string input)
    {
        if (pendingQuestion == null) { currentState = InvestState.AskMenu; ShowMainPanel(); return; }

        if (input.Trim().ToLower() == pendingQuestion.questionText.Trim().ToLower())
        {
            terminalController.HideTypeTestPanel();
            PrintQuestionResponse(pendingQuestion);
            pendingQuestion = null;
            currentState = InvestState.AskMenu;
            ShowMainPanel();
        }
        else
        {
            terminalController.AddLine("Ketik ulang pertanyaan di atas untuk memanggil jawaban.", TerminalLineType.Error);
        }
    }

    private void PrintQuestionResponse(CaseQuestion q)
    {
        if (currentSubject.isMimicBool)
            terminalController.AddLine(q.mimicResponse, TerminalLineType.Warning);
        else
            terminalController.AddLine(q.realResponse, TerminalLineType.Response);
    }

    // ═══════════════════════════════════════════════
    // INFO MODE
    // ═══════════════════════════════════════════════
    private void HandleInfoMode(string cmd)
    {
        if (cmd == "esc")
        {
            terminalController.HideDataPanel();
            currentState = InvestState.MainMenu;
            ShowMainPanel();
        }
    }

    // ═══════════════════════════════════════════════
    // CHECK MODE
    // ═══════════════════════════════════════════════
    private void HandleCheckMode(string cmd, string[] parts)
    {
        if (cmd == "esc")
        {
            terminalController.HideInspectPanel();
            currentState = InvestState.MainMenu;
            ShowMainPanel();
            return;
        }

        if (cmd == "rotate")
        {
            bool left = parts.Length > 1 && parts[1] == "left";
            if (left) RotatePhotoLeft(); else RotatePhotoRight();

            if (currentCase.glitchOnRotate)
                StartGlitch();
        }
    }

    public void RotatePhotoLeft()
    {
        if (currentSubject?.photoFrames == null) return;
        currentFrameIndex = (currentFrameIndex - 1 + currentSubject.photoFrames.Length) % currentSubject.photoFrames.Length;
        terminalController.UpdateInspectPhoto(currentSubject.photoFrames, currentFrameIndex);
    }

    public void RotatePhotoRight()
    {
        if (currentSubject?.photoFrames == null) return;
        currentFrameIndex = (currentFrameIndex + 1) % currentSubject.photoFrames.Length;
        terminalController.UpdateInspectPhoto(currentSubject.photoFrames, currentFrameIndex);
    }

    private void StartGlitch()
    {
        glitchActive = true;
        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        terminalController.AddLine("[!!!] Sinyal terganggu...", TerminalLineType.Error);
        yield return new WaitForSeconds(2f);
        terminalController.AddLine("Sinyal kembali normal.", TerminalLineType.System);
        glitchActive = false;
    }

    // ═══════════════════════════════════════════════
    // VERDICT + PRINT
    // ═══════════════════════════════════════════════
    public void ProcessVerdict(bool approved)
    {
        if (currentSubject == null) return;

        bool isCorrect = approved != currentSubject.isMimicBool;

        terminalController.AddLine(">>> VERDICT: " + (approved ? "APPROVED" : "DENIED") + " <<<", TerminalLineType.System);

        terminalController.ShowVerdictPanel(approved);

        if (isCorrect)
            terminalController.AddLine("[BENAR] Verifikasi akurat.", TerminalLineType.Response);
        else if (currentSubject.isMimicBool && approved)
            terminalController.AddLine("[FATAL] Kamu menyetujui MIMIC. Alpha Sector terancam bahaya.", TerminalLineType.Error);
        else
            terminalController.AddLine("[ERROR] Kamu menolak warga sah. Laporan dibuat.", TerminalLineType.Error);

        terminalController.AddLine("Ketik 'print' untuk mencetak dokumen.", TerminalLineType.System);
        currentState = InvestState.MainMenu;
    }

    private void StartPrintPhase()
    {
        terminalController.HideAllTutorialPanels();

        terminalController.AddLine(">>> MENCETAK DOKUMEN: " + currentSubject.fullNameString + " <<<", TerminalLineType.System);
        terminalController.ShowDataPanel(currentSubject);

        if (currentCase.usePrintTypeTest)
        {
            terminalController.ShowTypeTestPanel(currentCase.printTypeTestPrompt);
            terminalController.AddLine("Ketik ulang teks di atas untuk lanjut mencetak.", TerminalLineType.Warning);
            currentState = InvestState.TypeTestPrint;
        }
        else
        {
            terminalController.AddLine("Ketik 'confirm' untuk melanjutkan pencetakan.", TerminalLineType.System);
            currentState = InvestState.PrintConfirm;
        }
    }

    private void HandlePrintConfirm(string cmd)
    {
        if (cmd == "confirm")
        {
            StartCoroutine(RunPrintAnimation());
        }
        else
        {
            terminalController.AddLine("Ketik 'confirm' untuk melanjutkan pencetakan.", TerminalLineType.Error);
        }
    }

    private void HandleTypeTestPrint(string input)
    {
        if (input.Trim().ToLower() == currentCase.printTypeTestAnswer.Trim().ToLower())
        {
            terminalController.HideTypeTestPanel();
            StartCoroutine(RunPrintAnimation());
        }
        else
        {
            terminalController.AddLine("Ketik ulang teks di atas untuk lanjut mencetak.", TerminalLineType.Error);
        }
    }

    private IEnumerator RunPrintAnimation()
    {
        currentState = InvestState.PrintAnim;
        commandProcessor.BlockInput(true);
        terminalController.ShowLoadingPanel();

        yield return new WaitForSeconds(terminalController.GetPrintDuration());

        terminalController.HideLoadingPanel();
        terminalController.AddLine(">>> DOKUMEN TERCETAK <<<", TerminalLineType.System);
        commandProcessor.BlockInput(false);

        isCaseActive = false;
        gameManager.AdvanceAfterPrint();
    }

    public void ExitComputer()
    {
        terminalController.AddLine("Tekan E untuk keluar dari komputer.", TerminalLineType.System);
    }
}
