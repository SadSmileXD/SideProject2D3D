using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Skill System/Skill Data")]
public class SkillData : ScriptableObject
{
    [SerializeField] private Sprite m_Sprite;
    public Sprite Sprite
    {
        get => m_Sprite;
        set => m_Sprite = value;
    }

    [SerializeField] private string m_skillName;
  
    public string SkillName
    {
        get => m_skillName;
        set => m_skillName = value;
    }

    public void SetData(string skillName, Sprite sprite)
    {
        m_skillName = skillName;
        m_Sprite = sprite;
    }
}