using UnityEngine;

// 테스트용 임시 값들 나중에 실제 값 연결 후 삭제 예정
public class TestSaveCreator : MonoBehaviour
{
    public void CreateTestSave(int slot)
    {
        PlayData data = new PlayData
        {
            slotIndex = slot,
            school = "유니티고",
            playTime = "01:23:45",
            saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            gold = 500,
            reputation = 2
        };

        SaveSystem.Instance.Save(data);
        Debug.Log("저장 완료");
    }
}