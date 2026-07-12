using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OpeningSequence : MonoBehaviour
{
    private GameObject         screensaverPanel;
    private TerminalController terminalController;
    private TMP_InputField     playerInputField;
    private GameObject         terminalPanel;
    private GameObject         storyPanel;
    private TMP_Text           storyText;
    private GameObject         standbyCursor;

    [Header("=== Timing ===")]
    public float storyTypeSpeed = 0.03f;
    public float lineDelay      = 0.3f;
    public float frameDuration  = 3.5f;

    private string playerName = "";
    private bool   waitingForInput = false;
    private string pendingInput    = "";

    private SubjectDataModel targetSubject;
    private SubjectDataModel distractor1;
    private SubjectDataModel distractor2;
    private SubjectDataModel selectedSubject;

    private SubjectDataModel _selectionResult;
    private bool            _selectionWrong;

    // ── CONSTANTS ────────────────────────────────────────────────
    private const string OpeningCombinedText =
        "[ SISTEM ALPHA-SECTOR v2.1 ]\n" +
        "Anda adalah Verifier yang bertugas mengidentifikasi\nwarga asli dari ancaman Anomali.\n\n" +
        "Setiap warga yang mencurigakan harus diperiksa\ndata dirinya sebelum mendapat dokumen resmi.\n\n" +
        "Tugas pertama Anda akan menuntun langkah\ndemi langkah. Ikuti instruksi dengan saksama.";

    private const string ConfirmQuestionText = "Apakah kamu mendengar suaraku?";
    private const string FolderInstruction   = "Sekarang, buka folder data untuk memulai.";
    private const string Select1Instruction  = "Pilih subject dengan mengetik NAMAnya di terminal:";
    private const string Select2Instruction  = "Konfirmasi pilihanmu. Pilih orang yang SAMA:";
    private const string PrintInstruction    = "Ketik 'print' untuk mencetak data subject terpilih.";


    private const string Selection1Error = "Nama tidak ditemukan. Coba lagi.";
    private const string Selection2Wrong = "Bukan orang yang tadi kamu pilih. Mulai dari awal seleksi.";
    private const string PrintError      = "Ketik 'print' untuk mencetak data.";
    private const string ConfirmError    = "Ketik 'confirm' untuk melanjutkan.";

    // ── SUBJECT IDS ──────────────────────────────────────────────
    private const string TargetSubjectId    = "S-0042";
    private const string Distractor1Id     = "S-0044";
    private const string Distractor2Id     = "S-0043";

    private void Awake()
    {
        terminalController = FindFirstObjectByType<TerminalController>();

        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null)
        {
            Transform canvasTransform = canvasObj.transform;

            Transform screensaverTx = canvasTransform.Find("Screensaver_Panel");
            if (screensaverTx != null) screensaverPanel = screensaverTx.gameObject;

            Transform storyTx = canvasTransform.Find("Story_Panel");
            if (storyTx != null)
            {
                storyPanel = storyTx.gameObject;
                storyText  = storyPanel.GetComponentInChildren<TMP_Text>();
            }

            Transform terminalTx = canvasTransform.Find("Terminal_Panel");
            if (terminalTx != null)
            {
                terminalPanel    = terminalTx.gameObject;
                playerInputField = terminalPanel.GetComponentInChildren<TMP_InputField>();

                foreach (Transform child in terminalPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "BlinkingChat")
                    {
                        standbyCursor = child.gameObject;
                        break;
                    }
                }
            }
        }
    }

    private void Start()
    {
        if (!ValidateRefs()) return;

        targetSubject = terminalController.GetSubjectDataById(TargetSubjectId);
        distractor1   = terminalController.GetSubjectDataById(Distractor1Id);
        distractor2   = terminalController.GetSubjectDataById(Distractor2Id);

        if (targetSubject == null || distractor1 == null || distractor2 == null)
        {
            Debug.LogError("[OpeningSequence] Gagal mengambil subject dari database!");
            return;
        }

        screensaverPanel.SetActive(true);
        storyPanel.SetActive(false);
        terminalPanel.SetActive(true);
        terminalController.inputBlocked = true;
        if (standbyCursor != null) standbyCursor.SetActive(true);
        playerInputField.gameObject.SetActive(false);

        terminalController.HideAllTutorialPanels();

        StartCoroutine(RunSequence());
    }

    private void Update()
    {
        if (!waitingForInput) return;
        if (!Input.GetKeyDown(KeyCode.Return)) return;

        string input = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        pendingInput = input;
        waitingForInput = false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  MASTER SEQUENCE
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunSequence()
    {
        yield return StartCoroutine(RunBootEffect());
        yield return StartCoroutine(RunOpeningText());
        yield return StartCoroutine(RunNameInput());
        yield return StartCoroutine(RunIyaTidak());
        yield return StartCoroutine(RunFolderPhase());
        yield return StartCoroutine(RunSelectionPhase());
        yield return StartCoroutine(RunPrintPhase());
        yield return StartCoroutine(RunFinalText());
        yield return StartCoroutine(ActivateGameplay());
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Boot screensaver blink
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunBootEffect()
    {
        yield return new WaitForSeconds(0.4f);

        screensaverPanel.SetActive(false); yield return new WaitForSeconds(0.08f);
        screensaverPanel.SetActive(true);  yield return new WaitForSeconds(0.05f);
        screensaverPanel.SetActive(false); yield return new WaitForSeconds(0.12f);
        screensaverPanel.SetActive(true);  yield return new WaitForSeconds(0.04f);

        screensaverPanel.SetActive(false);
        storyPanel.SetActive(true);
        storyText.text = "";

        yield return new WaitForSeconds(0.5f);
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Opening combined text
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunOpeningText()
    {
        yield return StartCoroutine(TypeStoryText(OpeningCombinedText, append: false));
        yield return new WaitForSeconds(frameDuration);
        storyText.text = "";
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Name input
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunNameInput()
    {
        yield return StartCoroutine(TypeStoryText("Masukkan nama kamu:", append: false));
        yield return StartCoroutine(WaitForInputRaw());
        playerName = pendingInput;

        storyText.text = "";
        string confirm = "Identitas terverifikasi.\nSelamat bekerja, Verifier " + playerName + ".";
        yield return StartCoroutine(TypeStoryText(confirm, append: false));
        yield return new WaitForSeconds(lineDelay);
        storyText.text = "";
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Iya / Tidak dengan UI panel
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunIyaTidak()
    {
        yield return StartCoroutine(TypeStoryText(ConfirmQuestionText, append: false));
        terminalController.ShowIyaTidakPanel();

        while (true)
        {
            yield return StartCoroutine(WaitForInputRaw());
            if (pendingInput.ToLower() == "iya") break;
            if (pendingInput.ToLower() == "tidak")
            {
                terminalController.AddLine("Coba dengarkan baik-baik...", TerminalLineType.Warning);
                continue;
            }
            terminalController.AddLine("Ketik 'iya' atau 'tidak'.", TerminalLineType.Error);
        }

        terminalController.HideIyaTidakPanel();
        storyText.text = "";
        yield return StartCoroutine(TypeStoryText("Baik, kita mulai.", append: false));
        yield return new WaitForSeconds(lineDelay);
        storyText.text = "";
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Folder — story clear, baru show icon
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunFolderPhase()
    {
        yield return StartCoroutine(TypeStoryText(FolderInstruction, append: false));

        storyText.text = "";
        terminalController.ShowFolderIcon();

        while (true)
        {
            yield return StartCoroutine(WaitForInputRaw());
            if (pendingInput.ToLower() == "open") break;
            terminalController.AddLine("Ketik 'open' untuk membuka folder.", TerminalLineType.Error);
        }

        terminalController.HideFolderIcon();
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Selection 1 → Selection 2 (loop back on wrong)
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunSelectionPhase()
    {
        yield return StartCoroutine(RunSingleSelection(targetSubject, distractor1, Select1Instruction, null));
        if (_selectionWrong) yield break;
        SubjectDataModel currentTarget = _selectionResult;

        while (true)
        {
            yield return StartCoroutine(RunSingleSelection(currentTarget, distractor2, Select2Instruction, Selection2Wrong));

            if (!_selectionWrong)
            {
                terminalController.AddLine("Konfirmasi diterima: " + _selectionResult.fullNameString, TerminalLineType.Response);
                selectedSubject = _selectionResult;
                break;
            }

            terminalController.AddLine("Kembali ke seleksi awal...", TerminalLineType.Warning);
            yield return new WaitForSeconds(lineDelay);

            yield return StartCoroutine(RunSingleSelection(targetSubject, distractor1, Select1Instruction, null));
            if (_selectionWrong) yield break;
            currentTarget = _selectionResult;
        }
    }

    private IEnumerator RunSingleSelection(SubjectDataModel target, SubjectDataModel other, string instruction, string wrongMessage)
    {
        yield return StartCoroutine(TypeStoryText(instruction, append: false));
        storyText.text = "";
        terminalController.ShowPhotoSelection(target, other, instruction);

        _selectionResult = null;
        _selectionWrong = false;

        while (true)
        {
            yield return StartCoroutine(WaitForInputRaw());

            if (pendingInput.Equals(target.fullNameString, StringComparison.OrdinalIgnoreCase))
            {
                _selectionResult = target;
                break;
            }

            if (pendingInput.Equals(other.fullNameString, StringComparison.OrdinalIgnoreCase))
            {
                if (wrongMessage != null)
                {
                    terminalController.AddLine(wrongMessage, TerminalLineType.Error);
                    _selectionWrong = true;
                    break;
                }
                _selectionResult = other;
                break;
            }

            terminalController.AddLine(Selection1Error, TerminalLineType.Error);
        }

        terminalController.HidePhotoSelection();

        if (!_selectionWrong)
        {
            terminalController.AddLine("Subject dipilih: " + _selectionResult.fullNameString, TerminalLineType.Response);
            storyText.text = "";
            yield return new WaitForSeconds(lineDelay);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Print → data preview → confirm
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunPrintPhase()
    {
        yield return StartCoroutine(TypeStoryText(PrintInstruction, append: false));

        while (true)
        {
            yield return StartCoroutine(WaitForInputRaw());
            if (pendingInput.ToLower() == "print") break;
            terminalController.AddLine(PrintError, TerminalLineType.Error);
        }

        storyText.text = "";
        terminalController.ShowDataPanel(selectedSubject);

        while (true)
        {
            yield return StartCoroutine(WaitForInputRaw());
            if (pendingInput.ToLower() == "confirm") break;
            terminalController.AddLine(ConfirmError, TerminalLineType.Error);
        }

        terminalController.HideDataPanel();

        storyText.text = "";
        yield return StartCoroutine(TypeStoryText("Mencetak dokumen...", append: false));
        yield return new WaitForSeconds(lineDelay);
        storyText.text = "";
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Final text
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator RunFinalText()
    {
        yield return StartCoroutine(TypeStoryText(
            "[ SISTEM ] Verifikasi selesai. Lorem ipsum dolor sit amet.\n" +
            "Anomali terus mengintai. Tetap waspada, Verifier " + playerName + ".\n\n" +
            "Bersiaplah untuk kasus berikutnya.",
            append: false
        ));
        yield return new WaitForSeconds(frameDuration);
        storyText.text = "";
    }

    // ══════════════════════════════════════════════════════════════════
    //  PHASE: Activate free gameplay
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator ActivateGameplay()
    {
        yield return new WaitForSeconds(lineDelay);
        if (standbyCursor != null) standbyCursor.SetActive(false);

        terminalController.AddLine("SISTEM VERIFIKASI AKTIF.", TerminalLineType.Response);
        terminalController.AddLine("Ketik 'help' untuk melihat daftar perintah.", TerminalLineType.System);

        terminalController.inputBlocked = false;
        playerInputField.gameObject.SetActive(true);
        terminalController.FocusInput();
        terminalController.StartTutorial();
    }

    // ══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════
    private IEnumerator TypeStoryText(string text, bool append)
    {
        if (!append) storyText.text = "";
        else if (storyText.text != "") storyText.text += "\n";

        foreach (char c in text)
        {
            storyText.text += c;
            yield return new WaitForSeconds(storyTypeSpeed);
        }
    }

    private IEnumerator WaitForInputRaw()
    {
        waitingForInput = true;
        pendingInput = "";
        playerInputField.text = "";
        playerInputField.gameObject.SetActive(true);
        playerInputField.ActivateInputField();
        if (standbyCursor != null) standbyCursor.SetActive(false);

        while (waitingForInput)
            yield return null;

        playerInputField.gameObject.SetActive(false);
        if (standbyCursor != null) standbyCursor.SetActive(true);
        terminalController.AddLine("> " + pendingInput, TerminalLineType.Input);
    }

    private bool ValidateRefs()
    {
        if (screensaverPanel   == null) { Debug.LogError("[OpeningSequence] screensaverPanel tidak ditemukan!"); return false; }
        if (terminalController == null) { Debug.LogError("[OpeningSequence] TerminalController tidak ditemukan!"); return false; }
        if (playerInputField   == null) { Debug.LogError("[OpeningSequence] playerInputField tidak ditemukan!"); return false; }
        if (terminalPanel      == null) { Debug.LogError("[OpeningSequence] terminalPanel tidak ditemukan!"); return false; }
        if (storyPanel         == null) { Debug.LogError("[OpeningSequence] storyPanel tidak ditemukan!"); return false; }
        if (storyText          == null) { Debug.LogError("[OpeningSequence] storyText tidak ditemukan!"); return false; }
        if (standbyCursor      == null) { Debug.LogError("[OpeningSequence] standbyCursor (BlinkingChat) tidak ditemukan!"); return false; }
        return true;
    }
}
