using UnityEngine;

public class PlayerAccuracy : MonoBehaviour
{
    public static PlayerAccuracy Instance { get; private set; }

    [Header("Statistik Rahasia Akurasi Player")]
    public int totalCasesProcessed = 0;
    public int correctVerdictsCount = 0;
    public int wrongVerdictsCount = 0;
    public int mimicPassedCount = 0;   // Kesalahan: meloloskan Mimic (menyetujui Mimic)
    public int humanDeniedCount = 0;  // Kesalahan: menolak Manusia Asli

    public float AccuracyPercentage => totalCasesProcessed > 0
        ? ((float)correctVerdictsCount / totalCasesProcessed) * 100f
        : 100f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordVerdict(bool isApproved, bool isMimic)
    {
        totalCasesProcessed++;

        bool isCorrect = isApproved != isMimic;

        if (isCorrect)
        {
            correctVerdictsCount++;
        }
        else
        {
            wrongVerdictsCount++;
            if (isMimic && isApproved)
            {
                mimicPassedCount++;
            }
            else if (!isMimic && !isApproved)
            {
                humanDeniedCount++;
            }
        }

        Debug.Log($"[PlayerAccuracy] Cases: {totalCasesProcessed} | Correct: {correctVerdictsCount} | Wrong: {wrongVerdictsCount} | MimicsPassed: {mimicPassedCount} | Accuracy: {AccuracyPercentage:F1}%");
    }

    public void ResetStats()
    {
        totalCasesProcessed = 0;
        correctVerdictsCount = 0;
        wrongVerdictsCount = 0;
        mimicPassedCount = 0;
        humanDeniedCount = 0;
    }
}
