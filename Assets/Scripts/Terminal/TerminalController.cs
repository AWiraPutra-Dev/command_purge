using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

    [Header("Inspect Panel — foto zoom + rotate")]
    [SerializeField] private GameObject inspectPanelObject;
    [SerializeField] private Image      inspectPhotoImageComponent;
    [SerializeField] private TMP_Text   inspectCounterTextComponent;
    [SerializeField] private GameObject inspectLeftArrowObject;
    [SerializeField] private GameObject inspectRightArrowObject;
    [Tooltip("Sprite foto aneh/glitch buat efek visual kasus Climax saat rotate.")]
    [SerializeField] private Sprite     glitchPhotoSprite;

    [Header("Battery UI — fill image di AskInfoCheck Panel")]
    [Tooltip("Assign Battery_Fill Image dari Hierarchy. Fill Method: Horizontal, Fill Origin: Left.")]
    [SerializeField] private Image batteryFillImage;
    [Tooltip("Assign Battery_Frame GameObject dari Hierarchy untuk menyembunyikan baterai saat tutorial.")]
    [SerializeField] private GameObject batteryFrameObject;

    [Header("Loading Panel — animasi print")]
    [SerializeField] private GameObject loadingPanelObject;
    [SerializeField] private RectTransform loadingBarRectTransform;
    [SerializeField] private float       loadingDuration = 3f;

    public float GetPrintDuration() => Mathf.Max(0.1f, loadingDuration);

    [Header("Type Test Panel — robot check, ghost text ala web typing-test")]
    [SerializeField] private GameObject typeTestPanelObject;
    [SerializeField] private TMP_Text   typeTestPromptTextComponent;
    [Tooltip("TMP_Text buat nampilin target teks dengan warna per-karakter (benar/salah/belum diketik).")]
    [SerializeField] private TMP_Text   typeTestGhostTextComponent;
    [SerializeField] private Color typeTestPendingColorValue = new Color(0.35f, 0.42f, 0.35f);
    [SerializeField] private Color typeTestCorrectColorValue = new Color(0.35f, 0.85f, 0.35f);
    [SerializeField] private Color typeTestWrongColorValue   = new Color(0.85f, 0.25f, 0.25f);

    private string typeTestTargetPhrase = "";

    [Header("Folder Notification — tanda kasus baru")]
    [SerializeField] private GameObject folderNotificationObject;

    [Header("Ask / Info / Check Panel — menu utama investigasi")]
    [SerializeField] private GameObject askInfoCheckPanelObject;
    [SerializeField] private TMP_Text   askInfoCheckTitleText;
    [SerializeField] private TMP_Text   askInfoCheckActionsText;
    [SerializeField] private TMP_Text   askInfoCheckVerdictText;
    [SerializeField] private TMP_Text   askInfoCheckHintText;
    [SerializeField] private Image      askInfoCheckPhotoImage;

    [Header("Ask Question List Panel — dedicated panel baru, replace AskInfoCheck saat 'ask'")]
    [SerializeField] private GameObject askQuestionListPanelObject;
    [SerializeField] private TMP_Text   askQuestionListTitleText;
    [SerializeField] private Transform  askQuestionListContainer;
    [SerializeField] private GameObject askQuestionItemPrefab;
    [SerializeField] private TMP_Text   askQuestionResponseText;

    [Header("Traits Panel — kriteria anomali, SCALABLE, prefab di-instantiate, reveal satu-satu")]
    [SerializeField] private GameObject traitsPanelObject;
    [SerializeField] private Transform  traitsListContainer;
    [SerializeField] private GameObject traitsItemPrefab;

    [Header("Verdict Panel — hasil approved / denied")]
    [SerializeField] private GameObject verdictPanelObject;
    [SerializeField] private TMP_Text   verdictTextComponent;

    [Header("Staggered Reveal — buat boot text & list dinamis (traits/ask)")]
    [Tooltip("Opsional. Kalau kosong, reveal tetap jalan tanpa suara.")]
    [SerializeField] private AudioSource sfxAudioSourceComponent;
    [SerializeField] private AudioClip   revealTingClip;
    [SerializeField] private AudioClip   notificationSfxClip;
    [SerializeField] private float       revealStaggerDelay = 0.12f;

    [Header("Line Prefab")]
    [SerializeField] private GameObject terminalLinePrefabGameObject;

    [Header("Subject Definitions (4 case utama) — drag asset ke sini")]
    [SerializeField] private List<SubjectDefinition> subjectDefinitionList = new List<SubjectDefinition>();

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
        commandProcessorService.SetSubjectDefinitionList(subjectDefinitionList);

        if (playerInputFieldComponent != null)
            playerInputFieldComponent.onValueChanged.AddListener(HandleInputValueChangedForTypeTest);

        SetupAutoRefocus();

        // Auto-refocus: klik di panel manapun → balik ke terminal input
        AddClickRefocus(askInfoCheckPanelObject);
        AddClickRefocus(dataPanelObject);
        AddClickRefocus(inspectPanelObject);
        AddClickRefocus(traitsPanelObject);
        AddClickRefocus(verdictPanelObject);
        AddClickRefocus(typeTestPanelObject);
        AddClickRefocus(confirmPanelObject);
        AddClickRefocus(folderNotificationObject);
        AddClickRefocus(photoSelectionPanelObject);
        AddClickRefocus(iyaTidakPanelObject);
        AddClickRefocus(loadingPanelObject);
        AddClickRefocus(askQuestionListPanelObject);
    }

    // OPSI A: klik di manapun (panel/UI/layar) → fokus otomatis balik ke terminal,
    // biar player gak kehilangan kemampuan ngetik gara-gara klik UI.
    private void SetupAutoRefocus()
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        EventTrigger trigger = rootCanvas.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = rootCanvas.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => FocusInputField());
        trigger.triggers.Add(entry);
    }

    // TAMBAHAN: klik di panel manapun → fokus balik ke terminal input.
    // Dipanggil di Awake() untuk semua panel aktif.
    private void AddClickRefocus(GameObject panel)
    {
        if (panel == null) return;
        EventTrigger trigger = panel.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = panel.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => FocusInputField());
        trigger.triggers.Add(entry);
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

    private void Update()
    {
        KeepInputFieldFocused();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            KeepInputFieldFocused();
        }
    }

    public void KeepInputFieldFocused()
    {
        if (inputBlocked) return;
        if (playerInputFieldComponent == null) return;

        if (!playerInputFieldComponent.isFocused)
        {
            playerInputFieldComponent.ActivateInputField();
        }
    }

    private void OnDestroy()
    {
        playerInputFieldComponent.onSubmit.RemoveListener(HandlePlayerSubmittedInput);
        if (playerInputFieldComponent != null)
            playerInputFieldComponent.onValueChanged.RemoveListener(HandleInputValueChangedForTypeTest);
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
        if (inspectPanelObject       != null) inspectPanelObject.SetActive(false);
        if (loadingPanelObject       != null) loadingPanelObject.SetActive(false);
        if (typeTestPanelObject      != null) typeTestPanelObject.SetActive(false);
        if (folderNotificationObject  != null) folderNotificationObject.SetActive(false);
        if (askInfoCheckPanelObject   != null) askInfoCheckPanelObject.SetActive(false);
        if (askQuestionListPanelObject != null) askQuestionListPanelObject.SetActive(false);
        if (traitsPanelObject         != null) traitsPanelObject.SetActive(false);
        if (verdictPanelObject        != null) verdictPanelObject.SetActive(false);
        
        SetBatteryVisibility(false);
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
    // DATA PANEL (INFO MODE) — disamakan dengan tema ASK (foto & panel background tetap aktif)
    // ═══════════════════════════════════════════════
    public void ShowDataPanel(SubjectDataModel subject)
    {
        HideAllTutorialPanels();

        // Pastikan AskInfoCheck panel tetap aktif (foto subjek di background tetap terlihat)
        if (askInfoCheckPanelObject != null) askInfoCheckPanelObject.SetActive(true);

        Sprite photo = (subject != null && subject.subjectPhoto != null)
            ? subject.subjectPhoto
            : GetSubjectPhotoById(subject?.subjectIdString);
        SetAskInfoCheckPhoto(photo);

        // Sembunyikan teks bawaan AskInfoCheck agar tidak bertumpuk dengan DataPanel
        if (askInfoCheckTitleText   != null) askInfoCheckTitleText.gameObject.SetActive(false);
        if (askInfoCheckActionsText != null) askInfoCheckActionsText.gameObject.SetActive(false);
        if (askInfoCheckVerdictText != null) askInfoCheckVerdictText.gameObject.SetActive(false);
        if (askInfoCheckHintText    != null) askInfoCheckHintText.gameObject.SetActive(false);

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

        // Munculkan kembali teks bawaan AskInfoCheck jika kembali ke menu utama
        if (askInfoCheckTitleText   != null) askInfoCheckTitleText.gameObject.SetActive(true);
        if (askInfoCheckActionsText != null) askInfoCheckActionsText.gameObject.SetActive(true);
        if (askInfoCheckVerdictText != null) askInfoCheckVerdictText.gameObject.SetActive(true);
        if (askInfoCheckHintText    != null) askInfoCheckHintText.gameObject.SetActive(true);
    }

    public void SetAskQuestionResponse(string responseText)
    {
        if (askQuestionResponseText != null)
        {
            askQuestionResponseText.text = responseText;
            askQuestionResponseText.gameObject.SetActive(!string.IsNullOrEmpty(responseText));
        }
    }

    public Sprite GetSubjectPhotoById(string id)
    {
        if (id == "S-0042") return subject0042Photo;
        if (id == "S-0043") return subject0043Photo;
        if (id == "S-0044") return subject0044Photo;
        return null;
    }

    // ═══════════════════════════════════════════════
    // INSPECT PANEL — foto zoom + rotate
    // ═══════════════════════════════════════════════
    public void ShowInspectPanel(Sprite[] frames, int currentIndex)
    {
        HideAllTutorialPanels();
        if (inspectPanelObject != null) inspectPanelObject.SetActive(true);
        UpdateInspectPhoto(frames, currentIndex);
    }

    public void UpdateInspectPhoto(Sprite[] frames, int currentIndex)
    {
        if (frames == null || frames.Length == 0) return;

        if (inspectPhotoImageComponent != null)
            inspectPhotoImageComponent.sprite = frames[currentIndex];

        if (inspectCounterTextComponent != null)
            inspectCounterTextComponent.text = (currentIndex + 1) + "/" + frames.Length;

        if (inspectLeftArrowObject != null)
            inspectLeftArrowObject.SetActive(currentIndex > 0);

        if (inspectRightArrowObject != null)
            inspectRightArrowObject.SetActive(currentIndex < frames.Length - 1);
    }

    public void HideInspectPanel()
    {
        if (inspectPanelObject != null) inspectPanelObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // LOADING PANEL — animasi print (ping-pong kanan kiri)
    // ═══════════════════════════════════════════════
    private Coroutine loadingAnimCoroutine;

    public void ShowLoadingPanel()
    {
        HideAllTutorialPanels();
        if (loadingPanelObject != null) loadingPanelObject.SetActive(true);

        if (loadingAnimCoroutine != null) StopCoroutine(loadingAnimCoroutine);
        loadingAnimCoroutine = StartCoroutine(AnimateLoadingBarPingPong());
    }

    public void HideLoadingPanel()
    {
        if (loadingAnimCoroutine != null)
        {
            StopCoroutine(loadingAnimCoroutine);
            loadingAnimCoroutine = null;
        }
        if (loadingPanelObject != null) loadingPanelObject.SetActive(false);
    }

    private IEnumerator AnimateLoadingBarPingPong()
    {
        if (loadingBarRectTransform == null) yield break;

        RectTransform parentRect = loadingBarRectTransform.parent as RectTransform;
        float parentWidth = parentRect != null ? parentRect.rect.width : 300f;
        float barWidth = loadingBarRectTransform.rect.width;
        float maxX = (parentWidth - barWidth) * 0.5f;

        if (maxX <= 0f) maxX = 100f;

        float speed = maxX * 2f;
        float currentX = 0f;
        int direction = 1;

        while (loadingPanelObject != null && loadingPanelObject.activeSelf)
        {
            currentX += direction * speed * Time.deltaTime;
            if (currentX >= maxX)
            {
                currentX = maxX;
                direction = -1;
            }
            else if (currentX <= -maxX)
            {
                currentX = -maxX;
                direction = 1;
            }

            loadingBarRectTransform.anchoredPosition = new Vector2(currentX, loadingBarRectTransform.anchoredPosition.y);
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════
    // TYPE TEST PANEL — robot check, ghost text ala web typing-test
    // ═══════════════════════════════════════════════
    public void ShowTypeTestPanel(string prompt)
    {
        HideAllTutorialPanels();
        if (typeTestPanelObject != null) typeTestPanelObject.SetActive(true);
        if (typeTestPromptTextComponent != null)
            typeTestPromptTextComponent.text = prompt;

        typeTestTargetPhrase = prompt ?? "";
        RenderTypeTestGhost(playerInputFieldComponent != null ? playerInputFieldComponent.text : "");
    }

    public void HideTypeTestPanel()
    {
        if (typeTestPanelObject != null) typeTestPanelObject.SetActive(false);
        if (typeTestGhostTextComponent != null) typeTestGhostTextComponent.text = "";
        typeTestTargetPhrase = "";
    }

    private void HandleInputValueChangedForTypeTest(string currentValue)
    {
        if (typeTestPanelObject != null && typeTestPanelObject.activeSelf)
            RenderTypeTestGhost(currentValue);
    }

    // Render target phrase dengan warna per-karakter: abu-abu (belum diketik),
    // hijau (benar), merah (salah) — sama kayak typing-test di web.
    private void RenderTypeTestGhost(string typedSoFar)
    {
        if (typeTestGhostTextComponent == null || string.IsNullOrEmpty(typeTestTargetPhrase)) return;

        typedSoFar = typedSoFar ?? "";
        string hexCorrect = ColorUtility.ToHtmlStringRGB(typeTestCorrectColorValue);
        string hexWrong   = ColorUtility.ToHtmlStringRGB(typeTestWrongColorValue);
        string hexPending = ColorUtility.ToHtmlStringRGB(typeTestPendingColorValue);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < typeTestTargetPhrase.Length; i++)
        {
            char targetChar = typeTestTargetPhrase[i];

            if (i < typedSoFar.Length)
            {
                bool isCorrect = typedSoFar[i] == targetChar;
                string hex = isCorrect ? hexCorrect : hexWrong;
                sb.Append("<color=#").Append(hex).Append('>').Append(targetChar).Append("</color>");
            }
            else
            {
                sb.Append("<color=#").Append(hexPending).Append('>').Append(targetChar).Append("</color>");
            }
        }

        typeTestGhostTextComponent.text = sb.ToString();
    }

    public void PlayNotificationSfx()
    {
        if (sfxAudioSourceComponent != null && notificationSfxClip != null)
            sfxAudioSourceComponent.PlayOneShot(notificationSfxClip);
    }

    // ═══════════════════════════════════════════════
    // FOLDER NOTIFICATION — tanda kasus baru
    // ═══════════════════════════════════════════════
    public void ShowFolderNotification()
    {
        if (folderNotificationObject != null) folderNotificationObject.SetActive(true);
        PlayNotificationSfx();
    }

    public void HideFolderNotification()
    {
        if (folderNotificationObject != null) folderNotificationObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // ASK / INFO / CHECK / TRAITS PANEL — menu utama investigasi
    // ═══════════════════════════════════════════════
    public void ShowAskInfoCheckPanel()
    {
        HideAllTutorialPanels();
        if (askInfoCheckPanelObject != null) askInfoCheckPanelObject.SetActive(true);
        if (askInfoCheckTitleText != null)
            askInfoCheckTitleText.text = "INVESTIGASI SUBJEK";
        if (askInfoCheckActionsText != null)
            askInfoCheckActionsText.text =
                "<b><color=#FFE066>[ask]</color></b> Pertanyakan      " +
                "<b><color=#FFE066>[info]</color></b> Biodata      " +
                "<b><color=#FFE066>[check]</color></b> Inspeksi Foto      " +
                "<b><color=#FFE066>[traits]</color></b> Anomali";
        if (askInfoCheckVerdictText != null)
            askInfoCheckVerdictText.text = "<b>APPROVED</b>      <b>DENIED</b>";
        if (askInfoCheckHintText != null)
            askInfoCheckHintText.text = "(ketik perintah di terminal)";
    }

    // BUKA KASUS: foto subjek muncul dulu, baru teks option (ASK INFO CHECK...) barengan.
    public void ShowAskInfoCheckPanelSequential(Sprite photo)
    {
        HideAllTutorialPanels();
        if (askInfoCheckPanelObject != null) askInfoCheckPanelObject.SetActive(true);

        SetAskInfoCheckPhoto(photo);

        if (askInfoCheckTitleText != null)
        {
            askInfoCheckTitleText.text = "INVESTIGASI SUBJEK";
            askInfoCheckTitleText.gameObject.SetActive(false);
        }
        if (askInfoCheckActionsText != null)
        {
            askInfoCheckActionsText.text =
                "<b><color=#FFE066>[ask]</color></b> Pertanyakan      " +
                "<b><color=#FFE066>[info]</color></b> Biodata      " +
                "<b><color=#FFE066>[check]</color></b> Inspeksi Foto      " +
                "<b><color=#FFE066>[traits]</color></b> Anomali";
            askInfoCheckActionsText.gameObject.SetActive(false);
        }
        if (askInfoCheckVerdictText != null)
        {
            askInfoCheckVerdictText.text = "<b>APPROVED</b>      <b>DENIED</b>";
            askInfoCheckVerdictText.gameObject.SetActive(false);
        }
        if (askInfoCheckHintText != null)
        {
            askInfoCheckHintText.text = "(ketik perintah di terminal)";
            askInfoCheckHintText.gameObject.SetActive(false);
        }

        StartCoroutine(RevealAskInfoCheckStaggered());
    }

    private IEnumerator RevealAskInfoCheckStaggered()
    {
        yield return new WaitForSeconds(0.4f);

        if (askInfoCheckTitleText != null)   { askInfoCheckTitleText.gameObject.SetActive(true); }
        if (askInfoCheckActionsText != null) { askInfoCheckActionsText.gameObject.SetActive(true); }
        if (askInfoCheckVerdictText != null) { askInfoCheckVerdictText.gameObject.SetActive(true); }
        if (askInfoCheckHintText != null)    { askInfoCheckHintText.gameObject.SetActive(true); }
        PlayRevealSfx();
    }

    public void SetAskInfoCheckPhoto(Sprite photo)
    {
        if (askInfoCheckPhotoImage == null) return;
        askInfoCheckPhotoImage.sprite = photo;
        askInfoCheckPhotoImage.enabled = photo != null;
    }

    // SCALABLE: dedicated panel baru — hide AskInfoCheck, show AskQuestionListPanel,
    // satu item prefab di-instantiate per pertanyaan, reveal satu-satu + sfx.
    public void ShowAskQuestionPanel(List<CaseQuestion> questions, string responseText = null)
    {
        HideAllTutorialPanels();

        // Pastikan AskInfoCheck panel tetap aktif (foto subjek di background tetap terlihat)
        if (askInfoCheckPanelObject != null) askInfoCheckPanelObject.SetActive(true);

        // Sembunyikan teks bawaan AskInfoCheck agar tidak bertumpuk dengan list pertanyaan
        if (askInfoCheckTitleText   != null) askInfoCheckTitleText.gameObject.SetActive(false);
        if (askInfoCheckActionsText != null) askInfoCheckActionsText.gameObject.SetActive(false);
        if (askInfoCheckVerdictText != null) askInfoCheckVerdictText.gameObject.SetActive(false);
        if (askInfoCheckHintText    != null) askInfoCheckHintText.gameObject.SetActive(false);

        if (askQuestionListPanelObject != null) askQuestionListPanelObject.SetActive(true);

        if (askQuestionListTitleText != null)
            askQuestionListTitleText.text = "PERTANYAAN";

        // Update teks jawaban di UI (jika null/kosong, disembunyikan agar 'New Text' tidak pernah muncul)
        SetAskQuestionResponse(responseText);

        ClearContainer(askQuestionListContainer);

        if (questions == null || askQuestionListContainer == null || askQuestionItemPrefab == null)
            return;

        List<GameObject> spawnedItems = new List<GameObject>();
        for (int i = 0; i < questions.Count; i++)
        {
            CaseQuestion q = questions[i];
            string keyword = string.IsNullOrWhiteSpace(q.commandKeyword)
                ? ("q" + (i + 1))
                : q.commandKeyword.Trim().ToLower();

            GameObject itemGameObject = Instantiate(askQuestionItemPrefab, askQuestionListContainer);
            itemGameObject.SetActive(false);

            TMP_Text itemTextComponent = itemGameObject.GetComponentInChildren<TMP_Text>(true);
            if (itemTextComponent != null)
                itemTextComponent.text = "<b><color=#FFE066>[" + keyword + "]</color></b> " + q.questionText;

            spawnedItems.Add(itemGameObject);
        }

        StartCoroutine(RevealItemsStaggered(spawnedItems));
    }

    // Tampilkan hanya teks jawaban NPC di UI tanpa list opsi pertanyaan (digunakan saat delay respon NPC)
    public void ShowNPCResponseOnly(string responseText)
    {
        HideAllTutorialPanels();

        if (askInfoCheckPanelObject != null) askInfoCheckPanelObject.SetActive(true);

        if (askInfoCheckTitleText   != null) askInfoCheckTitleText.gameObject.SetActive(false);
        if (askInfoCheckActionsText != null) askInfoCheckActionsText.gameObject.SetActive(false);
        if (askInfoCheckVerdictText != null) askInfoCheckVerdictText.gameObject.SetActive(false);
        if (askInfoCheckHintText    != null) askInfoCheckHintText.gameObject.SetActive(false);

        if (askQuestionListPanelObject != null) askQuestionListPanelObject.SetActive(true);

        if (askQuestionListTitleText != null)
            askQuestionListTitleText.text = "JAWABAN SUBJEK";

        SetAskQuestionResponse(responseText);

        ClearContainer(askQuestionListContainer);
    }

    public void HideAskInfoCheckPanel()
    {
        if (askInfoCheckPanelObject != null) askInfoCheckPanelObject.SetActive(false);
        SetAskInfoCheckPhoto(null);
    }

    public void HideAskQuestionPanel()
    {
        if (askQuestionListPanelObject != null) askQuestionListPanelObject.SetActive(false);
        ClearContainer(askQuestionListContainer);

        // Reset dan sembunyikan UI respon pertanyaan saat keluar menu ask
        SetAskQuestionResponse(null);

        // Munculkan kembali teks bawaan AskInfoCheck jika kembali ke menu utama
        if (askInfoCheckTitleText   != null) askInfoCheckTitleText.gameObject.SetActive(true);
        if (askInfoCheckActionsText != null) askInfoCheckActionsText.gameObject.SetActive(true);
        if (askInfoCheckVerdictText != null) askInfoCheckVerdictText.gameObject.SetActive(true);
        if (askInfoCheckHintText    != null) askInfoCheckHintText.gameObject.SetActive(true);
    }

    // ═══════════════════════════════════════════════
    // TRAITS PANEL — kriteria anomali (SCALABLE, prefab per baris, reveal satu-satu)
    // ═══════════════════════════════════════════════
    public void ShowTraitsPanel(List<string> traits)
    {
        HideAllTutorialPanels();
        if (traitsPanelObject != null) traitsPanelObject.SetActive(true);

        ClearContainer(traitsListContainer);

        if (traits == null || traitsListContainer == null || traitsItemPrefab == null)
            return;

        List<GameObject> spawnedItems = new List<GameObject>();
        for (int i = 0; i < traits.Count; i++)
        {
            GameObject itemGameObject = Instantiate(traitsItemPrefab, traitsListContainer);
            itemGameObject.SetActive(false);

            TMP_Text itemTextComponent = itemGameObject.GetComponentInChildren<TMP_Text>(true);
            if (itemTextComponent != null)
                itemTextComponent.text = "- " + traits[i];

            spawnedItems.Add(itemGameObject);
        }

        StartCoroutine(RevealItemsStaggered(spawnedItems));
    }

    public void HideTraitsPanel()
    {
        if (traitsPanelObject != null) traitsPanelObject.SetActive(false);
        ClearContainer(traitsListContainer);
    }

    // ═══════════════════════════════════════════════
    // STAGGERED REVEAL — dipakai boot text (OpeningSequence) & list dinamis (ask/traits)
    // ═══════════════════════════════════════════════
    private void PlayRevealSfx()
    {
        if (sfxAudioSourceComponent != null && revealTingClip != null)
            sfxAudioSourceComponent.PlayOneShot(revealTingClip);
    }

    private IEnumerator RevealItemsStaggered(List<GameObject> items)
    {
        foreach (GameObject item in items)
        {
            if (item != null) item.SetActive(true);
            PlayRevealSfx();
            yield return new WaitForSeconds(revealStaggerDelay);
        }
    }

    // Dipanggil dari OpeningSequence buat nampilin baris-baris teks boot satu-satu
    // (bukan character-by-character kayak TypeStoryText), disertai sfx tiap baris.
    public IEnumerator RevealLinesStaggered(TMP_Text targetTextComponent, string[] lines)
    {
        if (targetTextComponent == null || lines == null) yield break;

        targetTextComponent.text = "";
        foreach (string line in lines)
        {
            if (targetTextComponent.text.Length > 0) targetTextComponent.text += "\n";
            targetTextComponent.text += line;
            PlayRevealSfx();
            yield return new WaitForSeconds(revealStaggerDelay);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    // ═══════════════════════════════════════════════
    // VERDICT PANEL — hasil approved / denied
    // ═══════════════════════════════════════════════
    public void ShowVerdictPanel(bool approved)
    {
        HideAllTutorialPanels();
        if (verdictPanelObject != null) verdictPanelObject.SetActive(true);
        if (verdictTextComponent != null)
        {
            if (approved)
            {
                verdictTextComponent.text =
                    "KEPUTUSAN DITERIMA: <color=#55FF55>APPROVED</color>\n\n" +
                    "<size=80%><color=#88FF88>(Ketik 'print' di terminal untuk mencetak dokumen keputusan)</color></size>";
            }
            else
            {
                verdictTextComponent.text =
                    "KEPUTUSAN DITERIMA: <color=#FF5555>DENIED</color>\n\n" +
                    "<size=80%><color=#FF9999>Subjek akan dikembalikan.\nKetik 'confirm' untuk memastikan keputusan.</color></size>";
            }
        }
    }

    // ═══════════════════════════════════════════════
    // BATTERY UI — update fill amount & visibility
    // ═══════════════════════════════════════════════
    public void SetBatteryVisibility(bool visible)
    {
        if (batteryFrameObject != null)
        {
            batteryFrameObject.SetActive(visible);
        }
        else if (batteryFillImage != null && batteryFillImage.transform.parent != null)
        {
            batteryFillImage.transform.parent.gameObject.SetActive(visible);
        }
    }

    private List<Image> spawnedBatteryBars = new List<Image>();

    public void UpdateBatteryFill(int current, int max)
    {
        if (batteryFillImage == null)
        {
            Debug.LogWarning("[TerminalController] batteryFillImage belum di-assign di Inspector!");
            return;
        }

        Transform frameTransform = batteryFrameObject != null ? batteryFrameObject.transform : batteryFillImage.transform.parent;
        if (frameTransform == null) return;

        // Bersihkan klon bar lama terlebih dahulu
        for (int i = frameTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = frameTransform.GetChild(i);
            if (child.gameObject != batteryFillImage.gameObject)
            {
                Destroy(child.gameObject);
            }
        }
        spawnedBatteryBars.Clear();

        if (max <= 0)
        {
            batteryFillImage.gameObject.SetActive(false);
            return;
        }

        // Tampilkan template bar dasar
        batteryFillImage.gameObject.SetActive(true);
        spawnedBatteryBars.Add(batteryFillImage);

        // Atur HorizontalLayoutGroup otomatis pada frame agar berjejer rapi
        HorizontalLayoutGroup layout = frameTransform.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = frameTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;      // Mengatur lebar otomatis
            layout.childControlHeight = true;     // Mengatur tinggi otomatis
            layout.childForceExpandWidth = true;  // Membagi rata lebar frame
            layout.childForceExpandHeight = true;
            layout.spacing = 4f;                  // Spasi antar bar baterai
            layout.padding = new RectOffset(6, 12, 6, 6); // Padding dari tepi frame
        }

        // Kloning template batteryFillImage hingga jumlah max
        for (int i = 1; i < max; i++)
        {
            Image clone = Instantiate(batteryFillImage, frameTransform);
            clone.name = "Battery_Fill_Clone_" + i;
            spawnedBatteryBars.Add(clone);
        }

        // Atur status aktif/tidaknya warna ijo per bar (dari kiri ke kanan)
        for (int i = 0; i < spawnedBatteryBars.Count; i++)
        {
            spawnedBatteryBars[i].gameObject.SetActive(true);
            
            // Atur ImageType ke Simple karena kita pakai layout bar terpisah (bukan radial/linear fillAmount lagi)
            spawnedBatteryBars[i].type = Image.Type.Simple;

            if (i < current)
            {
                // Bar aktif berwarna hijau penuh
                spawnedBatteryBars[i].color = Color.white; 
            }
            else
            {
                // Bar habis berwarna abu-abu gelap transparan
                spawnedBatteryBars[i].color = new Color(0.2f, 0.2f, 0.2f, 0.15f);
            }
        }
    }

    // ═══════════════════════════════════════════════
    // FORCED VERDICT MODE — tampilan saat baterai habis
    // ═══════════════════════════════════════════════
    public void ShowForcedVerdictMode(bool forceApproveOnly)
    {
        if (askInfoCheckActionsText == null) return;
        if (forceApproveOnly)
        {
            askInfoCheckActionsText.text =
                "<color=#FF5555>SISTEM: Penolakan tidak diizinkan oleh otoritas pusat.</color>\n\n" +
                "<color=#FFFF55>[approved]</color>  — setujui subjek";
        }
        else
        {
            askInfoCheckActionsText.text =
                "<color=#FF5555>Kesempatan investigasi habis. Ambil keputusan:</color>\n\n" +
                "<color=#55FF55>[approved]</color>  — izinkan masuk\n" +
                "<color=#FF5555>[denied]</color>    — tolak & kembalikan";
        }
    }

    // ═══════════════════════════════════════════════
    // PHOTO GLITCH — efek visual kasus Climax
    // ═══════════════════════════════════════════════
    public IEnumerator ShowPhotoGlitch(Sprite[] originalFrames, int originalIndex)
    {
        if (glitchPhotoSprite != null && inspectPhotoImageComponent != null)
        {
            inspectPhotoImageComponent.sprite = glitchPhotoSprite;
            yield return new WaitForSeconds(1.5f);

            // Kembali ke foto asli
            if (originalFrames != null && originalIndex < originalFrames.Length)
                inspectPhotoImageComponent.sprite = originalFrames[originalIndex];
        }
        else
        {
            yield return null;
        }
    }

    public void HideVerdictPanel()
    {
        if (verdictPanelObject != null) verdictPanelObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════
    // SERVICE ACCESS
    // ═══════════════════════════════════════════════
    public CommandProcessorService GetCommandProcessor()
    {
        return commandProcessorService;
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

        if (inspectPanelObject        == null) Debug.LogWarning("[TerminalController] inspectPanelObject belum di-assign!");
        if (inspectPhotoImageComponent== null) Debug.LogWarning("[TerminalController] inspectPhotoImageComponent belum di-assign!");
        if (loadingPanelObject        == null) Debug.LogWarning("[TerminalController] loadingPanelObject belum di-assign!");
        if (loadingBarRectTransform   == null) Debug.LogWarning("[TerminalController] loadingBarRectTransform belum di-assign!");
        if (typeTestPanelObject      == null) Debug.LogWarning("[TerminalController] typeTestPanelObject belum di-assign!");
        if (typeTestPromptTextComponent== null) Debug.LogWarning("[TerminalController] typeTestPromptTextComponent belum di-assign!");
        if (typeTestGhostTextComponent == null) Debug.LogWarning("[TerminalController] typeTestGhostTextComponent belum di-assign! (ghost text type-test gak akan muncul)");
        if (folderNotificationObject  == null) Debug.LogWarning("[TerminalController] folderNotificationObject belum di-assign!");
        if (askInfoCheckPanelObject   == null) Debug.LogWarning("[TerminalController] askInfoCheckPanelObject belum di-assign!");
        if (askInfoCheckTitleText     == null) Debug.LogWarning("[TerminalController] askInfoCheckTitleText belum di-assign!");
        if (askInfoCheckActionsText   == null) Debug.LogWarning("[TerminalController] askInfoCheckActionsText belum di-assign!");
        if (askInfoCheckVerdictText   == null) Debug.LogWarning("[TerminalController] askInfoCheckVerdictText belum di-assign!");
        if (askInfoCheckHintText      == null) Debug.LogWarning("[TerminalController] askInfoCheckHintText belum di-assign!");
        if (askInfoCheckPhotoImage    == null) Debug.LogWarning("[TerminalController] askInfoCheckPhotoImage belum di-assign!");
        if (askQuestionListPanelObject == null) Debug.LogWarning("[TerminalController] askQuestionListPanelObject belum di-assign! (panel 'ask' gak akan kelihatan)");
        if (askQuestionListTitleText  == null) Debug.LogWarning("[TerminalController] askQuestionListTitleText belum di-assign!");
        if (askQuestionListContainer  == null) Debug.LogWarning("[TerminalController] askQuestionListContainer belum di-assign! (ask menu gak akan nampilin daftar pertanyaan)");
        if (askQuestionItemPrefab     == null) Debug.LogWarning("[TerminalController] askQuestionItemPrefab belum di-assign! (ask menu gak akan nampilin daftar pertanyaan)");
        if (traitsPanelObject         == null) Debug.LogWarning("[TerminalController] traitsPanelObject belum di-assign! (menu 'traits' gak akan kelihatan)");
        if (traitsListContainer       == null) Debug.LogWarning("[TerminalController] traitsListContainer belum di-assign!");
        if (traitsItemPrefab          == null) Debug.LogWarning("[TerminalController] traitsItemPrefab belum di-assign!");
        if (verdictPanelObject        == null) Debug.LogWarning("[TerminalController] verdictPanelObject belum di-assign!");
        if (verdictTextComponent      == null) Debug.LogWarning("[TerminalController] verdictTextComponent belum di-assign!");
    }
}