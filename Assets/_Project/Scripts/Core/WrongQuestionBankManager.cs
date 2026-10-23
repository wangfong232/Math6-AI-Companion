using System;
using System.Collections.Generic;
using UnityEngine;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;

namespace Math6Companion.Core
{
    [Serializable]
    public class WrongQuestionItem
    {
        public string questionId;
        public string prompt;
        public List<string> options = new List<string>();
        public string correctAnswer;
        public string studentAnswer;
        public string explanation;
        public string activityType; // "MCQ" or "ESSAY"
        public string topic;
        public string difficulty;
        public int mistakeCount;
        public int reviewCorrectCount; // Spaced repetition: đúng 2 lần liên tiếp -> mastered
        public string lastFailedTime;
    }

    [Serializable]
    public class WrongQuestionBankData
    {
        public List<WrongQuestionItem> items = new List<WrongQuestionItem>();
    }

    /// <summary>
    /// Quản lý Sổ tay Câu hỏi Sai (Anki / Quizlet spaced repetition) cho Học sinh.
    /// Tự động lưu lại các câu trắc nghiệm / tự luận làm sai để ôn tập lại.
    /// </summary>
    public static class WrongQuestionBankManager
    {
        private const string PREFS_KEY_PREFIX = "Math6_WrongQuestionBank_";
        private static WrongQuestionBankData cachedData;

        private static string GetStorageKey()
        {
            string userId = PlayerPrefs.GetString("User_ID", "STU_001");
            return PREFS_KEY_PREFIX + userId;
        }

        public static void Load()
        {
            try
            {
                string key = GetStorageKey();
                string json = PlayerPrefs.GetString(key, "");
                if (!string.IsNullOrEmpty(json))
                {
                    cachedData = JsonUtility.FromJson<WrongQuestionBankData>(json);
                }
                if (cachedData == null)
                {
                    cachedData = new WrongQuestionBankData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WrongQuestionBankManager] Load error: " + ex.Message);
                cachedData = new WrongQuestionBankData();
            }
        }

        public static void Save()
        {
            try
            {
                if (cachedData == null) cachedData = new WrongQuestionBankData();
                string key = GetStorageKey();
                string json = JsonUtility.ToJson(cachedData);
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WrongQuestionBankManager] Save error: " + ex.Message);
            }
        }

        public static void RecordWrongMCQ(QuestionData q, string studentAnswer)
        {
            if (q == null) return;
            if (cachedData == null) Load();

            string qId = !string.IsNullOrEmpty(q.questionId) ? q.questionId : ("Q_" + q.prompt.GetHashCode());
            var existing = cachedData.items.Find(x => x.questionId == qId || x.prompt == q.prompt);

            var optList = new List<string>();
            if (q.options != null)
            {
                optList.Add(q.options.A ?? "");
                optList.Add(q.options.B ?? "");
                optList.Add(q.options.C ?? "");
                optList.Add(q.options.D ?? "");
            }

            if (existing != null)
            {
                existing.mistakeCount++;
                existing.studentAnswer = studentAnswer;
                existing.reviewCorrectCount = 0; // reset tiến trình ôn tập
                existing.lastFailedTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                existing.options = optList;
                existing.correctAnswer = q.correctAnswer;
                existing.explanation = q.explanation;
            }
            else
            {
                var item = new WrongQuestionItem
                {
                    questionId = qId,
                    prompt = q.prompt,
                    options = optList,
                    correctAnswer = q.correctAnswer,
                    studentAnswer = studentAnswer,
                    explanation = q.explanation,
                    activityType = "MCQ",
                    topic = q.topic,
                    difficulty = q.difficulty,
                    mistakeCount = 1,
                    reviewCorrectCount = 0,
                    lastFailedTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };
                cachedData.items.Add(item);
            }

            Save();
            Debug.Log($"[WrongQuestionBank] Đã lưu câu sai: {q.prompt} (Tổng: {cachedData.items.Count} câu)");
        }

