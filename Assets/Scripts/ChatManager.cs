using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    public Transform chatContent;      // ScrollView의 Content RectTransform
    public GameObject customerPrefab; // 손님 말풍선 프리팹
    public GameObject playerPrefab;   // 플레이어 말풍선 프리팹
    public ScrollRect scrollRect;      // Scroll View의 ScrollRect

    // 새 대사가 올 때 실행
    public void AddMessage(bool isPlayer, string text)
    {
        GameObject prefab = isPlayer ? playerPrefab : customerPrefab;
        GameObject newMessage = Instantiate(prefab, chatContent);

        // 메시지 생성 후 자동 스크롤을 맨 아래로 내리기
        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        // 1프레임 대기 (Vertical Layout Group이 프리팹 크기를 재계산할 시간 확보)
        yield return new WaitForEndOfFrame();

        // 스크롤바 위치를 맨 아래(0)로 고정
        scrollRect.verticalNormalizedPosition = 0f;
    }
}