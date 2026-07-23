using System;
using UnityEngine;

[Serializable]
public class CaseQuestion
{
    [TextArea] public string questionText;     // "Berapa ID kamu?"
    [TextArea] public string realResponse;      // jawaban kalau subjek REAL
    [TextArea] public string mimicResponse;     // jawaban kalau subjek MIMIC

    [Header("Command Keyword (buat scalable ask menu)")]
    [Tooltip("Kata kunci yang harus diketik player buat milih pertanyaan ini (misal 'id', 'name', 'reason'). " +
             "Kosongkan aja kalau mau auto pakai q1/q2/q3/dst sesuai urutan di list.")]
    public string commandKeyword;

    public bool useTypeTest = true;            // kasih jawaban lewat type test?
    [TextArea] public string typeTestPrompt;  // teks yg muncul di type test (biasanya = jawaban yg hrs diketik)
    public string typeTestAnswer;               // yg harus player ketik balik
}