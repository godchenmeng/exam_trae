namespace ExamSystem.Domain.Enums
{

/// <summary>
/// 题目类型枚举
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// 单选题
    /// </summary>
    SingleChoice = 1,

    /// <summary>
    /// 多选题
    /// </summary>
    MultipleChoice = 2,

    /// <summary>
    /// 判断题
    /// </summary>
    TrueFalse = 3,

    /// <summary>
    /// 填空题
    /// </summary>
    FillInBlank = 4,

    /// <summary>
    /// 简答题
    /// </summary>
    ShortAnswer = 5,

    /// <summary>
    /// 论述题
    /// </summary>
    Essay = 6
    }
}
