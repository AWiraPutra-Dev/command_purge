using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CaseManager : MonoBehaviour
{
    private enum InvestState
    {
        Notification,   // folder blink, tunggu "open"
        MainMenu,       // pilih ask / info / check / traits
        AskMenu,        // pilih pertanyaan (keyword dinamis per CaseQuestion)
        ResponseDelay,  // nunggu delay jawaban NPC sebelum balik ke option
        InfoMode,        // lihat data panel, "back" balik
        CheckMode,       // inspect foto, right/left buat rotate, "back" balik
        TraitsMode,      // lihat kriteria anomali, "back" balik
        TypeTestAsk,    // ketik jawaban pertanyaan
        TypeTestPrint,  // ketik jawaban print-phase
        PrintConfirm,   // tunggu "confirm" sebelum cetak
        PrintAnim,       // loading, input block
        DeniedConfirm    // tunggu "confirm" setelah denied, sebelum exit tanpa print
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

    [Header("Jeda antar-kasus (detik) — waktu di luar komputer sebelum kasus berikutnya mulai")]
    [SerializeField] private float interCaseDelay = 20f;

    [Header("Response Delay (detik) — jeda waktu NPC ngejawab sebelum balik ke option")]
    [SerializeField] private float npcResponseDelay = 2.0f;

    private Coroutine npcResponseCoroutine;

    public bool IsCaseActive => isCaseActive;
    public CaseDefinition CurrentCase => currentCase;
    public SubjectDataModel CurrentSubject => currentSubject;

    // Dipanggil pas dokumen selesai dicetak, ATAU pas player ketik "esc" di state manapun.
    // Kedua kasus TIDAK mereset state investigasi (chances, currentState, currentFrameIndex, dll
    // tetap seperti apa adanya) — jadi begitu balik ke komputer, lanjut persis dari titik yang sama.
    public Action OnRequestExitComputer;

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

        terminalController.UpdateBatteryFill(chancesRemaining, currentCase.maxChances);


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

        // ESC sekarang GLOBAL: langsung keluar dari komputer, diam-diam, di state manapun
        // (kecuali PrintAnim, yang inputnya sudah di-block total). State investigasi tetap disimpan.
        if (cmd == "esc")
        {
            if (npcResponseCoroutine != null)
            {
                StopCoroutine(npcResponseCoroutine);
                npcResponseCoroutine = null;
                commandProcessor.BlockInput(false);
            }
            OnRequestExitComputer?.Invoke();
            return;
        }

        switch (currentState)
        {
            case InvestState.Notification:   HandleNotification(cmd); break;
            case InvestState.MainMenu:       HandleMainMenu(cmd); break;
            case InvestState.AskMenu:        HandleAskMenu(cmd); break;
            case InvestState.ResponseDelay:  break; // input di-block selama delay respon NPC
            case InvestState.InfoMode:       HandleInfoMode(cmd); break;
            case InvestState.CheckMode:      HandleCheckMode(cmd, parts); break;
            case InvestState.TraitsMode:     HandleTraitsMode(cmd); break;
            case InvestState.TypeTestAsk:    HandleTypeTestAsk(cmd); break;
            case InvestState.TypeTestPrint:  HandleTypeTestPrint(cmd); break;
            case InvestState.PrintConfirm:   HandlePrintConfirm(cmd); break;
            case InvestState.PrintAnim:      break; // input di-block
            case InvestState.DeniedConfirm:  HandleDeniedConfirm(cmd); break;
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
        terminalController.ShowAskInfoCheckPanelSequential(GetFrontPhoto());
        
        // Tampilkan baterai hanya jika gameplay aktif (bukan saat tutorial print)
        terminalController.SetBatteryVisibility(isCaseActive);
        terminalController.UpdateBatteryFill(chancesRemaining, currentCase.maxChances);

        if (chancesRemaining <= 0)
            terminalController.ShowForcedVerdictMode(currentCase.forceApprovedOnly);
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
            terminalController.UpdateBatteryFill(chancesRemaining, currentCase.maxChances);
            PrintBatteryStatus();
            terminalController.HideAllTutorialPanels();
            terminalController.ShowDataPanel(currentSubject);
            terminalController.AddLine("Ketik 'back' untuk kembali.", TerminalLineType.System);
            currentState = InvestState.InfoMode;
        }
        else if (cmd == "check")
        {
            if (chancesRemaining <= 0) { NoChance(); return; }
            chancesRemaining--;
            terminalController.UpdateBatteryFill(chancesRemaining, currentCase.maxChances);
            PrintBatteryStatus();
            currentFrameIndex = 0;
            terminalController.ShowInspectPanel(currentSubject.photoFrames, currentFrameIndex);
            terminalController.AddLine("Ketik 'right' / 'left' buat putar, 'back' buat kembali.", TerminalLineType.System);
            currentState = InvestState.CheckMode;
        }
        else if (cmd == "traits")
        {
            // Traits = rulebook referensi, TIDAK makan baterai (bukan aksi investigasi ke subjek).
            terminalController.HideAllTutorialPanels();
            terminalController.ShowTraitsPanel(currentCase.traits);
            terminalController.AddLine("Ketik 'back' untuk kembali.", TerminalLineType.System);
            currentState = InvestState.TraitsMode;
        }
        else if (cmd == "approved" || cmd == "denied")
        {
            if (currentCase.forceApprovedOnly && cmd != "approved")
            {
                terminalController.AddLine("SISTEM: Subjek ini telah diverifikasi oleh otoritas pusat. Penolakan tidak diizinkan.", TerminalLineType.Error);
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
            terminalController.AddLine("Perintah tidak dikenal. Ketik ask / info / check / traits / print.", TerminalLineType.Error);
        }
    }

    private void NoChance()
    {
        terminalController.AddLine("Kesempatan habis. Ambil keputusan: approved / denied.", TerminalLineType.Error);
    }

    private void PrintBatteryStatus()
    {
        terminalController.AddLine("[Sisa Baterai / Kesempatan: " + chancesRemaining + "/" + currentCase.maxChances + "]", TerminalLineType.Warning);
    }

    // ═══════════════════════════════════════════════
    // ASK MENU — daftar pertanyaan dinamis (scalable, jumlah bebas)
    // ═══════════════════════════════════════════════
    private void ShowAskQuestionPanel(string responseText = null)
    {
        terminalController.ShowAskQuestionPanel(currentCase.questions, responseText);
        terminalController.AddLine("Ketik kata kunci pertanyaan yang mau ditanyakan, atau 'back' untuk batal.", TerminalLineType.System);
    }

    private void HandleAskMenu(string cmd)
    {
        if (cmd == "back")
        {
            if (npcResponseCoroutine != null)
            {
                StopCoroutine(npcResponseCoroutine);
                npcResponseCoroutine = null;
                commandProcessor.BlockInput(false);
            }
            terminalController.HideAskQuestionPanel();
            currentState = InvestState.MainMenu;
            ShowMainPanel();
            return;
        }

        int idx = FindQuestionIndexByKeyword(cmd);
        if (idx < 0)
        {
            terminalController.AddLine("Kata kunci tidak dikenal. Cek daftar pertanyaan di panel.", TerminalLineType.Error);
            return;
        }

        if (chancesRemaining <= 0) { NoChance(); return; }
        chancesRemaining--;
        terminalController.UpdateBatteryFill(chancesRemaining, currentCase.maxChances);
        PrintBatteryStatus();

        CaseQuestion q = currentCase.questions[idx];
        terminalController.AddLine("Kamu: " + q.questionText, TerminalLineType.Input);

        if (q.useTypeTest)
        {
            pendingQuestion = q;
            string prompt = !string.IsNullOrWhiteSpace(q.typeTestPrompt) ? q.typeTestPrompt : q.questionText;
            terminalController.ShowTypeTestPanel(prompt);
            terminalController.AddLine("Ketik ulang pertanyaan di atas untuk memanggil jawaban.", TerminalLineType.Warning);
            currentState = InvestState.TypeTestAsk;
        }
        else
        {
            PrintQuestionResponse(q);
            currentState = InvestState.AskMenu;
        }
    }

    private int FindQuestionIndexByKeyword(string cmd)
    {
        if (currentCase.questions == null) return -1;

        for (int i = 0; i < currentCase.questions.Count; i++)
        {
            string keyword = GetQuestionKeyword(currentCase.questions[i], i);
            if (cmd == keyword) return i;
        }
        return -1;
    }

    private string GetQuestionKeyword(CaseQuestion q, int index)
    {
        return string.IsNullOrWhiteSpace(q.commandKeyword)
            ? ("q" + (index + 1))
            : q.commandKeyword.Trim().ToLower();
    }

    private void HandleTypeTestAsk(string input)
    {
        if (pendingQuestion == null) { currentState = InvestState.AskMenu; ShowAskQuestionPanel(); return; }

        string prompt = !string.IsNullOrWhiteSpace(pendingQuestion.typeTestPrompt)
            ? pendingQuestion.typeTestPrompt
            : pendingQuestion.questionText;

        string target = !string.IsNullOrWhiteSpace(pendingQuestion.typeTestAnswer)
            ? pendingQuestion.typeTestAnswer
            : prompt;

        if (CheckTypeTestMatch(input, target))
        {
            terminalController.HideTypeTestPanel();
            PrintQuestionResponse(pendingQuestion);
            pendingQuestion = null;
            currentState = InvestState.AskMenu;
        }
        else
        {
            terminalController.AddLine("Ketik ulang pertanyaan di atas untuk memanggil jawaban.", TerminalLineType.Error);
        }
    }

    private void PrintQuestionResponse(CaseQuestion q)
    {
        if (npcResponseCoroutine != null)
        {
            StopCoroutine(npcResponseCoroutine);
            npcResponseCoroutine = null;
        }
        npcResponseCoroutine = StartCoroutine(NPCResponseRoutine(q));
    }

    private IEnumerator NPCResponseRoutine(CaseQuestion q)
    {
        currentState = InvestState.ResponseDelay;
        commandProcessor.BlockInput(true);

        string responseText = currentSubject.isMimicBool ? q.mimicResponse : q.realResponse;

        // Print jawaban ke terminal
        if (currentSubject.isMimicBool)
            terminalController.AddLine(responseText, TerminalLineType.Warning);
        else
            terminalController.AddLine(responseText, TerminalLineType.Response);

        // Tampilkan hanya respon NPC di UI tanpa opsi pertanyaan dulu
        terminalController.ShowNPCResponseOnly(responseText);

        // Delay respon NPC
        yield return new WaitForSeconds(npcResponseDelay);

        commandProcessor.BlockInput(false);
        
        // Sembunyikan panel respon dan balik ke MainMenu (opsi awal ask, info, check)
        terminalController.HideAskQuestionPanel();
        currentState = InvestState.MainMenu;
        ShowMainPanel();
        
        npcResponseCoroutine = null;
    }

    // ═══════════════════════════════════════════════
    // INFO MODE
    // ═══════════════════════════════════════════════
    private void HandleInfoMode(string cmd)
    {
        if (cmd == "back")
        {
            terminalController.HideDataPanel();
            currentState = InvestState.MainMenu;
            ShowMainPanel();
        }
    }

    // ═══════════════════════════════════════════════
    // TRAITS MODE — kriteria anomali, gratis (gak makan baterai)
    // ═══════════════════════════════════════════════
    private void HandleTraitsMode(string cmd)
    {
        if (cmd == "back")
        {
            terminalController.HideTraitsPanel();
            currentState = InvestState.MainMenu;
            ShowMainPanel();
        }
    }

    // ═══════════════════════════════════════════════
    // CHECK MODE
    // ═══════════════════════════════════════════════
    private void HandleCheckMode(string cmd, string[] parts)
    {
        if (cmd == "back")
        {
            terminalController.HideInspectPanel();
            currentState = InvestState.MainMenu;
            ShowMainPanel();
            return;
        }

        if (cmd == "right" || cmd == "left" || cmd == "rotate")
        {
            bool left = cmd == "left" || (parts.Length > 1 && parts[1] == "left");
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

        if (currentCase.glitchOnRotate)
        {
            yield return StartCoroutine(terminalController.ShowPhotoGlitch(
                currentSubject.photoFrames, currentFrameIndex));
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        terminalController.AddLine("Sinyal kembali normal.", TerminalLineType.System);
        glitchActive = false;
    }

    // ═══════════════════════════════════════════════
    // VERDICT + PRINT
    // ═══════════════════════════════════════════════
    public void ProcessVerdict(bool approved)
    {
        if (currentSubject == null) return;

        // Catat statistik keputusan secara rahasia di PlayerAccuracy
        if (PlayerAccuracy.Instance != null)
            PlayerAccuracy.Instance.RecordVerdict(approved, currentSubject.isMimicBool);

        terminalController.AddLine(">>> VERDICT: " + (approved ? "APPROVED" : "DENIED") + " <<<", TerminalLineType.System);
        terminalController.ShowVerdictPanel(approved);

        if (approved)
        {
            // Approved: lanjut ke fase print seperti biasa
            terminalController.AddLine("Keputusan telah dicatat ke sistem pusat.", TerminalLineType.Response);
            terminalController.AddLine("Ketik 'print' untuk mencetak dokumen.", TerminalLineType.System);
            currentState = InvestState.MainMenu;
        }
        else
        {
            // Denied: minta konfirmasi sebelum exit tanpa print
            terminalController.AddLine("Ketik 'confirm' untuk memastikan penolakan subjek ini.", TerminalLineType.Warning);
            currentState = InvestState.DeniedConfirm;
        }
    }

    // ═══════════════════════════════════════════════
    // DENIED CONFIRM — konfirmasi sebelum exit tanpa print
    // ═══════════════════════════════════════════════
    private void HandleDeniedConfirm(string cmd)
    {
        if (cmd == "confirm")
        {
            terminalController.AddLine("Terima kasih. Subjek telah dikembalikan. Shift berlanjut.", TerminalLineType.Response);
            isCaseActive = false;
            StartCoroutine(DeniedExitRoutine());
        }
        else
        {
            terminalController.AddLine("Ketik 'confirm' untuk memastikan penolakan.", TerminalLineType.Error);
        }
    }

    private IEnumerator DeniedExitRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        OnRequestExitComputer?.Invoke();
        yield return new WaitForSeconds(interCaseDelay);
        gameManager.AdvanceAfterPrint();
    }


    private void StartPrintPhase()
    {
        terminalController.HideAllTutorialPanels();
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
        string prompt = !string.IsNullOrWhiteSpace(currentCase?.printTypeTestPrompt)
            ? currentCase.printTypeTestPrompt
            : "typetest";

        string target = !string.IsNullOrWhiteSpace(currentCase?.printTypeTestAnswer)
            ? currentCase.printTypeTestAnswer
            : prompt;

        if (CheckTypeTestMatch(input, target))
        {
            terminalController.HideTypeTestPanel();
            StartCoroutine(RunPrintAnimation());
        }
        else
        {
            terminalController.AddLine("Ketik ulang teks di atas untuk lanjut mencetak.", TerminalLineType.Error);
        }
    }

    private bool CheckTypeTestMatch(string input, string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return true;
        if (input == null) return false;
        return string.Equals(input.Trim(), target.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // Begitu loading selesai: langsung OUT (exit komputer diam-diam, TANPA teks apa pun),
    // lalu jeda interCaseDelay detik di luar, baru kasus berikutnya mulai.
    private IEnumerator RunPrintAnimation()
    {
        currentState = InvestState.PrintAnim;
        commandProcessor.BlockInput(true);
        terminalController.ShowLoadingPanel();

        yield return new WaitForSeconds(terminalController.GetPrintDuration());

        terminalController.HideLoadingPanel();
        commandProcessor.BlockInput(false);

        isCaseActive = false;

        // Langsung keluar komputer, diam-diam (gak ada kata-kata terakhir).
        OnRequestExitComputer?.Invoke();

        // Jeda di luar sebelum kasus berikutnya mulai.
        yield return new WaitForSeconds(interCaseDelay);

        gameManager.AdvanceAfterPrint();
    }
}