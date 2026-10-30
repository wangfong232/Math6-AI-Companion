// =========================================================================
// GOOGLE APPS SCRIPT BACKEND CHUẨN HOÁ CHO MATH6-AI-COMPANION
// Khớp 100% với cấu trúc 8 bảng:
// 1. Users (UserID, Username, Password, Role, FullName, Grade, ClassID, CreatedAt)
// 2. Lessons (LessonID, Topic, Title, Grade, OrderIndex, Summary, Objectives, VideoUrl, ContentText)
// 3. Questions_Math6 (QuestionID, LessonID, Topic, Type, Difficulty, Prompt, OptA, OptB, OptC, OptD, CorrectAnswer, RubricJSON, Explanation, ImgUrl, Status)
// 4. Results (ResultID, UserID, LessonID, ActivityType, Score, MaxScore, TimeSpentSec, MistakesCount, SubmittedAt)
// 5. Progress (ProgressID, UserID, LessonID, Attempts, HighestScore, LastScore, Completed, LastAttemptDate)
// 6. Badges (BadgesID, BadgeCode, Name, Description, IconURL, ConditionType, Threshold)
// 7. StudentBadges (StudentBadgeID, UserID, BadgeCode, UnlockedAt)
// 8. AI_Tutor_Logs (LogID, UserID, QuestionID, StepIndex, StudentInput, AIResponseJSON, Timestamp, DetectedMistake, Score, MaxScore, Note)
// =========================================================================

function doGet(e) {
  try {
    const params = e ? e.parameter : {};
    const action = params.action || "";

    switch (action) {
      case "login":
        return handleLogin(params.username, params.password);

      case "getQuestions":
        return handleGetQuestions(params.topic, params.grade, params.limit);

      case "getLessons":
        return handleGetLessons(params.topic);

      case "getProgress":
        return handleGetProgress(params.userId);

      case "getResults":
        return handleGetResults(params.userId, params.limit);

      case "getBadges":
        return handleGetBadges();

      case "getStudentAnalytics":
        return handleGetStudentAnalytics(params.userId);

      default:
        return createJsonResponse({ success: false, error: "Action GET không hợp lệ: " + action });
    }
  } catch (err) {
    return createJsonResponse({ success: false, error: err.toString() });
  }
}

function doPost(e) {
  try {
    const params = e ? e.parameter : {};
    const action = params.action || "";
    let postData = {};

    if (e && e.postData && e.postData.contents) {
      try {
        postData = JSON.parse(e.postData.contents);
      } catch (ex) {
        postData = e.parameter;
      }
    }

    switch (action) {
      case "login":
        return handleLogin(postData.username || params.username, postData.password || params.password);

      case "saveResult":
        return handleSaveResult(postData);

      case "saveProgress":
        return handleSaveProgress(postData);

      case "logAITutor":
        return handleLogAITutor(postData);

      default:
        return createJsonResponse({ success: false, error: "Action POST không hợp lệ: " + action });
    }
  } catch (err) {
    return createJsonResponse({ success: false, error: err.toString() });
  }
}

