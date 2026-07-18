using UnityEngine;

[System.Serializable]
public class SubjectDataModel
{
    public string subjectIdString;
    public string fullNameString;
    public string genderString;
    public string dateOfBirthString;
    public string expiryDateString;
    public bool isMimicBool;
    public Sprite subjectPhoto;
    public Sprite[] photoFrames;

    public SubjectDataModel(string id, string name, string gender, string dob, string expiry, bool isMimic, Sprite photo = null, Sprite[] frames = null)
    {
        subjectIdString = id;
        fullNameString = name;
        genderString = gender;
        dateOfBirthString = dob;
        expiryDateString = expiry;
        isMimicBool = isMimic;
        subjectPhoto = photo;
        photoFrames = frames;
    }
}
