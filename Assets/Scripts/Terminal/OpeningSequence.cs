using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Opening sequence dengan dua mode:
/// 1. Terminal mode  — boot sequence, linux style
/// 2. Story mode     — frame per frame, auto-advance, teks di tengah
///
/// CARA GANTI KONTEN:
/// - storyFrames[] = ganti teks tiap frame
/// - frameDuration  = berapa lama tiap frame ditampilkan (detik)
/// </summary>
public class OpeningSequence : MonoBehaviour
{
    // ─── References ───────────────────────────────────────────
    [Header("=== Terminal References ===")]
    public TerminalController terminalController;
    public TMP_InputField     playerInputField;
    public GameObject         terminalPanel;      // Canvas/Terminal_Panel

    [Header("=== Story References ===")]
    public GameObject storyPanel;   // Panel hitam fullscreen
    public TMP_Text   storyText;    // TMP text di tengah story panel

    // ─── Timing ───────────────────────────────────────────────
    [Header("=== Timing ===")]
    public float typeSpeed      = 0.03f;   // kecepatan typewriter terminal
    public float lineDelay      = 0.3f;    // jeda antar baris terminal
    public float sectionDelay   = 0.8f;    // jeda antar section terminal

    public float storyTypeSpeed = 0.04f;   // kecepatan typewriter story
    public float frameDuration  = 3.5f;    // lama frame ditampilkan setelah selesai ketik


    // ─── Internal ─────────────────────────────────────────────
    private string playerName  = "";
    private bool   waitingForName = false;

    // =========================================================
    //  [FIXED] BOOT SEQUENCE — terminal style
    // =========================================================
    private readonly (string text, TerminalLineType type)[] bootLines =
    {
        ("Initializing ALPHA-SECTOR OS...",   TerminalLineType.System),
        ("Loading verification protocols...", TerminalLineType.System),
        ("Running security check...",         TerminalLineType.System),
        ("Connection established.",           TerminalLineType.System),
        ("",                                  TerminalLineType.System),
    };

    private const string WelcomeText    = "Selamat datang, Verifier. Shift dimulai.";
    private const string NamePromptText = "Sebelum kita mulai, masukkan nama kamu:";
    private const string NameConfirmText= "Identitas terverifikasi. Selamat bertugas, {NAME}.";

    // =========================================================
    //  [PLACEHOLDER] STORY FRAMES — frame per frame, auto-advance
    //  Ganti teks tiap frame sesuai cerita dari temen
    //  Bisa multi-baris pakai \n
    // =========================================================
    private readonly string[] storyFrames =
    {
        // Frame 1 — latar belakang
        "Lorem ipsum dolor sit amet,\nconsectetur adipiscing elit.\n\nSed do eiusmod tempor incididunt\nut labore et dolore magna aliqua.",

        // Frame 2 — kondisi dunia / situasi
        "Lorem ipsum dolor sit amet,\nconsectetur adipiscing elit.\n\nDuis aute irure dolor in reprehenderit\nin voluptate velit esse cillum.",

        // Frame 3 — tugas player
        "Tugasmu:\n\n> Lorem ipsum: [TUGAS 1]\n> Lorem ipsum: [TUGAS 2]\n> Lorem ipsum: [TUGAS 3]",

        // Frame 4 — peringatan / outro
        "Lorem ipsum dolor sit amet.\n\nIkuti protokol.\nJangan bertanya lebih dari yang perlu.\n\nGod bless you.",
    };

    // ─────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────
    private void Start()
    {
        if (!ValidateRefs()) return;

        // Sembunyiin story panel di awal
        storyPanel.SetActive(false);
        storyText.text = "";

        // Block input
        terminalController.inputBlocked = true;
        playerInputField.gameObject.SetActive(false);

        StartCoroutine(RunSequence());
    }

