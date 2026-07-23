using System;
using System.Collections.Generic;
using UnityEngine;

public class CommandProcessorService
{
    private const int MAX_ATTEMPTS_PER_SUBJECT = 2;

    private SubjectDataModel activeSubjectDataModel = null;
    private int correctVerdictCountInt = 0;

    private Dictionary<string, int> attemptCountBySubjectId = new Dictionary<string, int>();

    // Tutorial: subject hardcode (S-0042/0043/0044), tidak scalable
    private List<SubjectDataModel> subjectDatabaseList = new List<SubjectDataModel>()
    {
        new SubjectDataModel("S-0042", "Ahmad",    "Laki-laki",  "1987-03-14", "2027-03-14", false),
        new SubjectDataModel("S-0043", "Juan",  "???",        "2031-??-??", "2019-00-00", true),
        new SubjectDataModel("S-0044", "Sari", "Perempuan",  "1995-11-02", "2028-11-02", true),
    };

    // 4 case utama: subject scalable lewat asset SubjectDefinition.
    // List ini diisi lewat TerminalController (subjectDefinitionList) biar muncul di Inspector.
    private List<SubjectDefinition> subjectDefinitionList;

    public delegate void OnLineAddedDelegate(string text, TerminalLineType lineType);
    private OnLineAddedDelegate onLineAddedCallback;

    public Action<Sprite> OnSubjectPhotoChanged;
    public Action<bool, SubjectDataModel> OnVerdictResolved;

    private CaseManager caseManager;
    private bool inputBlocked;
    private bool isInGameplay;

    public CommandProcessorService(OnLineAddedDelegate lineCallback)
    {
        onLineAddedCallback = lineCallback;
    }

    public void SetCaseManager(CaseManager cm)
    {
        caseManager = cm;
        isInGameplay = true;
    }

    public void SetSubjectPhoto(string id, Sprite photo)
    {
        SubjectDataModel subject = subjectDatabaseList.Find(s => s.subjectIdString == id);
        if (subject != null)
            subject.subjectPhoto = photo;
    }

    public void SetSubjectDefinitionList(List<SubjectDefinition> list)
    {
        subjectDefinitionList = list;
    }

    public SubjectDataModel GetSubjectById(string id)
    {
        // 4 case utama: cari di SubjectDefinition (scalable)
        SubjectDefinition def = subjectDefinitionList != null
            ? subjectDefinitionList.Find(s => s.subjectId == id)
            : null;
        if (def != null)
        {
            Sprite[] frames = def.photoFrames;
            return new SubjectDataModel(
                def.subjectId, def.fullName, def.gender, def.dateOfBirth,
                def.expiryDate, def.isMimic, def.subjectPhoto, frames);
        }

        // Tutorial: fallback ke database hardcode
        return subjectDatabaseList.Find(s => s.subjectIdString == id);
    }

    public void ExecuteFetchById(string id)
    {
        ExecuteFetchCommand(new string[] { "fetch", id.ToUpper() });
    }

    public void BlockInput(bool block)
    {
        inputBlocked = block;
    }

    public void ProcessCommand(string rawInputString)
    {
        if (inputBlocked) return;

        string trimmedInputString = rawInputString.Trim().ToLower();
        string[] inputPartsArray = trimmedInputString.Split(' ');
        string baseCommandString = inputPartsArray[0];

        AddLine("VERIFIER@ALPHA-SEC:~$ " + rawInputString, TerminalLineType.Input);

        if (string.IsNullOrWhiteSpace(trimmedInputString)) return;

        // ── CASE ACTIVE: delegate ALL input to CaseManager state machine ──
        if (isInGameplay && caseManager != null && caseManager.IsCaseActive)
        {
            caseManager.ProcessInput(trimmedInputString, inputPartsArray);
            return;
        }

        // ── GLOBAL COMMANDS (free mode) ──
        switch (baseCommandString)
        {
            case "help": ExecuteHelpCommand(); break;
            case "status": ExecuteStatusCommand(); break;
            case "fetch": ExecuteFetchCommand(inputPartsArray); break;
            case "approved": ExecuteLegacyVerdict(true); break;
            case "denied": ExecuteLegacyVerdict(false); break;
            case "clear": ExecuteClearCommand(); break;
            default: ExecuteUnknownCommand(baseCommandString); break;
        }
    }

    private void ExecuteLegacyVerdict(bool approved)
    {
        ExecuteVerdictCommand(approved);
    }

    // ═══════════════════════════════════════════════
    // LEGACY COMMANDS (free mode after tutorial)
    // ═══════════════════════════════════════════════
    private void ExecuteHelpCommand()
    {
        AddLine("=== AVAILABLE COMMANDS ===", TerminalLineType.System);
        AddLine("help           — daftar perintah", TerminalLineType.Response);
        AddLine("status         — cek status sistem", TerminalLineType.Response);
        AddLine("fetch [id]     — ambil file subject", TerminalLineType.Response);
        AddLine("open           — buka kasus (mode kasus)", TerminalLineType.Response);
        AddLine("ask            — tampilkan daftar pertanyaan", TerminalLineType.Response);
        AddLine("info           — lihat data subjek", TerminalLineType.Response);
        AddLine("check          — periksa foto subjek", TerminalLineType.Response);
        AddLine("right / left    — putar foto subjek (saat check)", TerminalLineType.Response);
        AddLine("traits         — lihat kriteria anomali (gratis)", TerminalLineType.Response);
        AddLine("approved       — setujui subject aktif", TerminalLineType.Response);
        AddLine("denied         — tolak subject aktif", TerminalLineType.Response);
        AddLine("print          — cetak dokumen setelah verdict", TerminalLineType.Response);
        AddLine("back           — kembali ke menu utama kasus", TerminalLineType.Response);
        AddLine("esc            — keluar dari komputer (diam-diam)", TerminalLineType.Response);
        AddLine("clear          — bersihkan layar", TerminalLineType.Response);
        AddLine("==========================", TerminalLineType.System);
    }