// -------------------------------------------------------------------------
// 1. ĐĂNG NHẬP / LẤY THÔNG TIN HỌC SINH (Users)
// Cột: UserID | Username | Password | Role | FullName | Grade | ClassID | CreatedAt
// -------------------------------------------------------------------------
function handleLogin(username, password) {
  if (!username) {
    return createJsonResponse({ success: false, message: "Tên đăng nhập không được để trống!" });
  }

  const ss = SpreadsheetApp.getActiveSpreadsheet();
  let sheet = ss.getSheetByName("Users");
  if (!sheet) {
    sheet = ss.insertSheet("Users");
    sheet.appendRow(["UserID", "Username", "Password", "Role", "FullName", "Grade", "ClassID", "CreatedAt"]);
    sheet.appendRow(["STU_001", "student_01", "123", "STUDENT", "Nguyen Van An", 6, "6A1", "2026-09-16"]);
  }

  const data = sheet.getDataRange().getDisplayValues();
  const uname = String(username).trim().toLowerCase();
  const pwd = String(password || "").trim();

  for (let i = 1; i < data.length; i++) {
    const rowUname = String(data[i][1] || "").trim().toLowerCase();
    const rowUid = String(data[i][0] || "").trim().toLowerCase();

    if (rowUname === uname || rowUid === uname) {
      const dbPassword = String(data[i][2] || "").trim();
      // Nếu có mật khẩu thì kiểm tra khớp (hoặc cho phép nếu chưa đặt)
      if (pwd && dbPassword && pwd !== dbPassword) {
        return createJsonResponse({ success: false, message: "Mật khẩu không chính xác!" });
      }

      return createJsonResponse({
        success: true,
        message: "Đăng nhập thành công!",
        data: {
          userId: data[i][0] || ("USR_" + i),
          username: data[i][1],
          role: data[i][3] || "STUDENT",
          fullName: data[i][4] || data[i][1],
          grade: parseInt(data[i][5]) || 6,
          classId: data[i][6] || "6A1",
          nickname: data[i][4] || data[i][1]
        }
      });
    }
  }

  // Nếu tài khoản mới chưa có trong Users -> Tự động đăng ký
  const newUserId = "STU_" + Date.now().toString().slice(-4);
  const nowStr = new Date().toISOString().split("T")[0];
  sheet.appendRow([newUserId, username, pwd || "123", "STUDENT", username, 6, "6A1", nowStr]);

  return createJsonResponse({
    success: true,
    message: "Tạo tài khoản học sinh mới thành công!",
    data: {
      userId: newUserId,
      username: username,
      role: "STUDENT",
      fullName: username,
      grade: 6,
      classId: "6A1",
      nickname: username
    }
  });
}

// -------------------------------------------------------------------------
// 2. LẤY DANH SÁCH CÂU HỎI (Questions_Math6)
// Cột: QuestionID | LessonID | Topic | Type | Difficulty | Prompt | OptA | OptB | OptC | OptD | CorrectAnswer | RubricJSON | Explanation | ImgUrl | Status
// -------------------------------------------------------------------------
function handleGetQuestions(topic, grade, limit) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = ss.getSheetByName("Questions_Math6");
  if (!sheet) {
    return createJsonResponse({ success: false, error: "Không tìm thấy sheet: Questions_Math6" });
  }

  const data = sheet.getDataRange().getDisplayValues();
  if (data.length <= 1) {
    return createJsonResponse({ success: true, questions: [] });
  }

  const headers = data[0].map(h => String(h).trim().toLowerCase());
  const idx = {
    qid: headers.indexOf("questionid"),
    lid: headers.indexOf("lessonid"),
    top: headers.indexOf("topic"),
    type: headers.indexOf("type"),
    diff: headers.indexOf("difficulty"),
    prompt: headers.indexOf("prompt"),
    optA: headers.indexOf("opta"),
    optB: headers.indexOf("optb"),
    optC: headers.indexOf("optc"),
    optD: headers.indexOf("optd"),
    ans: headers.indexOf("correctanswer"),
    rubric: headers.indexOf("rubricjson"),
    exp: headers.indexOf("explanation"),
    // Tự động nhận diện nhiều biến thể tên cột ảnh (ImgUrl, ImageUrl, Img, Image, HinhAnh, HinhVe...)
    img: (function() {
      const candidates = ["imgurl", "imageurl", "img", "image", "hinhanh", "hinhve", "photo"];
      for (let c of candidates) {
        const i = headers.indexOf(c);
        if (i >= 0) return i;
      }
      return -1;
    })(),
    status: headers.indexOf("status")
  };

  const max = limit ? parseInt(limit) : 1000;
  const questions = [];

  for (let i = 1; i < data.length && questions.length < max; i++) {
    const row = data[i];
    const qid = row[idx.qid >= 0 ? idx.qid : 0];
    if (!qid) continue;

    const qTopic = idx.top >= 0 ? row[idx.top] : "";
    if (topic && topic !== "ALL" && !qTopic.toLowerCase().includes(topic.toLowerCase())) {
      continue;
    }

    const imgVal = idx.img >= 0 ? String(row[idx.img] || "").trim() : "";

    questions.push({
      questionId: qid,
      lessonId: idx.lid >= 0 ? row[idx.lid] : "",
      topic: qTopic,
      type: idx.type >= 0 ? row[idx.type] : "MCQ",
      difficulty: idx.diff >= 0 ? row[idx.diff] : "EASY",
      prompt: idx.prompt >= 0 ? row[idx.prompt] : "",
      options: {
        A: idx.optA >= 0 ? row[idx.optA] : "",
        B: idx.optB >= 0 ? row[idx.optB] : "",
        C: idx.optC >= 0 ? row[idx.optC] : "",
        D: idx.optD >= 0 ? row[idx.optD] : ""
      },
      correctAnswer: idx.ans >= 0 ? row[idx.ans] : "A",
      rubricRaw: idx.rubric >= 0 ? row[idx.rubric] : "",
      explanation: idx.exp >= 0 ? row[idx.exp] : "",
      imageUrl: imgVal,
      imgUrl: imgVal,
      status: idx.status >= 0 ? row[idx.status] : "PUBLISHED"
    });
  }

  return createJsonResponse({ success: true, questions: questions });
}

