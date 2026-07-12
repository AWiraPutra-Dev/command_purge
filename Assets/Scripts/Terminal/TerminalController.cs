using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class TerminalController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField playerInputFieldComponent;
    [SerializeField] private ScrollRect     outputScrollRectComponent;
    [SerializeField] private Transform      outputContentTransform;

    [Header("Slot foto subject gameplay")]
    [SerializeField] private Image subjectPhotoImageComponent;
    [SerializeField] private Sprite emptyPhotoPlaceholder;

    [Header("Subject & Folder Photos")]
    [SerializeField] private Sprite subject0042Photo;
    [SerializeField] private Sprite subject0043Photo;
    [SerializeField] private Sprite subject0044Photo;
    [SerializeField] private Sprite folderIconSprite;

    [Header("Iya / Tidak Panel")]
    [SerializeField] private GameObject iyaTidakPanelObject;
    [SerializeField] private TMP_Text   iyaOptionTextComponent;
    [SerializeField] private TMP_Text   tidakOptionTextComponent;

    [Header("Confirm Panel — untuk print confirm")]
    [SerializeField] private GameObject confirmPanelObject;
    [SerializeField] private TMP_Text   confirmInstructionTextComponent;

    [Header("Folder Icon")]
    [SerializeField] private GameObject folderIconObject;
    [SerializeField] private Image      folderImageComponent;
    [SerializeField] private TMP_Text   folderLabelTextComponent;

    [Header("Photo Selection Panel")]
    [SerializeField] private GameObject photoSelectionPanelObject;
    [SerializeField] private Image      subjectPhoto1ImageComponent;
    [SerializeField] private Image      subjectPhoto2ImageComponent;
    [SerializeField] private TMP_Text   subjectName1TextComponent;
    [SerializeField] private TMP_Text   subjectName2TextComponent;
    [SerializeField] private TMP_Text   photoInstructionTextComponent;

    [Header("Data Panel")]
    [SerializeField] private GameObject dataPanelObject;
    [SerializeField] private TMP_Text   dataIdTextComponent;
    [SerializeField] private TMP_Text   dataNameTextComponent;
    [SerializeField] private TMP_Text   dataGenderTextComponent;
    [SerializeField] private TMP_Text   dataDobTextComponent;
    [SerializeField] private TMP_Text   dataExpiryTextComponent;

    [Header("Line Prefab")]
    [SerializeField] private GameObject terminalLinePrefabGameObject;

    [Header("Batas Baris Terminal")]
    [SerializeField] private int maxLines = 4;

    [Header("Warna per Tipe Baris")]
    [SerializeField] private Color systemColorValue  = new Color(0.10f, 0.40f, 0.10f);
    [SerializeField] private Color inputColorValue   = new Color(0.20f, 0.80f, 0.20f);
    [SerializeField] private Color responseColorValue= new Color(0.16f, 0.67f, 0.16f);
    [SerializeField] private Color errorColorValue   = new Color(0.70f, 0.15f, 0.15f);
    [SerializeField] private Color warningColorValue = new Color(0.65f, 0.50f, 0.10f);

    [HideInInspector] public bool inputBlocked = false;

    private CommandProcessorService commandProcessorService;
    private List<GameObject> activeLineGameObjectList = new List<GameObject>();

    private void Awake()
    {
        if (outputScrollRectComponent == null)
            outputScrollRectComponent = FindFirstObjectByType<ScrollRect>();

        if (outputContentTransform == null && outputScrollRectComponent != null)
            outputContentTransform = outputScrollRectComponent.content;

        if (playerInputFieldComponent == null)
            playerInputFieldComponent = FindFirstObjectByType<TMP_InputField>();

        ValidateRequiredReferences();

        commandProcessorService = new CommandProcessorService(HandleNewLineAdded);
        commandProcessorService.OnSubjectPhotoChanged += HandleSubjectPhotoChanged;
    }

    private void Start()
    {
        playerInputFieldComponent.onSubmit.AddListener(HandlePlayerSubmittedInput);
        FocusInputField();

        if (subjectPhotoImageComponent != null)
        {
            subjectPhotoImageComponent.sprite = emptyPhotoPlaceholder;
            subjectPhotoImageComponent.gameObject.SetActive(false);
        }

        commandProcessorService.SetSubjectPhoto("S-0042", subject0042Photo);
        commandProcessorService.SetSubjectPhoto("S-0043", subject0043Photo);
        commandProcessorService.SetSubjectPhoto("S-0044", subject0044Photo);
    }

    private void OnDestroy()
    {
        playerInputFieldComponent.onSubmit.RemoveListener(HandlePlayerSubmittedInput);
        if (commandProcessorService != null)
            commandProcessorService.OnSubjectPhotoChanged -= HandleSubjectPhotoChanged;
    }

    public void StartTutorial()
    {
        Debug.Log("[TerminalController] Opening Sequence selesai! Memulai fase tutorial...");
        FocusInputField();
        if (subjectPhotoImageComponent != null)
            subjectPhotoImageComponent.gameObject.SetActive(true);
    }

    // ═══════════════════════════════════════════════
    // DATA SERVICE WRAPPER
    // ═══════════════════════════════════════════════
    public SubjectDataModel GetSubjectDataById(string id)
    {
        return commandProcessorService?.GetSubjectById(id);
    }

    // ═══════════════════════════════════════════════
    // PANEL CONTROLS — HIDE ALL
    // ═══════════════════════════════════════════════
    public void HideAllTutorialPanels()
    {
        if (iyaTidakPanelObject      != null) iyaTidakPanelObject.SetActive(false);
        if (confirmPanelObject       != null) confirmPanelObject.SetActive(false);
        if (folderIconObject         != null) folderIconObject.SetActive(false);
        if (photoSelectionPanelObject!= null) photoSelectionPanelObject.SetActive(false);
        if (dataPanelObject          != null) dataPanelObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // IYA / TIDAK PANEL
    // ═══════════════════════════════════════════════
    public void ShowIyaTidakPanel()
    {
        HideAllTutorialPanels();
        if (iyaTidakPanelObject != null) iyaTidakPanelObject.SetActive(true);
        if (iyaOptionTextComponent != null) iyaOptionTextComponent.gameObject.SetActive(true);
        if (tidakOptionTextComponent != null) tidakOptionTextComponent.gameObject.SetActive(true);
    }

    public void HideIyaTidakPanel()
    {
        if (iyaTidakPanelObject != null) iyaTidakPanelObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // CONFIRM PANEL — untuk print confirm
    // ═══════════════════════════════════════════════
    public void ShowConfirmPanel(string instruction)
    {
        HideAllTutorialPanels();
        if (confirmPanelObject != null) confirmPanelObject.SetActive(true);
        if (confirmInstructionTextComponent != null)
        {
            confirmInstructionTextComponent.text = instruction;
            confirmInstructionTextComponent.gameObject.SetActive(true);
        }
    }

    public void HideConfirmPanel()
    {
        if (confirmPanelObject != null) confirmPanelObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // FOLDER ICON
    // ═══════════════════════════════════════════════
    public void ShowFolderIcon()
    {
        HideAllTutorialPanels();
        if (folderIconObject != null) folderIconObject.SetActive(true);
        if (folderImageComponent != null && folderIconSprite != null)
            folderImageComponent.sprite = folderIconSprite;
    }

    public void HideFolderIcon()
    {
        if (folderIconObject != null) folderIconObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // PHOTO SELECTION PANEL
    // ═══════════════════════════════════════════════
    public void ShowPhotoSelection(SubjectDataModel subject1, SubjectDataModel subject2, string instruction)
    {
        HideAllTutorialPanels();
        if (photoSelectionPanelObject != null) photoSelectionPanelObject.SetActive(true);

        if (photoInstructionTextComponent != null)
            photoInstructionTextComponent.text = instruction;

        if (subjectPhoto1ImageComponent != null)
        {
            if (subject1 != null && subject1.subjectPhoto != null)
                subjectPhoto1ImageComponent.sprite = subject1.subjectPhoto;
            else
                subjectPhoto1ImageComponent.sprite = emptyPhotoPlaceholder;
            subjectPhoto1ImageComponent.enabled = true;
        }

        if (subjectPhoto2ImageComponent != null)
        {
            if (subject2 != null && subject2.subjectPhoto != null)
                subjectPhoto2ImageComponent.sprite = subject2.subjectPhoto;
            else
                subjectPhoto2ImageComponent.sprite = emptyPhotoPlaceholder;
            subjectPhoto2ImageComponent.enabled = true;
        }

        if (subjectName1TextComponent != null)
            subjectName1TextComponent.text = subject1 != null ? subject1.fullNameString : "???";

        if (subjectName2TextComponent != null)
            subjectName2TextComponent.text = subject2 != null ? subject2.fullNameString : "???";
    }

    public void HidePhotoSelection()
    {
        if (photoSelectionPanelObject != null) photoSelectionPanelObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // DATA PANEL
    // ═══════════════════════════════════════════════
    public void ShowDataPanel(SubjectDataModel subject)
    {
        HideAllTutorialPanels();
        if (dataPanelObject != null) dataPanelObject.SetActive(true);

        if (dataIdTextComponent      != null) dataIdTextComponent.text     = "ID       : " + (subject != null ? subject.subjectIdString    : "---");
        if (dataNameTextComponent    != null) dataNameTextComponent.text   = "Nama     : " + (subject != null ? subject.fullNameString    : "---");
        if (dataGenderTextComponent  != null) dataGenderTextComponent.text = "Gender   : " + (subject != null ? subject.genderString      : "---");
        if (dataDobTextComponent     != null) dataDobTextComponent.text    = "Tgl Lahir: " + (subject != null ? subject.dateOfBirthString : "---");
        if (dataExpiryTextComponent  != null) dataExpiryTextComponent.text = "Exp Date : " + (subject != null ? subject.expiryDateString : "---");
    }

    public void HideDataPanel()
    {
        if (dataPanelObject != null) dataPanelObject.SetActive(false);
    }

    public Sprite GetSubjectPhotoById(string id)
    {
        if (id == "S-0042") return subject0042Photo;
        if (id == "S-0043") return subject0043Photo;
        if (id == "S-0044") return subject0044Photo;
        return null;
    }

    // ═══════════════════════════════════════════════
    // INPUT HANDLER
    // ═══════════════════════════════════════════════
    private void HandlePlayerSubmittedInput(string rawInputString)
    {
        if (inputBlocked) return;
        if (string.IsNullOrWhiteSpace(rawInputString)) return;

        commandProcessorService.ProcessCommand(rawInputString);

        playerInputFieldComponent.text = string.Empty;
        playerInputFieldComponent.ActivateInputField();
    }

    private void HandleSubjectPhotoChanged(Sprite photo)
    {
        if (subjectPhotoImageComponent == null) return;
        subjectPhotoImageComponent.sprite = photo != null ? photo : emptyPhotoPlaceholder;
        subjectPhotoImageComponent.enabled = photo != null || emptyPhotoPlaceholder != null;
    }

    // ═══════════════════════════════════════════════
    // TERMINAL LINE MANAGEMENT
    // ═══════════════════════════════════════════════
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
        PruneOldLines();
    }

    private void ClearAllLines()
    {
        foreach (GameObject lineGameObject in activeLineGameObjectList)
            Destroy(lineGameObject);

        activeLineGameObjectList.Clear();
    }

    public TMP_Text SpawnEmptyLine(TerminalLineType type)
    {
        GameObject newLine = Instantiate(terminalLinePrefabGameObject, outputContentTransform);
        TMP_Text   tmpText = newLine.GetComponent<TMP_Text>();

        tmpText.text  = "";
        tmpText.color = GetColorForLineType(type);

        activeLineGameObjectList.Add(newLine);
        PruneOldLines();

        return tmpText;
    }

    public void AddLine(string text, TerminalLineType type)
    {
        HandleNewLineAdded(text, type);
    }

    private void PruneOldLines()
    {
        while (activeLineGameObjectList.Count > maxLines)
        {
            GameObject oldestLine = activeLineGameObjectList[0];
            activeLineGameObjectList.RemoveAt(0);
            Destroy(oldestLine);
        }
    }

    public void ScrollToBottom()
    {
        if (outputScrollRectComponent == null) return;
        Canvas.ForceUpdateCanvases();
        outputScrollRectComponent.verticalNormalizedPosition = 0f;
    }

    public void FocusInput()
    {
        FocusInputField();
    }

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
        if (subjectPhotoImageComponent == null)
            Debug.LogWarning("[TerminalController] subjectPhotoImageComponent belum di-assign!");

        if (iyaTidakPanelObject       == null) Debug.LogWarning("[TerminalController] iyaTidakPanelObject belum di-assign!");
        if (iyaOptionTextComponent    == null) Debug.LogWarning("[TerminalController] iyaOptionTextComponent belum di-assign!");
        if (tidakOptionTextComponent  == null) Debug.LogWarning("[TerminalController] tidakOptionTextComponent belum di-assign!");
        if (confirmPanelObject        == null) Debug.LogWarning("[TerminalController] confirmPanelObject belum di-assign!");
        if (confirmInstructionTextComponent == null) Debug.LogWarning("[TerminalController] confirmInstructionTextComponent belum di-assign!");
        if (folderIconObject          == null) Debug.LogWarning("[TerminalController] folderIconObject belum di-assign!");
        if (photoSelectionPanelObject == null) Debug.LogWarning("[TerminalController] photoSelectionPanelObject belum di-assign!");
        if (dataPanelObject           == null) Debug.LogWarning("[TerminalController] dataPanelObject belum di-assign!");
        if (dataGenderTextComponent   == null) Debug.LogWarning("[TerminalController] dataGenderTextComponent belum di-assign!");
    }
}