        public static void RecordWrongEssay(string prompt, string studentSolution, string teacherSolution, string rubricFeedback, string topic = "ALL", string difficulty = "MEDIUM")
        {
            if (string.IsNullOrEmpty(prompt)) return;
            if (cachedData == null) Load();

            string qId = "ESSAY_" + prompt.GetHashCode();
            var existing = cachedData.items.Find(x => x.questionId == qId || x.prompt == prompt);

            if (existing != null)
            {
                existing.mistakeCount++;
                existing.studentAnswer = studentSolution;
                existing.explanation = rubricFeedback;
                existing.lastFailedTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
            else
            {
                var item = new WrongQuestionItem
                {
                    questionId = qId,
                    prompt = prompt,
                    correctAnswer = teacherSolution,
                    studentAnswer = studentSolution,
                    explanation = rubricFeedback,
                    activityType = "ESSAY",
                    topic = topic,
                    difficulty = difficulty,
                    mistakeCount = 1,
                    reviewCorrectCount = 0,
                    lastFailedTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };
                cachedData.items.Add(item);
            }

            Save();
        }

        public static void RecordCorrectReview(string questionId)
        {
            if (cachedData == null) Load();
            var item = cachedData.items.Find(x => x.questionId == questionId);
            if (item != null)
            {
                item.reviewCorrectCount++;
                // Nếu đã ôn tập đúng từ 2 lần trở lên -> xóa khỏi danh sách cần ôn tập
                if (item.reviewCorrectCount >= 2)
                {
                    cachedData.items.Remove(item);
                    Debug.Log($"[WrongQuestionBank] Học sinh đã thành thạo câu {questionId}, đã gỡ khỏi Sổ tay câu sai!");
                }
                Save();
            }
        }

        public static List<QuestionData> GetWrongMCQQuestions()
        {
            if (cachedData == null) Load();
            var result = new List<QuestionData>();

            foreach (var item in cachedData.items)
            {
                if (item.activityType == "MCQ")
                {
                    var optMap = new QuestionOptionMap();
                    if (item.options != null)
                    {
                        if (item.options.Count > 0) optMap.A = item.options[0];
                        if (item.options.Count > 1) optMap.B = item.options[1];
                        if (item.options.Count > 2) optMap.C = item.options[2];
                        if (item.options.Count > 3) optMap.D = item.options[3];
                    }

                    result.Add(new QuestionData
                    {
                        questionId = item.questionId,
                        prompt = item.prompt,
                        options = optMap,
                        correctAnswer = item.correctAnswer,
                        explanation = item.explanation,
                        type = "MCQ",
                        topic = item.topic,
                        difficulty = item.difficulty
                    });
                }
            }

            return result;
        }

        public static List<WrongQuestionItem> GetAllWrongItems()
        {
            if (cachedData == null) Load();
            return new List<WrongQuestionItem>(cachedData.items);
        }

        public static int GetTotalWrongCount()
        {
            if (cachedData == null) Load();
            return cachedData.items.Count;
        }

        public static int GetMasteredCount()
        {
            if (cachedData == null) Load();
            return cachedData.items.FindAll(x => x.reviewCorrectCount >= 2).Count;
        }

        public static int GetNeedReviewCount()
        {
            if (cachedData == null) Load();
            return cachedData.items.FindAll(x => x.reviewCorrectCount < 2).Count;
        }

        public static void AddSampleWrongQuestions()
        {
            if (cachedData == null) Load();

            if (cachedData.items.Count == 0)
            {
                RecordWrongMCQ(new QuestionData
                {
                    questionId = "Q_SAMPLE_01",
                    topic = "LuyThua",
                    difficulty = "MEDIUM",
                    prompt = "Viết kết quả phép tính sau dưới dạng một luỹ thừa: 2^4 . 2^5 = ?",
                    options = new QuestionOptionMap { A = "2^9", B = "4^9", C = "2^20", D = "4^20" },
                    correctAnswer = "A",
                    explanation = "Khi nhân hai luỹ thừa cùng cơ số, giữ nguyên cơ số và cộng các số mũ: 4 + 5 = 9. Kết quả: 2^9."
                }, "B");

                RecordWrongMCQ(new QuestionData
                {
                    questionId = "Q_SAMPLE_02",
                    topic = "ThuTuPhepTinh",
                    difficulty = "MEDIUM",
                    prompt = "Tính giá trị của biểu thức: 14 + 2 . 8^2 = ?",
                    options = new QuestionOptionMap { A = "142", B = "1024", C = "130", D = "140" },
                    correctAnswer = "A",
                    explanation = "Ưu tiên luỹ thừa: 8^2 = 64. Nhân: 2 . 64 = 128. Cộng: 14 + 128 = 142."
                }, "B");

                RecordWrongMCQ(new QuestionData
                {
                    questionId = "Q_SAMPLE_03",
                    topic = "SoNguyen",
                    difficulty = "EASY",
                    prompt = "Tính giá trị của phép tính: (-15) + (-8) = ?",
                    options = new QuestionOptionMap { A = "-23", B = "23", C = "-7", D = "7" },
                    correctAnswer = "A",
                    explanation = "Cộng hai số nguyên cùng dấu âm: -(15 + 8) = -23."
                }, "D");
            }
        }

        public static void RemoveMasteredItems()
        {
            if (cachedData == null) Load();
            cachedData.items.RemoveAll(x => x.reviewCorrectCount >= 2);
            Save();
        }

        public static void ClearAll()
        {
            if (cachedData == null) cachedData = new WrongQuestionBankData();
            cachedData.items.Clear();
            Save();
        }
    }
}