// -------------------------------------------------------------------------
// 3. LẤY DANH SÁCH BÀI HỌC (Lessons)
// Cột: LessonID | Topic | Title | Grade | OrderIndex | Summary | Objectives | VideoUrl | ContentText
// -------------------------------------------------------------------------
function handleGetLessons(topic) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = ss.getSheetByName("Lessons");
  if (!sheet) {
    return createJsonResponse({ success: false, error: "Không tìm thấy sheet: Lessons" });
  }

  const data = sheet.getDataRange().getDisplayValues();
  if (data.length <= 1) {
    return createJsonResponse({ success: true, lessons: [] });
  }

  const headers = data[0].map(h => String(h).trim().toLowerCase());
  const idx = {
    lid: headers.indexOf("lessonid"),
    top: headers.indexOf("topic"),
    title: headers.indexOf("title"),
    grade: headers.indexOf("grade"),
    order: headers.indexOf("orderindex"),
    summary: headers.indexOf("summary"),
    obj: headers.indexOf("objectives"),
    video: headers.indexOf("videourl"),
    content: headers.indexOf("contenttext")
  };

  const lessons = [];
  for (let i = 1; i < data.length; i++) {
    const row = data[i];
    const lid = row[idx.lid >= 0 ? idx.lid : 0];
    if (!lid) continue;

    const rowTopic = idx.top >= 0 ? row[idx.top] : "";
    if (topic && topic !== "ALL" && !rowTopic.toLowerCase().includes(topic.toLowerCase())) {
      continue;
    }

    lessons.push({
      lessonId: lid,
      topic: rowTopic,
      title: idx.title >= 0 ? row[idx.title] : "",
      grade: idx.grade >= 0 ? (parseInt(row[idx.grade]) || 6) : 6,
      orderIndex: idx.order >= 0 ? (parseInt(row[idx.order]) || i) : i,
      summaryText: idx.summary >= 0 ? row[idx.summary] : "",
      objectives: idx.obj >= 0 ? row[idx.obj] : "",
      videoUrl: idx.video >= 0 ? row[idx.video] : "",
      description: idx.content >= 0 ? row[idx.content] : (idx.summary >= 0 ? row[idx.summary] : ""),
      topStudentNickname: "Trạng Tí",
      topStudentAttempts: 1,
      userHighestScore: 0
    });
  }

  return createJsonResponse({ success: true, lessons: lessons });
}

