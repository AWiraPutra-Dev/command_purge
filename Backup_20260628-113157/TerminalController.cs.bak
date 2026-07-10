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

    private CommandProcessorService commandProcessorService;
    private List<GameObject> activeLineGameObjectList = new List<GameObject>();

    private void Awake()
    {
        ValidateRequiredReferences();
        commandProcessorService = new CommandProcessorService(HandleNewLineAdded);
    }

    private void Start()
    {
        playerInputFieldComponent.onSubmit.AddListener(HandlePlayerSubmittedInput);
        PrintBootSequence();
        FocusInputField();
    }

    private void OnDestroy()
    {
        playerInputFieldComponent.onSubmit.RemoveListener(HandlePlayerSubmittedInput);
    }

    // Input handling

    private void HandlePlayerSubmittedInput(string rawInputString)
    {
        if (string.IsNullOrWhiteSpace(rawInputString)) return;

        commandProcessorService.ProcessCommand(rawInputString);

        playerInputFieldComponent.text = string.Empty;
        playerInputFieldComponent.ActivateInputField();
    }


    // Output handling — manggil commandproses


    private void HandleNewLineAdded(string textString, TerminalLineType lineTypeEnum)
    {
        // Sinyal khusus untuk clear layar
        if (textString == "__CLEAR__")
        {
            ClearAllLines();
            return;
        }

        SpawnTerminalLine(textString, lineTypeEnum);
        ScrollToBottomOfOutput();
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

    private void ScrollToBottomOfOutput()
    {
        // Perlu delay 1 frame agar layout sudah di-rebuild sebelum scroll
        Canvas.ForceUpdateCanvases();
        outputScrollRectComponent.verticalNormalizedPosition = 0f;
    }

    private void FocusInputField()
    {
        playerInputFieldComponent.Select();
        playerInputFieldComponent.ActivateInputField();
    }


    // Boot sequence — pesan awal terminal aktif

    private void PrintBootSequence()
    {
        HandleNewLineAdded("Initializing ALPHA-SECTOR OS...",              TerminalLineType.System);
        HandleNewLineAdded("Loading verification protocols...",            TerminalLineType.System);
        HandleNewLineAdded("Connection established.",                      TerminalLineType.System);
        HandleNewLineAdded("",                                             TerminalLineType.System);
        HandleNewLineAdded("Selamat datang, Verifier. Shift dimulai.",     TerminalLineType.Response);
        HandleNewLineAdded(". Warning Testing Line. ", TerminalLineType.Warning);
        HandleNewLineAdded("",                                             TerminalLineType.System);
        HandleNewLineAdded("Ketik  help  untuk daftar perintah.",          TerminalLineType.System);
    }


    // Helpers


    private Color GetColorForLineType(TerminalLineType lineTypeEnum)
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