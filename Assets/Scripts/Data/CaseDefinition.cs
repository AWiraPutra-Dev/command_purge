using UnityEngine;
using System.Collections.Generic;

public enum CaseType { Normal, Anomaly, Climax }

[CreateAssetMenu(menuName = "Command Purge/Case Definition", fileName = "NewCase")]
public class CaseDefinition : ScriptableObject
{
    public int caseNumber;
    public CaseType caseType = CaseType.Normal;
    public string subjectId;
    public string notificationText;

    [Header("Kesempatan (baterai)")]
    public int maxChances = 3;

    [Header("Daftar Pertanyaan (bebas isi)")]
    public List<CaseQuestion> questions;

    [Header("Type Test Fase Print (robot check) — trigger doang, lanjut ke printing")]
    public bool usePrintTypeTest = true;
    [TextArea] public string printTypeTestPrompt = "KETIK: typetest";  // teks yg harus diketik player pas print
    public string printTypeTestAnswer = "typetest";

    [Header("Climax (Case 4)")]
    public bool forceApprovedOnly = false;  // cuma 'approved' yg jalan
    public bool glitchOnRotate = false;       // glitch pas rotate di check
}