// -------------------------------------------------------------------------
// 4. LƯU KẾT QUẢ VÀ TỰ ĐỘNG ĐỒNG BỘ TIẾN ĐỘ (Results & Progress)
// Results:  ResultID | UserID | LessonID | ActivityType | Score | MaxScore | TimeSpentSec | MistakesCount | SubmittedAt
// Progress: ProgressID | UserID | LessonID | Attempts | HighestScore | LastScore | Completed | LastAttemptDate
// -------------------------------------------------------------------------
function handleSaveResult(payload) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  
  // 4.1 Ghi bảng Results
  let resSheet = ss.getSheetByName("Results");
  if (!resSheet) {
    resSheet = ss.insertSheet("Results");
    resSheet.appendRow(["ResultID", "UserID", "LessonID", "ActivityType", "Score", "MaxScore", "TimeSpentSec", "MistakesCount", "SubmittedAt"]);
  }

  const resId = "RES_" + Date.now();
  const userId = payload.userId || "STU_001";
  const lessonId = payload.lessonId || "GENERAL";
  const score = parseInt(payload.score) || 0;
  const maxScore = parseInt(payload.maxScore) || 10;
  const timeSpent = parseInt(payload.timeSpentSeconds || payload.timeSpentSec || payload.timeSpent) || 60;
  const mistakes = parseInt(payload.mistakesCount) || 0;
  const nowFormatted = Utilities.formatDate(new Date(), "Asia/Ho_Chi_Minh", "yyyy-MM-dd HH:mm:ss");

  resSheet.appendRow([
    resId,
    userId,
    lessonId,
    payload.activityType || "MCQ",
    score,
    maxScore,
    timeSpent,
    mistakes,
    nowFormatted
  ]);

  // 4.2 Cập nhật bảng Progress
  let progSheet = ss.getSheetByName("Progress");
  if (!progSheet) {
    progSheet = ss.insertSheet("Progress");
    progSheet.appendRow(["ProgressID", "UserID", "LessonID", "Attempts", "HighestScore", "LastScore", "Completed", "LastAttemptDate"]);
  }

  const progData = progSheet.getDataRange().getDisplayValues();
  let foundRow = -1;

  for (let i = 1; i < progData.length; i++) {
    if (progData[i][1] === userId && progData[i][2] === lessonId) {
      foundRow = i + 1;
      break;
    }
  }

  const isComplete = (score >= 5 || (maxScore > 0 && score / maxScore >= 0.5));

  if (foundRow > 0) {
    const curAttempts = (parseInt(progData[foundRow - 1][3]) || 0) + 1;
    const curHighest = Math.max(parseInt(progData[foundRow - 1][4]) || 0, score);
    const wasCompleted = String(progData[foundRow - 1][6]).toUpperCase() === "TRUE" || isComplete;

    progSheet.getRange(foundRow, 4).setValue(curAttempts);
    progSheet.getRange(foundRow, 5).setValue(curHighest);
    progSheet.getRange(foundRow, 6).setValue(score);
    progSheet.getRange(foundRow, 7).setValue(wasCompleted ? "TRUE" : "FALSE");
    progSheet.getRange(foundRow, 8).setValue(nowFormatted);
  } else {
    const progId = "PROG_" + Date.now();
    progSheet.appendRow([
      progId,
      userId,
      lessonId,
      1,
      score,
      score,
      isComplete ? "TRUE" : "FALSE",
      nowFormatted
    ]);
  }

  return createJsonResponse({ success: true, message: "Đã lưu kết quả bài làm thành công!", unlockedBadges: [] });
}

// -------------------------------------------------------------------------
// 5. LẤY TIẾN ĐỘ HỌC TẬP (Progress)
// -------------------------------------------------------------------------
function handleGetProgress(userId) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = ss.getSheetByName("Progress");
  if (!sheet) {
    return createJsonResponse({ success: true, progress: [] });
  }

  const data = sheet.getDataRange().getDisplayValues();
  const progress = [];

  for (let i = 1; i < data.length; i++) {
    const row = data[i];
    if (userId && row[1] && row[1] !== userId) continue;

    progress.push({
      progressId: row[0],
      userId: row[1],
      lessonId: row[2],
      attempts: parseInt(row[3]) || 0,
      highestScore: parseInt(row[4]) || 0,
      lastScore: parseInt(row[5]) || 0,
      isCompleted: String(row[6]).toUpperCase() === "TRUE",
      lastAttemptDate: row[7] || ""
    });
  }

  return createJsonResponse({ success: true, progress: progress });
}

