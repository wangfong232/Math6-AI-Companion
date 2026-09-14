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
    }

    [Serializable]
    public class QuestionOptionMap
    {
        public string A;
        public string B;
        public string C;
        public string D;
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
