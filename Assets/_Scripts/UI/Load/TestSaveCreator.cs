using UnityEngine;

// 테스트용 임시 값들 나중에 실제 값 연결 후 삭제 예정
public class TestSaveCreator : MonoBehaviour
{
    public void CreateTestSave(int slot)
    {
        PlayData data = new PlayData
        {
            slotIndex = slot,
            school = "한울 고등학교",
            playTime = "2000년00월00일",
            saveTime = System.DateTime.Now.ToString("2000.00.00"),
            gold = MoneyManager.Instance.Gold,
            reputation = MoneyManager.Instance.Reputation
        };

        SaveSystem.Instance.Save(data);
        Debug.Log("저장 완료");
    }
}