// -------------------------------------------------------------------------
// 6. LẤY LỊCH SỬ KẾT QUẢ (Results)
// -------------------------------------------------------------------------
function handleGetResults(userId, limit) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = ss.getSheetByName("Results");
  if (!sheet) {
    return createJsonResponse({ success: true, results: [] });
  }

  const data = sheet.getDataRange().getDisplayValues();
  const results = [];
  const max = limit ? parseInt(limit) : 50;

  for (let i = data.length - 1; i >= 1 && results.length < max; i--) {
    const row = data[i];
    if (userId && row[1] && row[1] !== userId) continue;

    results.push({
      resultId: row[0],
      userId: row[1],
      lessonId: row[2],
      activityType: row[3],
      score: parseInt(row[4]) || 0,
      maxScore: parseInt(row[5]) || 10,
      timeSpentSeconds: parseInt(row[6]) || 0,
      mistakesCount: parseInt(row[7]) || 0,
      submittedAt: row[8]
    });
  }

  return createJsonResponse({ success: true, results: results });
}

// -------------------------------------------------------------------------
// 7. LẤY DANH HIỆU (Badges)
// -------------------------------------------------------------------------
function handleGetBadges() {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = ss.getSheetByName("Badges");
  if (!sheet) {
    return createJsonResponse({ success: true, badges: [] });
  }

  const data = sheet.getDataRange().getDisplayValues();
  const badges = [];

  for (let i = 1; i < data.length; i++) {
    const row = data[i];
    if (!row[0]) continue;

    badges.push({
      badgeId: row[0],
      badgeCode: row[1],
      name: row[2],
      description: row[3],
      iconUrl: row[4]
    });
  }

  return createJsonResponse({ success: true, badges: badges });
}

// -------------------------------------------------------------------------
// 8. GHI LOG AI TUTOR (AI_Tutor_Logs - Đúng chuẩn 11 cột)
// Cột: LogID | UserID | QuestionID | StepIndex | StudentInput | AIResponseJSON | Timestamp | DetectedMistake | Score | MaxScore | Note
// -------------------------------------------------------------------------
function handleLogAITutor(payload) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  let sheet = ss.getSheetByName("AI_Tutor_Logs");
  if (!sheet) {
    sheet = ss.insertSheet("AI_Tutor_Logs");
    sheet.appendRow(["LogID", "UserID", "QuestionID", "StepIndex", "StudentInput", "AIResponseJSON", "Timestamp", "DetectedMistake", "Score", "MaxScore", "Note"]);
  }

  const logId = payload.logId || ("LOG_" + Date.now());
  const nowFormatted = Utilities.formatDate(new Date(), "Asia/Ho_Chi_Minh", "yyyy-MM-dd HH:mm:ss");

  sheet.appendRow([
    logId,
    payload.userId || "STU_001",
    payload.questionId || "",
    payload.stepIndex || 1,
    payload.studentAnswer || payload.studentPrompt || payload.studentInput || "(Học sinh nộp bài giải)",
    payload.aiFeedback || (typeof payload.aiResponse === "object" ? JSON.stringify(payload.aiResponse) : payload.aiResponse) || "",
    payload.timestamp || nowFormatted,
    payload.detectedMistake || "NONE",
    payload.score || 0,
    payload.maxScore || 10,
    payload.sessionType || payload.note || ""
  ]);

  return createJsonResponse({ success: true, message: "Đã lưu AI Tutor log thành công!" });
}

// -------------------------------------------------------------------------
// 9. PHÂN TÍCH HỌC TẬP (Student Analytics)
// -------------------------------------------------------------------------
function handleGetStudentAnalytics(userId) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const progSheet = ss.getSheetByName("Progress");
  let completed = 0;
  let totalScore = 0;
  let scoreCount = 0;

  if (progSheet) {
    const pData = progSheet.getDataRange().getDisplayValues();
    for (let i = 1; i < pData.length; i++) {
      if (!userId || pData[i][1] === userId) {
        if (String(pData[i][6]).toUpperCase() === "TRUE") completed++;
        const s = parseInt(pData[i][4]);
        if (!isNaN(s)) {
          totalScore += s;
          scoreCount++;
        }
      }
    }
  }

  const avg = scoreCount > 0 ? (totalScore / scoreCount).toFixed(1) : 0;

  return createJsonResponse({
    success: true,
    analytics: {
      userId: userId || "STU_001",
      completedLessons: completed,
      averageScore: parseFloat(avg),
      badges: [],
      progressList: []
    }
  });
}

function createJsonResponse(data) {
  return ContentService.createTextOutput(JSON.stringify(data))
    .setMimeType(ContentService.MimeType.JSON);
}
