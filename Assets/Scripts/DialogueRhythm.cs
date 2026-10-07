using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueRhythm : MonoBehaviour
{
    [Header("노래")]
    public AudioSource musicSource;

    [Header("채팅 UI (Scroll View)")]
    public RectTransform chatContent;      // ScrollView 안의 Content
    public ScrollRect chatScrollRect;      // ScrollView의 ScrollRect
    public GameObject customerBubblePrefab; // 손님 말풍선 프리팹
    public GameObject playerBubblePrefab;   // 플레이어 말풍선 프리팹

    [Header("Customer 노트 Image (D, F, J, K)")]
    public Image[] customerNotes = new Image[4];

    [Header("Player 노트 Image (D, F, J, K)")]
    public Image[] playerNotes = new Image[4];

    [Header("노트 색상 설정")]
    public Color normalColor = Color.white;
    public Color activeColor = Color.yellow;
    public Color hitColor = Color.green;
    public Color missColor = Color.red;

    [Header("입력 피드백")]
    public Color inputcolor = new Color(0.6f, 0.85f, 1f);

    [Header("노래에 맞춘 노트 타이밍 (초 단위, 직접 입력)")]
    public float[] customerNoteTimes = new float[4]; // 예: 1.2, 2.0, 2.8, 3.6
    public float[] playerNoteTimes = new float[4];    // 예: 4.4, 5.2, 6.0, 6.8

    [Header("판정 허용 오차 (초)")]
    public float hitWindow = 0.35f; // 목표 시간 ± 이 값 안이면 HIT 인정


    string[] customerDialogue =
    {
        " 안녕하세요.",
        "전..",
        "오늘",
        "너무 힘든 일이 있었어요. "
    };

    string[] playerDialogue =
    {
        " 반갑습니다.",
        "오늘",
        "어떤 하루였고,",
        "무슨 일이 있었나요? "
    };

    string[] playerMissDialogue =
    {
        " 반갑ㅅㅂ니다.",
        "오눌",
        "어던 하류였고,",
        "뮤슨 일이 잇엇나요? "
    };

    int currentBeat = 0;
    int playerBeat = 0;

    bool playerTurn = false;
    bool processingMiss = false;

    // 현재 진행 중인 채팅 말풍선
    GameObject currentCustomerBubbleObj;
    TMP_Text currentCustomerBubbleText;

    GameObject currentPlayerBubbleObj;
    TMP_Text currentPlayerBubbleText;


    void Start()
    {
        ResetNoteColors();

        musicSource.Play();

        StartCoroutine(ShowCustomerDialogue());
    }



    // =========================================================
    // Customer 대사 (음악 시간에 맞춰 진행)
    // =========================================================

    IEnumerator ShowCustomerDialogue()
    {
        CreateCustomerBubble();

        while (currentBeat < customerDialogue.Length)
        {
            // 지정한 노트 시간이 될 때까지 대기
            yield return new WaitUntil(
                () => musicSource.time >= customerNoteTimes[currentBeat]
            );

            // 현재 Customer 노트를 노란색으로 표시
            SetNoteColor(
                customerNotes,
                currentBeat,
                activeColor
            );

            // 채팅 말풍선에 단어 이어 붙이기
            currentCustomerBubbleText.text +=
                customerDialogue[currentBeat] + " ";

            ScrollToBottom();

            // 노트를 초록색으로 잠깐 표시
            StartCoroutine(
                FlashNoteColor(
                    customerNotes,
                    currentBeat,
                    hitColor,
                    0.2f
                )
            );

            currentBeat++;
        }

        // Customer 대사가 끝나면 Player 턴 시작
        CreatePlayerBubble();

        playerTurn = true;

        HighlightCurrentPlayerNote();

        Debug.Log("플레이어의 차례!");
    }



    // =========================================================
    // Player / Customer 입력
    // =========================================================

    void Update()
    {
        if (!playerTurn)
        {
            CheckCustomerWrongInput();
            return;
        }

        if (processingMiss)
        {
            CheckPlayerInputFeedback();
            return;
        }

        // 목표 시간을 놓치면(허용 오차를 넘기면) 자동 MISS
        if (musicSource.time > playerNoteTimes[playerBeat] + hitWindow)
        {
            MissNote();
            return;
        }

        // D
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (playerBeat == 0) CheckInputTiming();
            else WrongPlayerInput(0);
            return;
        }

        // F
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (playerBeat == 1) CheckInputTiming();
            else WrongPlayerInput(1);
            return;
        }

        // J
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (playerBeat == 2) CheckInputTiming();
            else WrongPlayerInput(2);
            return;
        }

        // K
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (playerBeat == 3) CheckInputTiming();
            else WrongPlayerInput(3);
            return;
        }
    }



    // =========================================================
    // 입력 타이밍 판정 (음악 시간 기준)
    // =========================================================

    void CheckInputTiming()
    {
        float targetTime = playerNoteTimes[playerBeat];
        float diff = musicSource.time - targetTime;

        Debug.Log("타이밍 오차: " + diff.ToString("F3") + "초");

        if (Mathf.Abs(diff) <= hitWindow)
        {
            PlayerInput();
        }
        else
        {
            MissNote();
        }
    }



    // =========================================================
    // Player가 올바른 타이밍에 눌렀을 때
    // =========================================================

    void PlayerInput()
    {
        if (playerBeat >= playerDialogue.Length) return;

        StartCoroutine(
            FlashNoteColor(
                playerNotes,
                playerBeat,
                hitColor,
                0.5f
            )
        );

        currentPlayerBubbleText.text +=
            playerDialogue[playerBeat] + " ";

        ScrollToBottom();

        playerBeat++;

        if (playerBeat < playerDialogue.Length)
        {
            HighlightCurrentPlayerNote();
        }
        else
        {
            playerTurn = false;
            Debug.Log("플레이어의 대사가 모두 끝났습니다.");
        }
    }



    // =========================================================
    // Player가 노트를 놓쳤을 때
    // =========================================================

    void MissNote()
    {
        if (processingMiss) return;

        processingMiss = true;
        StartCoroutine(ProcessMiss());
    }


    IEnumerator ProcessMiss()
    {
        Debug.Log("MISS!");

        if (playerBeat < playerNotes.Length)
        {
            playerNotes[playerBeat].color = missColor;
        }

        if (playerBeat < playerMissDialogue.Length)
        {
            currentPlayerBubbleText.text +=
                playerMissDialogue[playerBeat] + " ";

            ScrollToBottom();
        }

        // 다음 노트 타이밍까지 (또는 0.3초) 빨간색 유지
        float wait = 0.3f;
        if (playerBeat + 1 < playerNoteTimes.Length)
        {
            wait = Mathf.Max(
                0.1f,
                playerNoteTimes[playerBeat + 1] - musicSource.time - hitWindow
            );
        }

        yield return new WaitForSeconds(wait);

        if (playerBeat < playerNotes.Length)
        {
            playerNotes[playerBeat].color = normalColor;
        }

        playerBeat++;

        if (playerBeat < playerDialogue.Length)
        {
            HighlightCurrentPlayerNote();
        }
        else
        {
            playerTurn = false;
            Debug.Log("플레이어의 대사가 모두 끝났습니다.");
        }

        processingMiss = false;
    }



    // =========================================================
    // 채팅 말풍선 생성 (Scroll View Content에 새로 추가)
    // =========================================================

    void CreateCustomerBubble()
    {
        currentCustomerBubbleObj = Instantiate(customerBubblePrefab, chatContent);
        currentCustomerBubbleText = currentCustomerBubbleObj.GetComponentInChildren<TMP_Text>();
        currentCustomerBubbleText.text = "";

        ScrollToBottom();
    }

    void CreatePlayerBubble()
    {
        currentPlayerBubbleObj = Instantiate(playerBubblePrefab, chatContent);
        currentPlayerBubbleText = currentPlayerBubbleObj.GetComponentInChildren<TMP_Text>();
        currentPlayerBubbleText.text = "";

        ScrollToBottom();
    }

    // 새 메시지가 추가될 때마다 최신 메시지가 보이도록
    // 스크롤을 맨 아래로 이동 → 기존 메시지들은 자연스럽게 위로 밀려 올라감
    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }



    // =========================================================
    // 잘못된 키 입력 피드백
    // =========================================================

    void CheckPlayerInputFeedback()
    {
        if (Input.GetKeyDown(KeyCode.D)) { WrongPlayerInput(0); return; }
        if (Input.GetKeyDown(KeyCode.F)) { WrongPlayerInput(1); return; }
        if (Input.GetKeyDown(KeyCode.J)) { WrongPlayerInput(2); return; }
        if (Input.GetKeyDown(KeyCode.K)) { WrongPlayerInput(3); return; }
    }

    void CheckCustomerWrongInput()
    {
        if (Input.GetKeyDown(KeyCode.D)) { WrongPlayerInput(0); return; }
        if (Input.GetKeyDown(KeyCode.F)) { WrongPlayerInput(1); return; }
        if (Input.GetKeyDown(KeyCode.J)) { WrongPlayerInput(2); return; }
        if (Input.GetKeyDown(KeyCode.K)) { WrongPlayerInput(3); return; }
    }

    void WrongPlayerInput(int index)
    {
        if (index >= 0 && index < playerNotes.Length)
        {
            StartCoroutine(FlashWrongInput(playerNotes, index));
        }
    }

    IEnumerator FlashWrongInput(Image[] notes, int index)
    {
        if (index < 0 || index >= notes.Length) yield break;
        if (notes[index] == null) yield break;

        Color originalColor = notes[index].color;
        notes[index].color = inputcolor;

        yield return new WaitForSeconds(0.1f);

        notes[index].color = originalColor;
    }



    // =========================================================
    // 노트 색상 관련 유틸
    // =========================================================

    void HighlightCurrentPlayerNote()
    {
        if (playerBeat < playerNotes.Length)
        {
            SetNoteColor(playerNotes, playerBeat, activeColor);
        }
    }

    IEnumerator FlashNoteColor(Image[] notes, int index, Color targetColor, float duration)
    {
        if (index < 0 || index >= notes.Length) yield break;
        if (notes[index] == null) yield break;

        notes[index].color = targetColor;
        yield return new WaitForSeconds(duration);
        notes[index].color = normalColor;
    }

    void SetNoteColor(Image[] notes, int index, Color color)
    {
        if (index < 0 || index >= notes.Length) return;
        if (notes[index] != null) notes[index].color = color;
    }

    void ResetNoteColors()
    {
        for (int i = 0; i < 4; i++)
        {
            if (customerNotes[i] != null) customerNotes[i].color = normalColor;
            if (playerNotes[i] != null) playerNotes[i].color = normalColor;
        }
    }
}