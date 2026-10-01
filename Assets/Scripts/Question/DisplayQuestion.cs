//// bật tắt giao diện câu hỏi khi player đi vào trigger,
//// đọc dữ liệu câu hỏi từ JSON,
//// kiểm tra đáp án và trả về vị trí player,
//// còn spawneffect sẽ chuyển sang file khác để quán lý


//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class DisplayQuestion : MonoBehaviour
//{
//    // =========================================================
//    // UI
//    // =========================================================
//    [SerializeField] private GameObject screenQuestion;
//    [Header("UI")]
//    [SerializeField] private GameObject background;
//    [SerializeField] private TextMeshProUGUI questionText;
//    [SerializeField] private Button answerButton1;
//    [SerializeField] private Button answerButton2;
//    [SerializeField] private Button HideAndShowQuestionButton;
//    [Header("Buff/Debuff References")]
//    [SerializeField] private SpeedBuff speedBuffAsset;
//    [SerializeField] private SlowDebuff slowDebuffAsset;

//    // =========================================================
//    // QUESTION DATA
//    // =========================================================

//    [Header("Question")]
//    [SerializeField] private TextAsset questionJson;

//    [Header("Quiz Scoring")]
//    [Tooltip("Thời gian giới hạn cho mỗi câu hỏi (giây) - dùng để tính time bonus.")]
//    [SerializeField] private float questionTimeLimit = 10f;


//    // =========================================================
//    // INTERNAL DATA
//    // =========================================================

//    private QuestionData[] questions;

//    private bool isAnswered = false;
//    private bool isTriggered = false;

//    bool isQuestionAvailable = false; // Biến kiểm tra xem có câu hỏi hay không
//    public GameObject player;

//    // =========================================================
//    // UNITY
//    // =========================================================

//    private void Awake()
//    {
//        //HideAndShowQuestionButton.gameObject.SetActive(false);
//        if (questionJson == null)
//        {
//            questionJson = Resources.Load<TextAsset>("questions");
//        }

//        if (questionJson == null)
//        {
//            Debug.LogError("Không tìm thấy questions.json trong Assets/Resources/");
//            return;
//        }

//        // Đọc JSON
//        QuestionList questionList =
//            JsonUtility.FromJson<QuestionList>(questionJson.text);

//        if (questionList == null || questionList.questions == null)
//        {
//            Debug.LogError("Không thể đọc dữ liệu questions.json");
//            return;
//        }

//        questions = questionList.questions;

//        Debug.Log($"Đã load {questions.Length} câu hỏi.");
//    }


//    private void Start()
//    {
//        // Ban đầu ẩn UI
//        HideQuestionUI();

//        // Chưa có câu hỏi nào -> ẩn luôn nút toggle
//        isQuestionAvailable = false;
//        HideAndShowButton();

//        // Đăng ký sự kiện cho 2 button
//        answerButton1.onClick.AddListener(() => CheckAnswer(0));
//        answerButton2.onClick.AddListener(() => CheckAnswer(1));
//    }


//    private void OnTriggerEnter(Collider other)
//    {
//        if (!other.CompareTag("Player"))
//            return;

//        if (isTriggered)
//            return;

//        screenQuestion.SetActive(true);
//        isQuestionVisible = true; // đồng bộ trạng thái, tránh lệch khi bấm nút toggle lần đầu
//        player = other.gameObject;

//        ShowRandomQuestion();
//    }

//    void HideAndShowButton()
//    {
//        if (isQuestionAvailable)
//        {
//            HideAndShowQuestionButton.gameObject.SetActive(true);
//        }
//        else
//        {
//            HideAndShowQuestionButton.gameObject.SetActive(false);
//        }
//    }

//    // =========================================================
//    // QUESTION
//    // =========================================================

//    private void ShowRandomQuestion()
//    {
//        if (questions == null || questions.Length == 0)
//        {
//            Debug.LogError("Không có câu hỏi nào.");
//            return;
//        }

//        // Đánh dấu object đã được kích hoạt
//        isTriggered = true;

//        isAnswered = false;

//        // Lấy câu hỏi ngẫu nhiên
//        int randomIndex = Random.Range(0, questions.Length);

//        QuestionData question = questions[randomIndex];

//        // Hiển thị câu hỏi
//        questionText.text = question.question;

