// QuestionManager.cs
// Singleton duy nhất quản lý:
// - Load dữ liệu câu hỏi từ JSON (chỉ load 1 lần)
// - Hiển thị / ẩn UI câu hỏi (dùng chung cho mọi chướng ngại vật)
// - Kiểm tra đáp án và gọi hiệu ứng / buff tương ứng
// - Tính điểm quiz qua PlayerScoreManager
//
// Mỗi chướng ngại vật chỉ cần gắn QuestionTrigger.cs (nhẹ, không chứa UI/JSON)
// và gọi QuestionManager.Instance.ShowQuestion(trigger, player) khi va chạm.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    // =========================================================
    // UI
    // =========================================================
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
    // =========================================================
    // QUESTION DATA
    // =========================================================
    [Header("Question")]
    [SerializeField] private TextAsset questionJson;

    [Header("Quiz Scoring")]
    [Tooltip("Thời gian giới hạn cho mỗi câu hỏi (giây) - dùng để tính time bonus.")]
    [SerializeField] private float questionTimeLimit = 10f;

    private QuestionData[] questions;

    // =========================================================
    // INTERNAL STATE
    // =========================================================
    private bool isAnswered = false;
    private int currentCorrectAnswer;

    // Lưu lại để dựng AnswerResult khi CheckAnswer() được gọi
    private int currentDifficulty;
    private float questionStartTime;

    private bool isQuestionAvailable = false; // Có câu hỏi đang chờ trả lời không
    private bool isQuestionVisible = false;   // UI câu hỏi đang hiện hay đang tạm ẩn

    private GameObject player;
    private QuestionTrigger currentTrigger; // Chướng ngại vật đang hỏi (để báo kết quả trở lại)

    // =========================================================
    // UNITY
    // =========================================================

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

    // =========================================================
    // ĐƯỢC GỌI TỪ QuestionTrigger KHI PLAYER VA CHẠM
    // =========================================================

    // Trả về true nếu câu hỏi được chấp nhận hiển thị,
    // false nếu đang có câu hỏi khác chưa được trả lời (trigger sẽ không bị khóa).
    public bool ShowQuestion(QuestionTrigger trigger, GameObject playerObj)
    {
        // Đang có câu hỏi khác chưa trả lời -> từ chối, không cho hỏi chồng lên nhau
        if (isQuestionAvailable)
        {
            Debug.Log("Đang có câu hỏi khác chưa trả lời, bỏ qua trigger mới.");
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

        int randomIndex = Random.Range(0, questions.Length);
        QuestionData question = questions[randomIndex];

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

        // Lưu thông tin cần thiết để tính điểm khi CheckAnswer() được gọi
        currentDifficulty = question.difficulty;
        questionStartTime = Time.time;

        isQuestionAvailable = true;
        HideAndShowButton();

        return true;
    }

    // =========================================================
    // TOGGLE ẨN/HIỆN TẠM THỜI (gọi từ HideAndShowQuestionButton)
    // =========================================================

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

    // =========================================================
    // ANSWER
    // =========================================================

    private void CheckAnswer(int selectedAnswer)
    {
        if (isAnswered)
            return;

        isAnswered = true;

        bool isCorrect = selectedAnswer == currentCorrectAnswer;

        // ---- Đóng gói kết quả trả lời + gửi cho PlayerScoreManager ----
        // Gọi 1 LẦN DUY NHẤT cho cả 2 trường hợp đúng/sai: AddQuizScore() tự lo
        // việc cộng điểm (nếu đúng) và reset streak (nếu sai), không cần if ở đây.
        float timeTaken = Time.time - questionStartTime;
        AnswerResult result = new AnswerResult(isCorrect, timeTaken, questionTimeLimit, currentDifficulty);
        PlayerScoreManager.Instance?.AddQuizScore(result);

        PlayerStats playerstats = player != null ? player.GetComponent<PlayerStats>() : null;

        if (isCorrect)
        {
            Debug.Log("Đúng!");

            if (SpawnEffect.Instance != null)
                SpawnEffect.Instance.PlayCorrectEffect(player);

            if (playerstats != null && speedBuffAsset != null)
                speedBuffAsset.ApplyBuff(playerstats);

            currentTrigger?.OnAnsweredCorrectly();
        }
        else
        {
            Debug.Log("Sai!!");

            if (SpawnEffect.Instance != null)
                SpawnEffect.Instance.PlayWrongEffect(player);

            if (playerstats != null && slowDebuffAsset != null)
                slowDebuffAsset.ApplyBuff(playerstats);

            currentTrigger?.OnAnsweredWrong();
        }

        HideQuestionUI();

        isQuestionAvailable = false;
        isQuestionVisible = false;
        HideAndShowButton();

        currentTrigger = null;
    }

    // =========================================================
    // UI
    // =========================================================

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

    // =========================================================
    // JSON DATA CLASS
    // =========================================================

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