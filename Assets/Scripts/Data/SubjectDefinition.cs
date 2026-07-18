using UnityEngine;

[CreateAssetMenu(menuName = "Command Purge/Subject Definition", fileName = "NewSubject")]
public class SubjectDefinition : ScriptableObject
{
    public string subjectId;
    public string fullName;
    public string gender;
    public string dateOfBirth;
    public string expiryDate;
    public bool isMimic;

    public Sprite subjectPhoto;
    public Sprite[] photoFrames; // index 0 = depan (panel utama), 1 = belakang, 2 = samping kanan, 3 = samping kiri
}
