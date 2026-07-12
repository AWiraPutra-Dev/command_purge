using System;
using System.Collections.Generic;
using UnityEngine;

public class CommandProcessorService
{
    private const int MAX_ATTEMPTS_PER_SUBJECT = 2;

    private SubjectDataModel activeSubjectDataModel = null;
    private int correctVerdictCountInt = 0;

    private Dictionary<string, int> attemptCountBySubjectId = new Dictionary<string, int>();

    private List<SubjectDataModel> subjectDatabaseList = new List<SubjectDataModel>()
    {
        new SubjectDataModel("S-0042", "Ahmad",    "Laki-laki",  "1987-03-14", "2027-03-14", false),
        new SubjectDataModel("S-0043", "Juan",  "???",        "2031-??-??", "2019-00-00", true),
        new SubjectDataModel("S-0044", "Sari", "Perempuan",  "1995-11-02", "2028-11-02", true),
    };

    public delegate void OnLineAddedDelegate(string text, TerminalLineType lineType);
    private OnLineAddedDelegate onLineAddedCallback;

    public Action<Sprite> OnSubjectPhotoChanged;
    public Action<bool, SubjectDataModel> OnVerdictResolved;

    public CommandProcessorService(OnLineAddedDelegate lineCallback)
    {
        onLineAddedCallback = lineCallback;
    }

    public void SetSubjectPhoto(string id, Sprite photo)
    {
        SubjectDataModel subject = subjectDatabaseList.Find(s => s.subjectIdString == id);
        if (subject != null)
            subject.subjectPhoto = photo;
    }

    public SubjectDataModel GetSubjectById(string id)
    {
        return subjectDatabaseList.Find(s => s.subjectIdString == id);
    }

    public void ProcessCommand(string rawInputString)
    {
        string trimmedInputString = rawInputString.Trim().ToLower();
        string[] inputPartsArray = trimmedInputString.Split(' ');
        string baseCommandString = inputPartsArray[0];

        AddLine("VERIFIER@ALPHA-SEC:~$ " + rawInputString, TerminalLineType.Input);

        if (string.IsNullOrWhiteSpace(trimmedInputString)) return;

        switch (baseCommandString)
        {
            case "help": ExecuteHelpCommand(); break;
            case "status": ExecuteStatusCommand(); break;
            case "fetch": ExecuteFetchCommand(inputPartsArray); break;
            case "approved": ExecuteVerdictCommand(isApproved: true); break;
            case "denied": ExecuteVerdictCommand(isApproved: false); break;
            case "clear": ExecuteClearCommand(); break;
            default: ExecuteUnknownCommand(baseCommandString); break;
        }
    }

    private void ExecuteHelpCommand()
    {
        AddLine("=== AVAILABLE COMMANDS ===", TerminalLineType.System);
        AddLine("help           — daftar perintah", TerminalLineType.Response);
        AddLine("status         — cek status sistem", TerminalLineType.Response);
        AddLine("fetch [id]     — ambil file subject", TerminalLineType.Response);
        AddLine("approved       — setujui subject aktif", TerminalLineType.Response);
        AddLine("denied         — tolak subject aktif", TerminalLineType.Response);
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
