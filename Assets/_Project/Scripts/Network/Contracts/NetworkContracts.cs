// Assets/_Project/Scripts/Network/Contracts/NetworkContracts.cs
using System;
using System.Collections.Generic;

namespace Math6Companion.Core.Contracts
{
    #region Data Models
    [Serializable]
    public class UserData
    {
        public string userId;
        public string username;
        public string fullName;
        public string role; // "STUDENT" or "TEACHER"
        public int grade;
        public string classId;
        public string nickname; // Bí danh của học sinh
    }

    [Serializable]
    public class QuestionOptionMap
    {
        public string A;
        public string B;
        public string C;
        public string D;

        public int Count => 4;

        public string this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return A;
                    case 1: return B;
                    case 2: return C;
                    case 3: return D;
                    default: return string.Empty;
                }
            }
            set
            {
                switch (index)
                {
                    case 0: A = value; break;
                    case 1: B = value; break;
                    case 2: C = value; break;
                    case 3: D = value; break;
                }
            }
        }
    }

    [Serializable]
    public class RubricItem
    {
        public int step;
        public string description;
        public int points;
    }

    [Serializable]
    public class QuestionData
    {
        public string questionId;
        public string lessonId;
        public string topic;
        public string type; // "MCQ" or "ESSAY"
        public string difficulty; // "EASY", "MEDIUM", "HARD"
        public string prompt;
        public QuestionOptionMap options;
        public string correctAnswer;
        public List<RubricItem> rubric;
        public string explanation;
        public int maxScore;
    }

    [Serializable]
    public class LessonData
    {
        public string lessonId;
        public string topic;
        public string title;
        public string description;
        public int grade;
        public string summaryText;
        public string videoUrl;
        public int userHighestScore;
        public string topStudentNickname;
        public int topStudentAttempts;
    }

    [Serializable]
    public class ResultSubmission
    {
        public string action = "saveResult";
        public string userId;
        public string lessonId;
        public string activityType;
        public int score;
        public int maxScore;
        public int timeSpentSeconds;
        public int mistakesCount;
    }

    [Serializable]
    public class BadgeData
    {
        public string badgeId;
        public string badgeCode;
        public string name;
        public string description;
        public string iconUrl;
    }

    [Serializable]
    public class StudentProgressData
    {
        public string lessonId;
        public int attempts;
        public int highestScore;
        public int lastScore;
        public bool isCompleted;
    }

    [Serializable]
    public class StudentAnalyticsData
    {
        public string userId;
        public string fullName;
        public int completedLessons;
        public float averageScore;
        public List<BadgeData> badges;
        public List<StudentProgressData> progressList;
    }
    #endregion

    #region AI Tutor Models
    [Serializable]
    public class TutorHistoryItem
    {
        public string role; // "tutor" or "student"
        public string text;
    }

    [Serializable]
    public class TutorTurnPayload
    {
        public string problemStatement;
        public int totalSteps;
        public int currentStepIndex;
        public string studentInput;
        public List<TutorHistoryItem> conversationHistory = new List<TutorHistoryItem>();
    }

    [Serializable]
    public class AITutorStepResponse
    {
        public bool isCorrect;
        public int scoreDelta;
        public string state; // "WAITING_INPUT", "STEP_PASSED", "TUTOR_COMPLETED"
        public string tutorDialogue;
        public string hint;
        public string highlightFormula;
        public int nextStepIndex;
        public bool shouldWaitForStudent;
        public bool isLessonCompleted;
    }

    [Serializable]
    public class EssaySubmissionPayload
    {
        public string questionId;
        public string prompt;
        public string studentSteps;
        public List<RubricItem> rubric;
    }

    [Serializable]
    public class StepAnalysisItem
    {
        public int step;
        public bool isCorrect;
        public string comment;
    }

    [Serializable]
    public class AIGradingResponse
    {
        public int score;
        public int maxScore;
        public bool passed;
        public string detectedMistakeType;
        public List<StepAnalysisItem> stepAnalysis;
        public string pedagogicalFeedback;
        public string remedialExercise;
    }
    #endregion

    #region Service Interfaces
    public interface INetworkService
    {
        void Login(string username, string passwordHash, Action<bool, UserData, string> callback);
        void GetLessons(string topic, Action<bool, List<LessonData>, string> callback);
        void GetQuestions(string topic, string grade, Action<bool, List<QuestionData>, string> callback);
        void SaveResult(ResultSubmission submission, Action<bool, List<BadgeData>, string> callback);
        void GetStudentAnalytics(string studentId, Action<bool, StudentAnalyticsData, string> callback);
    }

    public interface IAITutorService
    {
        void SendTutorTurn(TutorTurnPayload payload, Action<bool, AITutorStepResponse, string> callback);
        void GradeEssay(EssaySubmissionPayload payload, Action<bool, AIGradingResponse, string> callback);
    }
    #endregion
}
