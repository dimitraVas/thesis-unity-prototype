using UnityEngine;

[System.Serializable]
public class ParticleData
{
    public string emotion;
    public string emotionCategory;
    public Color color;
    public int index;
    public string inputText;

    // Constructor to initialize the data
    public ParticleData(string emotion, string category, Color color, int index, string inputText)
    {
        this.emotion = emotion;
        this.emotionCategory = category;
        this.color = color;
        this.index = index;
        this.inputText = inputText;
    }
}