    private void Update()
    {
        if (waitingForName && Input.GetKeyDown(KeyCode.Return))
        {
            string input = playerInputField.text.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                playerName    = input;
                waitingForName = false;
                playerInputField.gameObject.SetActive(false);
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // Main Sequence
    // ─────────────────────────────────────────────────────────
    private IEnumerator RunSequence()
    {
        // ── 1. BOOT (terminal) ────────────────────────────────
        foreach (var line in bootLines)
            yield return StartCoroutine(TypeTerminalLine(line.text, line.type));

        yield return new WaitForSeconds(sectionDelay);

        // ── CLEAR — hapus boot text, bersih sebelum nama prompt
        terminalController.AddLine("__CLEAR__", TerminalLineType.System);
        yield return new WaitForSeconds(0.3f);

        // ── 2. WELCOME + INPUT NAMA (terminal) ───────────────
        yield return StartCoroutine(TypeTerminalLine(WelcomeText, TerminalLineType.Response));
        yield return new WaitForSeconds(lineDelay);
        yield return StartCoroutine(TypeTerminalLine(NamePromptText, TerminalLineType.System));
        yield return StartCoroutine(WaitForPlayerName());

        string confirm = NameConfirmText.Replace("{NAME}", playerName);
        yield return StartCoroutine(TypeTerminalLine(confirm, TerminalLineType.Response));
        yield return new WaitForSeconds(sectionDelay);

        // ── 3. SWITCH KE STORY MODE ───────────────────────────
        // Story_Panel fullscreen hitam jadi nutup terminal otomatis
        // Terminal_Panel tetap aktif biar coroutine tidak mati
        storyPanel.SetActive(true);

        // ── 4. STORY FRAMES (auto-advance) ───────────────────
        foreach (string frame in storyFrames)
        {
            yield return StartCoroutine(ShowStoryFrame(frame));
        }

        // ── 5. BALIK KE TERMINAL ──────────────────────────────
        storyPanel.SetActive(false);
        storyText.text = "";

        // ── 6. PESAN AKHIR + BUKA INPUT ──────────────────────
        yield return new WaitForSeconds(sectionDelay);
        terminalController.AddLine("Ketik  help  untuk daftar perintah.", TerminalLineType.System);

        terminalController.inputBlocked = false;
        playerInputField.gameObject.SetActive(true);
        terminalController.FocusInput();

        Debug.Log("[OpeningSequence] Selesai.");
    }

    // ─────────────────────────────────────────────────────────
    // Story Frame:typewriter → tunggu 
    // ─────────────────────────────────────────────────────────
    private IEnumerator ShowStoryFrame(string text)
    {
        storyText.text  = "";
        storyText.alpha = 1f;

        // Typewriter
        foreach (char c in text)
        {
            storyText.text += c;
            yield return new WaitForSeconds(storyTypeSpeed);
        }

        // Tahan beberapa detik
        yield return new WaitForSeconds(frameDuration);


        storyText.text = "";
    }


    // ─────────────────────────────────────────────────────────
    // Terminal: typewriter per baris
    // ─────────────────────────────────────────────────────────
    private IEnumerator TypeTerminalLine(string text, TerminalLineType type)
    {
        if (string.IsNullOrEmpty(text))
        {
            terminalController.AddLine("", type);
            yield return new WaitForSeconds(lineDelay * 0.5f);
            yield break;
        }

        TMP_Text lineText = terminalController.SpawnEmptyLine(type);
        foreach (char c in text)
        {
            lineText.text += c;
            terminalController.ScrollToBottom();
            yield return new WaitForSeconds(typeSpeed);
        }

        yield return new WaitForSeconds(lineDelay);
    }

    // ─────────────────────────────────────────────────────────
    // Tunggu player input nama
    // ─────────────────────────────────────────────────────────
    private IEnumerator WaitForPlayerName()
    {
        playerInputField.text = "";
        playerInputField.gameObject.SetActive(true);
        playerInputField.ActivateInputField();

        waitingForName = true;
        while (waitingForName)
            yield return null;

        terminalController.AddLine("> " + playerName, TerminalLineType.Input);
        yield return new WaitForSeconds(lineDelay);
    }

    // ─────────────────────────────────────────────────────────
    // Validasi references
    // ─────────────────────────────────────────────────────────
    private bool ValidateRefs()
    {
        if (terminalController == null) { Debug.LogError("[OS] terminalController null!"); return false; }
        if (playerInputField   == null) { Debug.LogError("[OS] playerInputField null!");   return false; }
        if (terminalPanel      == null) { Debug.LogError("[OS] terminalPanel null!");      return false; }
        if (storyPanel         == null) { Debug.LogError("[OS] storyPanel null!");         return false; }
        if (storyText          == null) { Debug.LogError("[OS] storyText null!");          return false; }
        return true;
    }
}