//        // Hiển thị 2 đáp án từ JSON
//        if (question.options != null && question.options.Length >= 2)
//        {
//            answerButton1.GetComponentInChildren<TextMeshProUGUI>().text =
//                question.options[0];

//            answerButton2.GetComponentInChildren<TextMeshProUGUI>().text =
//                question.options[1];
//        }

//        // Hiện UI
//        background.SetActive(true);
//        questionText.gameObject.SetActive(true);
//        answerButton1.gameObject.SetActive(true);
//        answerButton2.gameObject.SetActive(true);

//        // Lưu đáp án đúng của câu hiện tại
//        currentCorrectAnswer = question.correctAnswer;

//        // Lưu thông tin cần thiết để tính điểm khi CheckAnswer() được gọi
//        currentDifficulty = question.difficulty;
//        questionStartTime = Time.time;

//        //isQuestionAvailable
//        isQuestionAvailable = true;
//        HideAndShowButton();
//    }

//    private bool isQuestionVisible = false; // biến kiểm tra xem câu hỏi đang hiển thị hay không
//    public void HideAndShowQuestion()
//    {
//        if (isQuestionVisible)
//        {
//            screenQuestion.SetActive(false);
//            isQuestionVisible = false;
//        }
//        else
//        {
//            screenQuestion.SetActive(true);
//            isQuestionVisible = true;
//        }

//    }


//    // =========================================================
//    // ANSWER
//    // =========================================================


//    private int currentCorrectAnswer;
//    private int currentDifficulty;
//    private float questionStartTime;

//    public bool isAnswerCorrect;


//    private void CheckAnswer(int selectedAnswer)
//    {

//        if (isAnswered)
//            return;

//        isAnswered = true;

//        bool isCorrect = selectedAnswer == currentCorrectAnswer;
//        isAnswerCorrect = isCorrect;

//        // ---- Đóng gói kết quả trả lời + gửi cho PlayerScoreManager ----
//        // Gọi 1 LẦN DUY NHẤT cho cả 2 trường hợp đúng/sai: AddQuizScore() tự lo
//        // việc cộng điểm (nếu đúng) và reset streak (nếu sai), không cần if ở đây.
//        float timeTaken = Time.time - questionStartTime;
//        AnswerResult result = new AnswerResult(isCorrect, timeTaken, questionTimeLimit, currentDifficulty);
//        PlayerScoreManager.Instance?.AddQuizScore(result);
//        Debug.LogWarning("Đã có result");

//        if (isCorrect)
//        {
//            Debug.Log("Đúng!");

//            if (SpawnEffect.Instance != null)
//                SpawnEffect.Instance.PlayCorrectEffect(player);

//            PlayerStats playerstats = player.GetComponent<PlayerStats>();
//            if (playerstats != null)
//                speedBuffAsset.ApplyBuff(playerstats);

//            HideQuestionUI();
//        }
//        else
//        {
//            if (SpawnEffect.Instance != null)
//                SpawnEffect.Instance.PlayWrongEffect(player);

//            PlayerStats playerstats = player.GetComponent<PlayerStats>();
//            if (playerstats != null)
//                slowDebuffAsset.ApplyBuff(playerstats);

//            HideQuestionUI();

//            HideQuestionUI();
//        }

//        // Trả lời xong -> không còn câu hỏi để tạm ẩn/hiện nữa
//        isQuestionAvailable = false;
//        isQuestionVisible = false;
//        HideAndShowButton();
//    }


//    // =========================================================
//    // UI
//    // =========================================================

//    private void HideQuestionUI()
//    {
//        if (background != null)
//            background.SetActive(false);

//        if (questionText != null)
//            questionText.gameObject.SetActive(false);

//        if (answerButton1 != null)
//            answerButton1.gameObject.SetActive(false);

//        if (answerButton2 != null)
//            answerButton2.gameObject.SetActive(false);
//    }


//    // =========================================================
//    // JSON DATA CLASS
//    // =========================================================

//    [System.Serializable]
//    private class QuestionList
//    {
//        public QuestionData[] questions;
//    }


//    [System.Serializable]
//    private class QuestionData
//    {
//        public int id;
//        public string category;
//        public string type;
//        public string question;
//        public string[] options;
//        public int correctAnswer;
//        public int difficulty;
//    }
//}