    private void ExecuteStatusCommand()
    {
        bool hasPendingSubjectBool = activeSubjectDataModel != null;

        AddLine("SYSTEM STATUS REPORT", TerminalLineType.System);
        AddLine("> Koneksi database  : [OK]", TerminalLineType.Response);
        AddLine("> Printer           : [STANDBY]", TerminalLineType.Response);
        AddLine("> Shift akurasi     : " + correctVerdictCountInt + " benar", TerminalLineType.Response);
        AddLine("> Subject aktif     : " + (hasPendingSubjectBool ? activeSubjectDataModel.subjectIdString : "tidak ada"),
                                                                                     TerminalLineType.Response);
        if (hasPendingSubjectBool)
            AddLine("> Menunggu verdict untuk: " + activeSubjectDataModel.subjectIdString, TerminalLineType.Warning);
    }

    private void ExecuteFetchCommand(string[] inputPartsArray)
    {
        if (inputPartsArray.Length < 2)
        {
            AddLine("ERR: format salah. Gunakan: fetch [ID]", TerminalLineType.Error);
            return;
        }

        string requestedIdString = inputPartsArray[1].ToUpper();
        SubjectDataModel foundSubject = subjectDatabaseList.Find(
            subject => subject.subjectIdString == requestedIdString
        );

        if (foundSubject == null)
        {
            AddLine("ERR: ID tidak ditemukan — " + requestedIdString, TerminalLineType.Error);
            return;
        }

        if (attemptCountBySubjectId.TryGetValue(foundSubject.subjectIdString, out int usedAttempts)
            && usedAttempts >= MAX_ATTEMPTS_PER_SUBJECT)
        {
            AddLine("ERR: subject " + foundSubject.subjectIdString + " sudah di-lock (gagal maksimal).", TerminalLineType.Error);
            return;
        }

        activeSubjectDataModel = foundSubject;
        OnSubjectPhotoChanged?.Invoke(foundSubject.subjectPhoto);

        AddLine(">>> FILE DITERIMA <<<", TerminalLineType.System);
        AddLine("================================", TerminalLineType.Response);
        AddLine("SUBJECT ID   : " + foundSubject.subjectIdString, TerminalLineType.Response);
        AddLine("NAMA         : " + foundSubject.fullNameString, TerminalLineType.Response);
        AddLine("GENDER       : " + foundSubject.genderString, TerminalLineType.Response);
        AddLine("TGL LAHIR    : " + foundSubject.dateOfBirthString, TerminalLineType.Response);
        AddLine("EXP DATE     : " + foundSubject.expiryDateString, TerminalLineType.Response);
        AddLine("================================", TerminalLineType.Response);

        int remainingAttempts = MAX_ATTEMPTS_PER_SUBJECT - usedAttempts;
        if (usedAttempts > 0)
            AddLine("> Percobaan tersisa: " + remainingAttempts, TerminalLineType.Warning);

        AddLine("> Periksa data & foto. Ketik approved atau denied.", TerminalLineType.Warning);
    }

    private void ExecuteVerdictCommand(bool isApproved)
    {
        if (activeSubjectDataModel == null)
        {
            AddLine("ERR: tidak ada subject aktif. Gunakan fetch [ID] dulu.", TerminalLineType.Error);
            return;
        }

        SubjectDataModel subject = activeSubjectDataModel;
        string verdictString = isApproved ? "APPROVED" : "DENIED";
        bool isCorrectVerdict = isApproved != subject.isMimicBool;

        AddLine(">>> VERDICT: " + verdictString + " <<<", TerminalLineType.System);

        if (isCorrectVerdict)
        {
            AddLine("[BENAR] Verifikasi akurat.", TerminalLineType.Response);
            AddLine("Dokumen dicetak dan dikirim.", TerminalLineType.Response);
            correctVerdictCountInt++;
        }
        else
        {
            int prevAttempts = attemptCountBySubjectId.TryGetValue(subject.subjectIdString, out int a) ? a : 0;
            attemptCountBySubjectId[subject.subjectIdString] = prevAttempts + 1;

            if (subject.isMimicBool && isApproved)
                AddLine("[FATAL] Kamu menyetujui MIMIC. Alpha Sector terancam bahaya.", TerminalLineType.Error);
            else
                AddLine("[ERROR] Kamu menolak warga sah. Laporan dibuat.", TerminalLineType.Error);

            int attemptsLeft = MAX_ATTEMPTS_PER_SUBJECT - (prevAttempts + 1);
            if (attemptsLeft > 0)
                AddLine("> Subject bisa di-fetch ulang. Sisa percobaan: " + attemptsLeft, TerminalLineType.Warning);
            else
                AddLine("> Subject di-lock. Maksimal percobaan tercapai.", TerminalLineType.Error);
        }

        OnVerdictResolved?.Invoke(isCorrectVerdict, subject);

        activeSubjectDataModel = null;
        OnSubjectPhotoChanged?.Invoke(null);
    }

    private void ExecuteClearCommand()
    {
        AddLine("__CLEAR__", TerminalLineType.System);
    }

    private void ExecuteUnknownCommand(string unknownCommandString)
    {
        AddLine("ERR: perintah tidak dikenal — \"" + unknownCommandString + "\"", TerminalLineType.Error);
        AddLine("> Ketik help untuk daftar perintah.", TerminalLineType.System);
    }

    private void AddLine(string textString, TerminalLineType lineTypeEnum)
    {
        onLineAddedCallback?.Invoke(textString, lineTypeEnum);
    }
}
