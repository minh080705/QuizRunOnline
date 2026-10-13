/// <summary>
/// Loại câu hỏi mà một QuestionTrigger (prefab câu hỏi) sẽ hỏi. Khớp với trường "category" trong Resources/questions.json
/// ("math", "english"). Any = trộn mọi loại (mặc định, giữ hành vi cũ cho prefab chưa chỉnh).
/// </summary>
public enum QuestionCategory
{
    Any = 0,
    Math = 1,
    English = 2,
}
