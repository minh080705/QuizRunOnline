/// <summary>
/// Quản lý câu hỏi: nạp JSON theo loại, hiển thị UI, chấm đáp án rồi áp buff và tính điểm.
/// Chướng ngại vật chỉ cần QuestionTrigger gọi ShowQuestion khi va chạm.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [SerializeField] private GameObject screenQuestion;
    [Header("UI")]
    [SerializeField] private GameObject background;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button answerButton1;
    [SerializeField] private Button answerButton2;
    [SerializeField] private Button HideAndShowQuestionButton;

    [Header("Buff/Debuff References")]
    [SerializeField] private SpeedBuff speedBuffAsset;
    [SerializeField] private SlowDebuff slowDebuffAsset;
    [Header("Question")]
    [SerializeField] private TextAsset questionJson;

    [Header("Quiz Scoring")]
    [Tooltip("Thời gian tối đa để trả lời mỗi câu (giây). Hết giờ coi như trả lời sai. Trả lời càng sớm càng được nhiều điểm thưởng tốc độ.")]
    [SerializeField] private float answerTimeLimit = 5f;

    [Tooltip("Tùy chọn: text hiện số giây còn lại. Đặt bên trong ScreenQuestion để tự ẩn/hiện theo câu hỏi.")]
    [SerializeField] private TextMeshProUGUI timerText;

    private QuestionData[] questions;

    // Mỗi loại câu hỏi một "túi" xáo trộn: hỏi hết túi mới lặp lại câu cũ (xem QuestionBag).
    private readonly Dictionary<QuestionCategory, QuestionBag> bags = new Dictionary<QuestionCategory, QuestionBag>();

    private bool isAnswered = false;
    private int currentCorrectAnswer;

    private int currentDifficulty;
    private float questionStartTime;

    private bool isQuestionAvailable = false; // Có câu hỏi đang chờ trả lời không
    private bool isQuestionVisible = false;   // UI câu hỏi đang hiện hay đang tạm ẩn

    private GameObject player;
    private QuestionTrigger currentTrigger; // Chướng ngại vật đang hỏi (để báo kết quả trở lại)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (questionJson == null)
        {
            questionJson = Resources.Load<TextAsset>("questions");
        }

        if (questionJson == null)
        {
            return;
        }

        QuestionList questionList = JsonUtility.FromJson<QuestionList>(questionJson.text);

        if (questionList == null || questionList.questions == null)
        {
            Debug.LogError("Không thể đọc dữ liệu questions.json");
            return;
        }

        questions = questionList.questions;
        BuildBags();
    }

    // Chia chỉ số câu hỏi theo trường "category" (không phân biệt hoa thường). Câu có loại lạ chỉ nằm trong nhóm Any.
    private void BuildBags()
    {
        var all = new List<int>(questions.Length);
        var math = new List<int>();
        var english = new List<int>();
        int unknown = 0;

        for (int i = 0; i < questions.Length; i++)
        {
            all.Add(i);

            switch ((questions[i].category ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "math": math.Add(i); break;
                case "english": english.Add(i); break;
                default: unknown++; break;
            }
        }

        if (unknown > 0)
            Debug.LogWarning($"[Question] {unknown} câu có category không phải math/english, chỉ được hỏi ở prefab loại Any.");

        bags[QuestionCategory.Any] = new QuestionBag(all);
        bags[QuestionCategory.Math] = new QuestionBag(math);
        bags[QuestionCategory.English] = new QuestionBag(english);
    }

    // Chọn chỉ số câu hỏi kế tiếp của loại yêu cầu; loại đó không có câu nào thì rơi về kho trộn.
    private int PickQuestionIndex(QuestionCategory category)
    {
        if (!bags.TryGetValue(category, out QuestionBag bag) || bag.Count == 0)
        {
            if (category != QuestionCategory.Any)
                Debug.LogWarning($"[Question] Không có câu hỏi loại {category}, dùng kho trộn.");
            bag = bags[QuestionCategory.Any];
        }
        return bag.Next();
    }

    private void Start()
    {
        HideQuestionUI();

        isQuestionAvailable = false;
        isQuestionVisible = false;
        HideAndShowButton();

        answerButton1.onClick.AddListener(() => CheckAnswer(0));
        answerButton2.onClick.AddListener(() => CheckAnswer(1));
    }

    // Đếm giờ: chạy cả khi người chơi tạm ẩn câu hỏi bằng nút toggle. Hết giờ -> tính là trả lời sai.
    private void Update()
    {
        if (!isQuestionAvailable || isAnswered)
            return;

        float remaining = answerTimeLimit - (Time.time - questionStartTime);

        if (timerText != null)
            timerText.text = Mathf.Max(0, Mathf.CeilToInt(remaining)).ToString();

        if (remaining <= 0f)
        {
            CheckAnswer(-1); // -1 không khớp đáp án nào -> sai
        }
    }

    // Trả về true nếu câu hỏi được chấp nhận hiển thị,
    // false nếu đang có câu hỏi khác chưa được trả lời (trigger sẽ không bị khóa).
    public bool ShowQuestion(QuestionTrigger trigger, GameObject playerObj)
    {
        if (isQuestionAvailable)
        {
            return false;
        }

        if (questions == null || questions.Length == 0)
        {
            Debug.LogError("Không có câu hỏi nào.");
            return false;
        }

        currentTrigger = trigger;
        player = playerObj;
        isAnswered = false;

        screenQuestion.SetActive(true);
        isQuestionVisible = true;

        QuestionCategory category = trigger != null ? trigger.Category : QuestionCategory.Any;
        QuestionData question = questions[PickQuestionIndex(category)];

        questionText.text = question.question;

        if (question.options != null && question.options.Length >= 2)
        {
            answerButton1.GetComponentInChildren<TextMeshProUGUI>().text = question.options[0];
            answerButton2.GetComponentInChildren<TextMeshProUGUI>().text = question.options[1];
        }

        background.SetActive(true);
        questionText.gameObject.SetActive(true);
        answerButton1.gameObject.SetActive(true);
        answerButton2.gameObject.SetActive(true);

        currentCorrectAnswer = question.correctAnswer;

        currentDifficulty = question.difficulty;
        questionStartTime = Time.time;

        isQuestionAvailable = true;
        HideAndShowButton();

        // Dừng hẳn nhân vật trong lúc trả lời (tốc độ cũ được PlayerStats lưu lại).
        PlayerStats frozenStats = playerObj != null ? playerObj.GetComponent<PlayerStats>() : null;
        if (frozenStats != null)
            frozenStats.FreezeForQuestion();

        return true;
    }

    public void HideAndShowQuestion()
    {
        if (!isQuestionAvailable)
            return; // không có câu hỏi thì không có gì để toggle

        if (isQuestionVisible)
        {
            screenQuestion.SetActive(false);
            isQuestionVisible = false;
        }
        else
        {
            screenQuestion.SetActive(true);
            isQuestionVisible = true;
        }
    }

    private void HideAndShowButton()
    {
        if (HideAndShowQuestionButton == null) return;
        HideAndShowQuestionButton.gameObject.SetActive(isQuestionAvailable);
    }

    private void CheckAnswer(int selectedAnswer)
    {
        if (isAnswered)
            return;

        isAnswered = true;

        bool isCorrect = selectedAnswer == currentCorrectAnswer;

        // Gọi 1 LẦN DUY NHẤT cho cả 2 trường hợp đúng/sai: AddQuizScore() tự lo
        // việc cộng điểm (nếu đúng) và reset streak (nếu sai), không cần if ở đây.
        float timeTaken = Mathf.Min(Time.time - questionStartTime, answerTimeLimit);
        AnswerResult result = new AnswerResult(isCorrect, timeTaken, answerTimeLimit, currentDifficulty);
        PlayerScoreManager.Instance?.AddQuizScore(result);

        PlayerStats playerstats = player != null ? player.GetComponent<PlayerStats>() : null;

        // Chạy lại với tốc độ cũ TRƯỚC khi áp buff/debuff (buff snapshot sideSpeed lúc áp dụng).
        if (playerstats != null)
            playerstats.UnfreezeAfterQuestion();

        if (isCorrect)
        {
            if (SpawnEffect.Instance != null)
                SpawnEffect.Instance.PlayCorrectEffect(player);

            // Không áp buff lên nhân vật đang chết: buff chụp sideSpeed lúc áp dụng (đang = 0) và sẽ khôi phục sai về 0.
            if (playerstats != null && !playerstats.isDead && speedBuffAsset != null)
                speedBuffAsset.ApplyBuff(playerstats);

            currentTrigger?.OnAnsweredCorrectly();
        }
        else
        {
            if (SpawnEffect.Instance != null)
                SpawnEffect.Instance.PlayWrongEffect(player);

            if (playerstats != null && !playerstats.isDead && slowDebuffAsset != null)
                slowDebuffAsset.ApplyBuff(playerstats);

            currentTrigger?.OnAnsweredWrong();
        }

        HideQuestionUI();

        isQuestionAvailable = false;
        isQuestionVisible = false;
        HideAndShowButton();

        currentTrigger = null;
    }

    private void HideQuestionUI()
    {
        if (screenQuestion != null)
            screenQuestion.SetActive(false);

        if (background != null)
            background.SetActive(false);

        if (questionText != null)
            questionText.gameObject.SetActive(false);

        if (answerButton1 != null)
            answerButton1.gameObject.SetActive(false);

        if (answerButton2 != null)
            answerButton2.gameObject.SetActive(false);
    }

    /// <summary>
    /// Túi câu hỏi xáo trộn: lấy lần lượt hết túi rồi mới trộn lại, nên một câu không lặp trước khi hỏi hết các câu còn lại.
    /// Lượt mới không bắt đầu bằng đúng câu cuối của lượt trước.
    /// </summary>
    private class QuestionBag
    {
        private readonly List<int> indices;
        private readonly List<int> order = new List<int>();
        private int cursor;
        private int last = -1;

        public int Count => indices.Count;

        public QuestionBag(List<int> indices)
        {
            this.indices = indices;
        }

        public int Next()
        {
            if (indices.Count == 0) return -1;
            if (cursor >= order.Count) Reshuffle();

            last = order[cursor++];
            return last;
        }

        private void Reshuffle()
        {
            order.Clear();
            order.AddRange(indices);

            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            // Các chỉ số trong túi đều khác nhau nên đổi chỗ với phần tử cuối là đủ để tránh trùng câu vừa hỏi.
            if (order.Count > 1 && order[0] == last)
                (order[0], order[order.Count - 1]) = (order[order.Count - 1], order[0]);

            cursor = 0;
        }
    }

    [System.Serializable]
    private class QuestionList
    {
        public QuestionData[] questions;
    }

    [System.Serializable]
    private class QuestionData
    {
        public int id;
        public string category;
        public string type;
        public string question;
        public string[] options;
        public int correctAnswer;
        public int difficulty;
    }
}