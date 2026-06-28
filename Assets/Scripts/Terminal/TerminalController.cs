using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
 
/// <summary>
/// Controller utama terminal. Tugasnya:
/// 1. Terima input dari player via TMP_InputField
/// 2. Kirim ke CommandProcessorService
/// 3. Tampilkan hasilnya sebagai baris teks di scroll view
/// </summary>
public class TerminalController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField playerInputFieldComponent;
    [SerializeField] private ScrollRect     outputScrollRectComponent;
    [SerializeField] private Transform      outputContentTransform;
 
    [Header("Line Prefab")]
    [SerializeField] private GameObject terminalLinePrefabGameObject;
 
    [Header("Warna per Tipe Baris")]
    [SerializeField] private Color systemColorValue  = new Color(0.10f, 0.40f, 0.10f);
    [SerializeField] private Color inputColorValue   = new Color(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color responseColorValue= new Color(0.16f, 0.67f, 0.16f);
    [SerializeField] private Color errorColorValue   = new Color(0.70f, 0.15f, 0.15f);
    [SerializeField] private Color warningColorValue = new Color(0.65f, 0.50f, 0.10f);
 
    // ── Dipakai OpeningSequence untuk block input saat intro ──
    [HideInInspector] public bool inputBlocked = false;
 
    private CommandProcessorService commandProcessorService;
    private List<GameObject> activeLineGameObjectList = new List<GameObject>();
 
    private void Awake()
    {
        // Auto-find semua scene references jika null
        // Ini fix untuk cross-prefab reference issue di Unity
        if (outputScrollRectComponent == null)
        {
            outputScrollRectComponent = FindFirstObjectByType<ScrollRect>();
            if (outputScrollRectComponent != null)
                Debug.Log("[TC] ScrollRect auto-found: " + outputScrollRectComponent.name);
        }
 
        if (outputContentTransform == null && outputScrollRectComponent != null)
        {
            outputContentTransform = outputScrollRectComponent.content;
            Debug.Log("[TC] Content auto-resolved dari ScrollRect");
        }
 
        if (playerInputFieldComponent == null)
        {
            playerInputFieldComponent = FindFirstObjectByType<TMP_InputField>();
            if (playerInputFieldComponent != null)
                Debug.Log("[TC] InputField auto-found: " + playerInputFieldComponent.name);
        }
 
        ValidateRequiredReferences();
        commandProcessorService = new CommandProcessorService(HandleNewLineAdded);
    }
 
    private void Start()
    {
        playerInputFieldComponent.onSubmit.AddListener(HandlePlayerSubmittedInput);
        // PrintBootSequence() dihapus dari sini —
        // sekarang ditangani oleh OpeningSequence
        FocusInputField();
    }
 
    private void OnDestroy()
    {
        playerInputFieldComponent.onSubmit.RemoveListener(HandlePlayerSubmittedInput);
    }
 
    // ── Input handling ─────────────────────────────────────────
 
    private void HandlePlayerSubmittedInput(string rawInputString)
    {
        // Blok input saat opening sequence berjalan
        if (inputBlocked) return;
        if (string.IsNullOrWhiteSpace(rawInputString)) return;
 
        commandProcessorService.ProcessCommand(rawInputString);
 
        playerInputFieldComponent.text = string.Empty;
        playerInputFieldComponent.ActivateInputField();
    }
 
    // ── Output handling ────────────────────────────────────────
 
    private void HandleNewLineAdded(string textString, TerminalLineType lineTypeEnum)
    {
        if (textString == "__CLEAR__")
        {
            ClearAllLines();
            return;
        }
 
        SpawnTerminalLine(textString, lineTypeEnum);
        ScrollToBottom();
    }
 
    private void SpawnTerminalLine(string textString, TerminalLineType lineTypeEnum)
    {
        GameObject newLineGameObject = Instantiate(terminalLinePrefabGameObject, outputContentTransform);
        TMP_Text   lineTextComponent = newLineGameObject.GetComponent<TMP_Text>();
 
        lineTextComponent.text  = textString;
        lineTextComponent.color = GetColorForLineType(lineTypeEnum);
 
        activeLineGameObjectList.Add(newLineGameObject);
    }
 
    private void ClearAllLines()
    {
        foreach (GameObject lineGameObject in activeLineGameObjectList)
            Destroy(lineGameObject);
 
        activeLineGameObjectList.Clear();
    }
 
    // ── Public API untuk OpeningSequence ──────────────────────
 
    /// <summary>
    /// Spawn line prefab kosong dengan warna sesuai tipe.
    /// OpeningSequence akan isi teks nya karakter per karakter.
    /// </summary>
    public TMP_Text SpawnEmptyLine(TerminalLineType type)
    {
        if (terminalLinePrefabGameObject == null)
            Debug.LogError("[TC] terminalLinePrefabGameObject NULL!");
        if (outputContentTransform == null)
            Debug.LogError("[TC] outputContentTransform NULL! Re-assign Content di Inspector.");
 
        GameObject newLine = Instantiate(terminalLinePrefabGameObject, outputContentTransform);
        TMP_Text   tmpText = newLine.GetComponent<TMP_Text>();
 
        tmpText.text  = "";
        tmpText.color = GetColorForLineType(type);
 
        activeLineGameObjectList.Add(newLine);
        return tmpText;
    }
 
    /// <summary>Tambah satu baris langsung tanpa typewriter.</summary>
    public void AddLine(string text, TerminalLineType type)
    {
        HandleNewLineAdded(text, type);
    }
 
    /// <summary>Scroll output ke paling bawah.</summary>
    public void ScrollToBottom()
    {
        if (outputScrollRectComponent == null) return;
        Canvas.ForceUpdateCanvases();
        outputScrollRectComponent.verticalNormalizedPosition = 0f;
    }
 
    /// <summary>Fokuskan input field ke player.</summary>
    public void FocusInput()
    {
        FocusInputField();
    }
 
    // ── Helpers ───────────────────────────────────────────────
 
    private void FocusInputField()
    {
        playerInputFieldComponent.Select();
        playerInputFieldComponent.ActivateInputField();
    }
 
    public Color GetColorForLineType(TerminalLineType lineTypeEnum)
    {
        switch (lineTypeEnum)
        {
            case TerminalLineType.System:   return systemColorValue;
            case TerminalLineType.Input:    return inputColorValue;
            case TerminalLineType.Response: return responseColorValue;
            case TerminalLineType.Error:    return errorColorValue;
            case TerminalLineType.Warning:  return warningColorValue;
            default:                        return responseColorValue;
        }
    }
 
    private void ValidateRequiredReferences()
    {
        if (playerInputFieldComponent == null)
            Debug.LogError("[TerminalController] playerInputFieldComponent belum di-assign!");
        if (outputScrollRectComponent == null)
            Debug.LogError("[TerminalController] outputScrollRectComponent belum di-assign!");
        if (outputContentTransform == null)
            Debug.LogError("[TerminalController] outputContentTransform belum di-assign!");
        if (terminalLinePrefabGameObject == null)
            Debug.LogError("[TerminalController] terminalLinePrefabGameObject belum di-assign!");
    }
}