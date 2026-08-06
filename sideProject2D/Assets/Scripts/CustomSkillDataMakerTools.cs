using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class CustomSkillDataMakerTools : EditorWindow
{
    // 경로 설정
    private string m_saveFolderPath = "Assets/SkillData";

    // 데이터 리스트 및 선택 상태
    private List<SkillData> m_skillList = new List<SkillData>();
    private SkillData m_selectedSkill = null;
    private Vector2 m_leftScrollPos;
    private Vector2 m_rightScrollPos;

    // 수정 및 신규 생성용 임시 필드
    private string m_editSkillName = "";
    private Sprite m_editSkillSprite = null;

    [MenuItem("Tools/SkillDataMakerTools")]
    public static void ShowWindow()
    {
        CustomSkillDataMakerTools window = GetWindow<CustomSkillDataMakerTools>("Skill Data Editor");
        window.minSize = new Vector2(600, 400);
    }

    private void OnEnable()
    {
        // 툴 창이 열릴 때 기존 에셋 목록을 새로고침
        RefreshSkillList();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // --------------------------------------------------
        // [좌측 패널] 스킬 데이터 리스트 영역 (너비 220px)
        // --------------------------------------------------
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220), GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField("Skill Assets", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh List"))
        {
            RefreshSkillList();
        }

        if (GUILayout.Button("+ Create New Skill", GUILayout.Height(25)))
        {
            SelectSkill(null); // 신규 생성 모드로 전환
        }

        EditorGUILayout.Space(5);

        // 스킬 목록 스크롤 뷰
        m_leftScrollPos = EditorGUILayout.BeginScrollView(m_leftScrollPos);
        for (int i = 0; i < m_skillList.Count; i++)
        {
            SkillData skill = m_skillList[i];
            if (skill == null) continue;

            // 선택된 항목 하이라이트 스타일 적용
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            if (m_selectedSkill == skill)
            {
                buttonStyle.normal.textColor = Color.cyan;
                buttonStyle.fontStyle = FontStyle.Bold;
            }

            string displayName = string.IsNullOrEmpty(skill.SkillName) ? skill.name : skill.SkillName;

            if (GUILayout.Button($" [{i + 1}] {displayName}", buttonStyle, GUILayout.Height(25)))
            {
                SelectSkill(skill);
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        // --------------------------------------------------
        // [우측 패널] 상세 데이터 수정 및 생성 영역
        // --------------------------------------------------
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        m_rightScrollPos = EditorGUILayout.BeginScrollView(m_rightScrollPos);

        if (m_selectedSkill != null)
        {
            // --- 기존 데이터 수정 모드 ---
            EditorGUILayout.LabelField($"Edit Skill: {m_selectedSkill.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 에셋 파일 직접 바인딩 표시
            EditorGUILayout.ObjectField("Asset File", m_selectedSkill, typeof(SkillData), false);
            EditorGUILayout.Space();

            m_editSkillName = EditorGUILayout.TextField("Skill Name", m_editSkillName);
            m_editSkillSprite = (Sprite)EditorGUILayout.ObjectField("Skill Sprite", m_editSkillSprite, typeof(Sprite), false);

            EditorGUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                SaveChanges();
            }

            if (GUILayout.Button("Delete Asset", GUILayout.Height(30), GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Delete Skill", $"'{m_selectedSkill.name}' 에셋을 정말 삭제하시겠습니까?", "Delete", "Cancel"))
                {
                    DeleteSelectedSkill();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            // --- 신규 생성 모드 ---
            EditorGUILayout.LabelField("Create New Skill Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            m_editSkillName = EditorGUILayout.TextField("Skill Name", m_editSkillName);
            m_editSkillSprite = (Sprite)EditorGUILayout.ObjectField("Skill Sprite", m_editSkillSprite, typeof(Sprite), false);
            m_saveFolderPath = EditorGUILayout.TextField("Save Path", m_saveFolderPath);

            EditorGUILayout.Space(15);

            if (GUILayout.Button("Create Asset", GUILayout.Height(30)))
            {
                CreateSkillAsset();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    // 프로젝트 내 지정된 폴더의 모든 SkillData 에셋 검색 및 로드
    private void RefreshSkillList()
    {
        m_skillList.Clear();

        if (!Directory.Exists(m_saveFolderPath))
        {
            Directory.CreateDirectory(m_saveFolderPath);
            AssetDatabase.Refresh();
        }

        // SkillData 타입의 에셋 GUID 검색
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { m_saveFolderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill != null)
            {
                m_skillList.Add(skill);
            }
        }
    }

    // 항목 선택 처리
    private void SelectSkill(SkillData skill)
    {
        m_selectedSkill = skill;

        if (m_selectedSkill != null)
        {
            m_editSkillName = m_selectedSkill.SkillName;
            m_editSkillSprite = m_selectedSkill.Sprite;
            Selection.activeObject = m_selectedSkill; // 프로젝트 창에서도 해당 파일 선택
        }
        else
        {
            // 신규 생성 모드로 초기화
            m_editSkillName = "";
            m_editSkillSprite = null;
        }
    }

    // 변경사항 저장
    private void SaveChanges()
    {
        if (m_selectedSkill == null) return;

        Undo.RecordObject(m_selectedSkill, "Update Skill Data"); // Undo(되돌리기) 지원

        m_selectedSkill.SetData(m_editSkillName, m_editSkillSprite);

        // 변경사항 저장 플래그 설정
        EditorUtility.SetDirty(m_selectedSkill);
        AssetDatabase.SaveAssets();

        RefreshSkillList();
        Debug.Log($"[SkillDataMaker] '{m_selectedSkill.name}' 변경사항 저장 완료.");
    }

    // 신규 생성
    private void CreateSkillAsset()
    {
        if (string.IsNullOrEmpty(m_editSkillName))
        {
            EditorUtility.DisplayDialog("Error", "스킬 이름을 입력해 주세요.", "OK");
            return;
        }

        SkillData newSkill = ScriptableObject.CreateInstance<SkillData>();
        newSkill.SetData(m_editSkillName, m_editSkillSprite);

        string fullPath = $"{m_saveFolderPath}/{m_editSkillName}.asset";
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        AssetDatabase.CreateAsset(newSkill, fullPath);
        AssetDatabase.SaveAssets();

        RefreshSkillList();
        SelectSkill(newSkill);

        Debug.Log($"[SkillDataMaker] 에셋 생성 완료: {fullPath}");
    }

    // 선택된 에셋 삭제
    private void DeleteSelectedSkill()
    {
        if (m_selectedSkill == null) return;

        string path = AssetDatabase.GetAssetPath(m_selectedSkill);
        AssetDatabase.DeleteAsset(path);

        SelectSkill(null);
        RefreshSkillList();
    }
}