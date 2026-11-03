// =========================================================================
// GOOGLE APPS SCRIPT BACKEND CHUáº¨N HOÃ CHO MATH6-AI-COMPANION
// Khá»›p 100% vá»›i cáº¥u trÃºc 8 báº£ng:
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

      // --- HỆ THỐNG ĐẤU KHÁNG 1V1 REALTIME ---
      case "joinVersusQueue":
        return handleJoinVersusQueue(params.userId, params.nickname, params.mode);

      case "pollVersusQueue":
        return handlePollVersusQueue(params.userId);

      case "cancelVersusQueue":
        return handleCancelVersusQueue(params.userId);

      case "submitVersusScore":
        return handleSubmitVersusScore(params.roomId, params.userId, params.score);

      case "pollVersusRoomResult":
        return handlePollVersusRoomResult(params.roomId, params.userId);

      default:
        return createJsonResponse({ success: false, error: "Action GET khÃ´ng há»£p lá»‡: " + action });
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

      // --- HỆ THỐNG ĐẤU KHÁNG 1V1 REALTIME ---
      case "joinVersusQueue":
        return handleJoinVersusQueue(postData.userId || params.userId, postData.nickname || params.nickname, postData.mode || params.mode);

      case "pollVersusQueue":
        return handlePollVersusQueue(postData.userId || params.userId);

      case "cancelVersusQueue":
        return handleCancelVersusQueue(postData.userId || params.userId);

      case "submitVersusScore":
        return handleSubmitVersusScore(postData.roomId || params.roomId, postData.userId || params.userId, postData.score || params.score);

      case "pollVersusRoomResult":
        return handlePollVersusRoomResult(postData.roomId || params.roomId, postData.userId || params.userId);

      default:
        return createJsonResponse({ success: false, error: "Action POST khÃ´ng há»£p lá»‡: " + action });
    }
  } catch (err) {
    return createJsonResponse({ success: false, error: err.toString() });
  }
}

// -------------------------------------------------------------------------
// 1. ÄÄ‚NG NHáº¬P / Láº¤Y THÃ”NG TIN Há»ŒC SINH (Users)
// Cá»™t: UserID | Username | Password | Role | FullName | Grade | ClassID | CreatedAt
// -------------------------------------------------------------------------
function handleLogin(username, password) {
  if (!username) {
    return createJsonResponse({ success: false, message: "TÃªn Ä‘Äƒng nháº­p khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!" });
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
      // Náº¿u cÃ³ máº­t kháº©u thÃ¬ kiá»ƒm tra khá»›p (hoáº·c cho phÃ©p náº¿u chÆ°a Ä‘áº·t)
      if (pwd && dbPassword && pwd !== dbPassword) {
        return createJsonResponse({ success: false, message: "Máº­t kháº©u khÃ´ng chÃ­nh xÃ¡c!" });
      }

      return createJsonResponse({
        success: true,
        message: "ÄÄƒng nháº­p thÃ nh cÃ´ng!",
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

  // Náº¿u tÃ i khoáº£n má»›i chÆ°a cÃ³ trong Users -> Tá»± Ä‘á»™ng Ä‘Äƒng kÃ½
  const newUserId = "STU_" + Date.now().toString().slice(-4);
  const nowStr = new Date().toISOString().split("T")[0];
  sheet.appendRow([newUserId, username, pwd || "123", "STUDENT", username, 6, "6A1", nowStr]);

  return createJsonResponse({
    success: true,
    message: "Táº¡o tÃ i khoáº£n há»c sinh má»›i thÃ nh cÃ´ng!",
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
// 2. Láº¤Y DANH SÃCH CÃ‚U Há»ŽI (Questions_Math6)
// Cá»™t: QuestionID | LessonID | Topic | Type | Difficulty | Prompt | OptA | OptB | OptC | OptD | CorrectAnswer | RubricJSON | Explanation | ImgUrl | Status
// -------------------------------------------------------------------------
function handleGetQuestions(topic, grade, limit) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = ss.getSheetByName("Questions_Math6");
  if (!sheet) {
    return createJsonResponse({ success: false, error: "KhÃ´ng tÃ¬m tháº¥y sheet: Questions_Math6" });
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
    // Tá»± Ä‘á»™ng nháº­n diá»‡n nhiá»u biáº¿n thá»ƒ tÃªn cá»™t áº£nh (ImgUrl, ImageUrl, Img, Image, HinhAnh, HinhVe...)
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
// 3. Láº¤Y DANH SÃCH BÃ€I Há»ŒC (Lessons)
// Cá»™t: LessonID | Topic | Title | Grade | OrderIndex | Summary | Objectives | VideoUrl | ContentText
// -------------------------------------------------------------------------
function handleGetLessons(topic) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = ss.getSheetByName("Lessons");
  if (!sheet) {
    return createJsonResponse({ success: false, error: "KhÃ´ng tÃ¬m tháº¥y sheet: Lessons" });
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
// 4. LÆ¯U Káº¾T QUáº¢ VÃ€ Tá»° Äá»˜NG Äá»’NG Bá»˜ TIáº¾N Äá»˜ (Results & Progress)
// Results:  ResultID | UserID | LessonID | ActivityType | Score | MaxScore | TimeSpentSec | MistakesCount | SubmittedAt
// Progress: ProgressID | UserID | LessonID | Attempts | HighestScore | LastScore | Completed | LastAttemptDate
// -------------------------------------------------------------------------
function handleSaveResult(payload) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  
  // 4.1 Ghi báº£ng Results
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

  // 4.2 Cáº­p nháº­t báº£ng Progress
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

  return createJsonResponse({ success: true, message: "ÄÃ£ lÆ°u káº¿t quáº£ bÃ i lÃ m thÃ nh cÃ´ng!", unlockedBadges: [] });
}

// -------------------------------------------------------------------------
// 5. Láº¤Y TIáº¾N Äá»˜ Há»ŒC Táº¬P (Progress)
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
// 6. Láº¤Y Lá»ŠCH Sá»¬ Káº¾T QUáº¢ (Results)
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
// 7. Láº¤Y DANH HIá»†U (Badges)
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
// 8. GHI LOG AI TUTOR (AI_Tutor_Logs - ÄÃºng chuáº©n 11 cá»™t)
// Cá»™t: LogID | UserID | QuestionID | StepIndex | StudentInput | AIResponseJSON | Timestamp | DetectedMistake | Score | MaxScore | Note
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
    payload.studentAnswer || payload.studentPrompt || payload.studentInput || "(Há»c sinh ná»™p bÃ i giáº£i)",
    payload.aiFeedback || (typeof payload.aiResponse === "object" ? JSON.stringify(payload.aiResponse) : payload.aiResponse) || "",
    payload.timestamp || nowFormatted,
    payload.detectedMistake || "NONE",
    payload.score || 0,
    payload.maxScore || 10,
    payload.sessionType || payload.note || ""
  ]);

  return createJsonResponse({ success: true, message: "ÄÃ£ lÆ°u AI Tutor log thÃ nh cÃ´ng!" });
}

// -------------------------------------------------------------------------
// 9. PHÃ‚N TÃCH Há»ŒC Táº¬P (Student Analytics)
// -------------------------------------------------------------------------
function handleGetStudentAnalytics(userId) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const progSheet = ss.getSheetByName("Progress");
  const userSheet = ss.getSheetByName("Users");
  let fullName = "Học sinh";
  if (userSheet && userId) {
    const uData = userSheet.getDataRange().getDisplayValues();
    for (let u = 1; u < uData.length; u++) {
      if (uData[u][0] === userId || uData[u][1] === userId) {
        fullName = uData[u][4] || uData[u][1];
        break;
      }
    }
  }

  let completed = 0;
  let totalScore = 0;
  let scoreCount = 0;
  const progressList = [];

  if (progSheet && userId) {
    const pData = progSheet.getDataRange().getDisplayValues();
    for (let i = 1; i < pData.length; i++) {
      if (pData[i][1] === userId) {
        const attempts = parseInt(pData[i][3]) || 0;
        const highest = parseInt(pData[i][4]) || 0;
        const last = parseInt(pData[i][5]) || 0;
        const isComp = String(pData[i][6]).toUpperCase() === "TRUE";

        if (isComp) completed++;
        if (!isNaN(highest) && highest > 0) {
          totalScore += highest;
          scoreCount++;
        }

        progressList.push({
          lessonId: pData[i][2],
          attempts: attempts,
          highestScore: highest,
          lastScore: last,
          isCompleted: isComp
        });
      }
    }
  }

  const avg = scoreCount > 0 ? (totalScore / scoreCount).toFixed(1) : 0;

  return createJsonResponse({
    success: true,
    analytics: {
      userId: userId || "",
      fullName: fullName,
      completedLessons: completed,
      averageScore: parseFloat(avg),
      badges: [],
      progressList: progressList
    }
  });
}

function createJsonResponse(data) {
  return ContentService.createTextOutput(JSON.stringify(data))
    .setMimeType(ContentService.MimeType.JSON);
}

// =========================================================================
// 10. HỆ THỐNG ĐẤU KHÁNG 1V1 REALTIME (Match_Queue & Match_Rooms)
// =========================================================================

function handleJoinVersusQueue(userId, nickname, mode) {
  if (!userId) return createJsonResponse({ success: false, message: "Thiếu userId" });
  nickname = nickname || "Học sinh";
  mode = (mode || "MCQ").toUpperCase();

  const lock = LockService.getScriptLock();
  try {
    lock.waitLock(10000);
  } catch (e) {
    return createJsonResponse({ success: false, message: "Hệ thống đang bận, vui lòng thử lại!" });
  }

  try {
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    let qSheet = ss.getSheetByName("Match_Queue");
    if (!qSheet) {
      qSheet = ss.insertSheet("Match_Queue");
      qSheet.appendRow(["UserID", "Nickname", "Mode", "Status", "MatchedRoomID", "LastPingTime"]);
    }

    let rSheet = ss.getSheetByName("Match_Rooms");
    if (!rSheet) {
      rSheet = ss.insertSheet("Match_Rooms");
      rSheet.appendRow(["RoomID", "Player1_ID", "Player1_Name", "Player2_ID", "Player2_Name", "Mode", "QuestionIDs", "Player1_Score", "Player2_Score", "Player1_Done", "Player2_Done", "WinnerID", "Status", "CreatedAt"]);
    }

    const now = Date.now();
    const qData = qSheet.getDataRange().getValues();

    // 1. Dọn dẹp trạng thái cũ: Khi người chơi ấn Bắt đầu ghép cặp mới,
    // xóa bỏ phòng cũ đã kết thúc hoặc quá hạn để tránh bị kẹt với đối thủ cũ.
    for (let i = 1; i < qData.length; i++) {
      if (String(qData[i][0]) === String(userId)) {
        if (qData[i][3] === "MATCHED" && qData[i][4]) {
          const oldRoomId = String(qData[i][4]);
          const oldRoom = getRoomDetails(rSheet, oldRoomId, userId);
          // Chỉ tái sử dụng phòng nếu phòng VỪA MỚI TẠO trong vòng 20s và chưa ai nộp bài
          if (oldRoom && oldRoom.status === "PLAYING" && (now - oldRoom.createdAtMs < 20000) && oldRoom.player1Score === -1 && oldRoom.player2Score === -1) {
            lock.releaseLock();
            return createJsonResponse({
              success: true,
              status: "MATCHED",
              roomId: oldRoomId,
              opponentId: oldRoom.opponentId,
              opponentNickname: oldRoom.opponentNickname,
              mode: oldRoom.mode,
              questionIds: oldRoom.questionIds
            });
          } else {
            // Phòng cũ đã xong hoặc hết hạn -> reset hàng đợi của người chơi này
            qSheet.getRange(i + 1, 4).setValue("WAITING");
            qSheet.getRange(i + 1, 5).setValue("");
            qSheet.getRange(i + 1, 6).setValue(now);
          }
        }
      }
    }

    // 2. Tìm người chơi khác đang WAITING cùng thể thức (ping trong vòng 35s)
    let matchedRowIndex = -1;
    let opponent = null;

    const currentQData = qSheet.getDataRange().getValues();
    for (let i = 1; i < currentQData.length; i++) {
      const uId = String(currentQData[i][0]);
      const uMode = String(currentQData[i][2]);
      const uStatus = String(currentQData[i][3]);
      const lastPing = Number(currentQData[i][5]) || 0;

      if (uId !== String(userId) && uMode === mode && uStatus === "WAITING") {
        if (now - lastPing < 35000) {
          matchedRowIndex = i + 1;
          opponent = {
            userId: uId,
            nickname: String(currentQData[i][1])
          };
          break;
        } else {
          qSheet.getRange(i + 1, 4).setValue("EXPIRED");
        }
      }
    }

    if (opponent && matchedRowIndex > 0) {
      // Ghép cặp thành công -> Tạo phòng đấu mới!
      const roomId = "ROOM_" + now;
      const questionIds = pickRandomQuestionIds(ss, mode, 10);
      const qIdsStr = questionIds.join(",");

      rSheet.appendRow([
        roomId,
        opponent.userId, opponent.nickname,
        userId, nickname,
        mode,
        qIdsStr,
        -1, -1,
        false, false,
        "",
        "PLAYING",
        new Date().toISOString()
      ]);

      // Cập nhật người đợi thành MATCHED
      qSheet.getRange(matchedRowIndex, 4).setValue("MATCHED");
      qSheet.getRange(matchedRowIndex, 5).setValue(roomId);
      qSheet.getRange(matchedRowIndex, 6).setValue(now);

      // Cập nhật người thách đấu thành MATCHED
      upsertQueueUser(qSheet, userId, nickname, mode, "MATCHED", roomId, now);

      lock.releaseLock();

      return createJsonResponse({
        success: true,
        status: "MATCHED",
        roomId: roomId,
        opponentId: opponent.userId,
        opponentNickname: opponent.nickname,
        mode: mode,
        questionIds: questionIds
      });
    }

    // 3. Chưa có ai đợi -> Đưa người chơi vào hàng đợi WAITING
    upsertQueueUser(qSheet, userId, nickname, mode, "WAITING", "", now);

    lock.releaseLock();

    return createJsonResponse({
      success: true,
      status: "WAITING",
      message: "Đang xếp hàng tìm bạn học..."
    });

  } catch (err) {
    if (lock) lock.releaseLock();
    return createJsonResponse({ success: false, error: err.toString() });
  }
}

function handlePollVersusQueue(userId) {
  if (!userId) return createJsonResponse({ success: false, message: "Thiếu userId" });

  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const qSheet = ss.getSheetByName("Match_Queue");
  if (!qSheet) return createJsonResponse({ success: false, status: "NOT_IN_QUEUE" });

  const qData = qSheet.getDataRange().getValues();
  const now = Date.now();

  for (let i = 1; i < qData.length; i++) {
    if (String(qData[i][0]) === String(userId)) {
      qSheet.getRange(i + 1, 6).setValue(now);

      const status = String(qData[i][3]);
      if (status === "MATCHED") {
        const roomId = String(qData[i][4]);
        const rSheet = ss.getSheetByName("Match_Rooms");
        const roomInfo = getRoomDetails(rSheet, roomId, userId);
        if (roomInfo) {
          return createJsonResponse({
            success: true,
            status: "MATCHED",
            roomId: roomId,
            opponentId: roomInfo.opponentId,
            opponentNickname: roomInfo.opponentNickname,
            mode: roomInfo.mode,
            questionIds: roomInfo.questionIds
          });
        }
      }
      return createJsonResponse({ success: true, status: status });
    }
  }

  return createJsonResponse({ success: false, status: "NOT_IN_QUEUE" });
}

function handleCancelVersusQueue(userId) {
  if (!userId) return createJsonResponse({ success: false });
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const qSheet = ss.getSheetByName("Match_Queue");
  if (!qSheet) return createJsonResponse({ success: true });

  const qData = qSheet.getDataRange().getValues();
  for (let i = 1; i < qData.length; i++) {
    if (String(qData[i][0]) === String(userId)) {
      const oldRoomId = String(qData[i][4] || "");
      qSheet.getRange(i + 1, 4).setValue("CANCELLED");
      qSheet.getRange(i + 1, 5).setValue("");
      qSheet.getRange(i + 1, 6).setValue(Date.now());

      if (oldRoomId) {
        const rSheet = ss.getSheetByName("Match_Rooms");
        if (rSheet) {
          const rData = rSheet.getDataRange().getValues();
          for (let r = 1; r < rData.length; r++) {
            if (String(rData[r][0]) === oldRoomId && rData[r][12] === "PLAYING") {
              const p1Done = String(rData[r][9]).toLowerCase() === "true" || rData[r][9] === true;
              const p2Done = String(rData[r][10]).toLowerCase() === "true" || rData[r][10] === true;
              if (!p1Done && !p2Done) {
                rSheet.getRange(r + 1, 13).setValue("CANCELLED");
              }
              break;
            }
          }
        }
      }
      break;
    }
  }
  return createJsonResponse({ success: true, message: "Đã hủy tìm trận" });
}

function handleSubmitVersusScore(roomId, userId, score) {
  if (!roomId || !userId) return createJsonResponse({ success: false, message: "Thiếu tham số" });
  score = parseInt(score) || 0;
  if (score <= 10 && score > 0) score = score * 10;

  const lock = LockService.getScriptLock();
  try {
    lock.waitLock(10000);
  } catch (e) {
    return createJsonResponse({ success: false, message: "Hệ thống đang bận" });
  }

  try {
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    const rSheet = ss.getSheetByName("Match_Rooms");
    if (!rSheet) {
      lock.releaseLock();
      return createJsonResponse({ success: false, message: "Không tìm thấy Match_Rooms" });
    }

    const rData = rSheet.getDataRange().getValues();
    let rowIdx = -1;
    for (let i = 1; i < rData.length; i++) {
      if (String(rData[i][0]) === String(roomId)) {
        rowIdx = i + 1;
        break;
      }
    }

    if (rowIdx === -1) {
      lock.releaseLock();
      return createJsonResponse({ success: false, message: "Phòng không tồn tại" });
    }

    const row = rData[rowIdx - 1];
    const p1Id = String(row[1]);
    const p1Name = String(row[2]);
    let p1Score = parseInt(row[7]);
    let p1Done = String(row[9]).toLowerCase() === "true" || row[9] === true;

    const p2Id = String(row[3]);
    const p2Name = String(row[4]);
    let p2Score = parseInt(row[8]);
    let p2Done = String(row[10]).toLowerCase() === "true" || row[10] === true;

    if (userId === p1Id) {
      p1Score = score;
      p1Done = true;
      rSheet.getRange(rowIdx, 8).setValue(score);
      rSheet.getRange(rowIdx, 10).setValue(true);
    } else if (userId === p2Id) {
      p2Score = score;
      p2Done = true;
      rSheet.getRange(rowIdx, 9).setValue(score);
      rSheet.getRange(rowIdx, 11).setValue(true);
    }

    const isFinished = p1Done && p2Done;
    let winnerId = "";
    if (isFinished) {
      rSheet.getRange(rowIdx, 13).setValue("FINISHED");
      if (p1Score > p2Score) winnerId = p1Id;
      else if (p2Score > p1Score) winnerId = p2Id;
      else winnerId = "DRAW";
      rSheet.getRange(rowIdx, 12).setValue(winnerId);
    }

    lock.releaseLock();

    return createJsonResponse({
      success: true,
      roomId: roomId,
      player1Id: p1Id,
      player1Name: p1Name,
      player1Score: p1Score,
      player1Done: p1Done,
      player2Id: p2Id,
      player2Name: p2Name,
      player2Score: p2Score,
      player2Done: p2Done,
      isFinished: isFinished,
      winnerId: winnerId
    });

  } catch (err) {
    if (lock) lock.releaseLock();
    return createJsonResponse({ success: false, error: err.toString() });
  }
}

function handlePollVersusRoomResult(roomId, userId) {
  if (!roomId) return createJsonResponse({ success: false, message: "Thiếu roomId" });

  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const rSheet = ss.getSheetByName("Match_Rooms");
  if (!rSheet) return createJsonResponse({ success: false, message: "Không tìm thấy Match_Rooms" });

  const rData = rSheet.getDataRange().getValues();
  for (let i = 1; i < rData.length; i++) {
    if (String(rData[i][0]) === String(roomId)) {
      const row = rData[i];
      const p1Id = String(row[1]);
      const p1Name = String(row[2]);
      let p1Score = parseInt(row[7]);
      const p1Done = String(row[9]).toLowerCase() === "true" || row[9] === true;

      const p2Id = String(row[3]);
      const p2Name = String(row[4]);
      let p2Score = parseInt(row[8]);
      const p2Done = String(row[10]).toLowerCase() === "true" || row[10] === true;

      if (p1Score <= 10 && p1Score > 0) p1Score = p1Score * 10;
      if (p2Score <= 10 && p2Score > 0) p2Score = p2Score * 10;

      const isFinished = p1Done && p2Done;
      let winnerId = String(row[11] || "");
      if (isFinished && !winnerId) {
        if (p1Score > p2Score) winnerId = p1Id;
        else if (p2Score > p1Score) winnerId = p2Id;
        else winnerId = "DRAW";
      }

      return createJsonResponse({
        success: true,
        roomId: roomId,
        player1Id: p1Id,
        player1Name: p1Name,
        player1Score: p1Score,
        player1Done: p1Done,
        player2Id: p2Id,
        player2Name: p2Name,
        player2Score: p2Score,
        player2Done: p2Done,
        isFinished: isFinished,
        winnerId: winnerId
      });
    }
  }

  return createJsonResponse({ success: false, message: "Phòng không tồn tại: " + roomId });
}

function upsertQueueUser(qSheet, userId, nickname, mode, status, roomId, pingTime) {
  const data = qSheet.getDataRange().getValues();
  for (let i = 1; i < data.length; i++) {
    if (String(data[i][0]) === String(userId)) {
      qSheet.getRange(i + 1, 2).setValue(nickname);
      qSheet.getRange(i + 1, 3).setValue(mode);
      qSheet.getRange(i + 1, 4).setValue(status);
      qSheet.getRange(i + 1, 5).setValue(roomId);
      qSheet.getRange(i + 1, 6).setValue(pingTime);
      return;
    }
  }
  qSheet.appendRow([userId, nickname, mode, status, roomId, pingTime]);
}

function getRoomDetails(rSheet, roomId, currentUserId) {
  if (!rSheet) return null;
  const data = rSheet.getDataRange().getValues();
  for (let i = 1; i < data.length; i++) {
    if (String(data[i][0]) === String(roomId)) {
      const p1Id = String(data[i][1]);
      const p1Name = String(data[i][2]);
      const p2Id = String(data[i][3]);
      const p2Name = String(data[i][4]);
      const mode = String(data[i][5]);
      const qIdsStr = String(data[i][6] || "");
      const questionIds = qIdsStr ? qIdsStr.split(",") : [];
      const p1Score = parseInt(data[i][7]);
      const p2Score = parseInt(data[i][8]);
      const p1Done = String(data[i][9]).toLowerCase() === "true" || data[i][9] === true;
      const p2Done = String(data[i][10]).toLowerCase() === "true" || data[i][10] === true;
      const winnerId = String(data[i][11] || "");
      const status = String(data[i][12] || "PLAYING");
      const createdAtRaw = data[i][13];
      let createdAtMs = 0;
      try {
        createdAtMs = new Date(createdAtRaw).getTime() || 0;
      } catch (e) {
        createdAtMs = 0;
      }

      const isP1 = String(currentUserId) === p1Id;
      return {
        roomId: roomId,
        opponentId: isP1 ? p2Id : p1Id,
        opponentNickname: isP1 ? p2Name : p1Name,
        mode: mode,
        questionIds: questionIds,
        player1Id: p1Id,
        player1Name: p1Name,
        player1Score: p1Score,
        player1Done: p1Done,
        player2Id: p2Id,
        player2Name: p2Name,
        player2Score: p2Score,
        player2Done: p2Done,
        winnerId: winnerId,
        status: status,
        createdAtMs: createdAtMs
      };
    }
  }
  return null;
}

function pickRandomQuestionIds(ss, mode, count) {
  const qSheet = ss.getSheetByName("Questions_Math6");
  if (!qSheet) return [];
  const data = qSheet.getDataRange().getValues();
  if (data.length <= 1) return [];

  const headers = data[0].map(h => String(h).trim().toLowerCase());
  const qidIdx = headers.indexOf("questionid");
  const typeIdx = headers.indexOf("type");

  const pool = [];
  for (let i = 1; i < data.length; i++) {
    const qid = String(data[i][qidIdx >= 0 ? qidIdx : 0]);
    const qType = typeIdx >= 0 ? String(data[i][typeIdx]).toUpperCase() : "MCQ";
    if (mode === "ALL" || qType === mode || (mode === "MCQ" && (!qType || qType === "MCQ"))) {
      if (qid) pool.push(qid);
    }
  }

  // Shuffle pool
  for (let i = pool.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    const temp = pool[i];
    pool[i] = pool[j];
    pool[j] = temp;
  }

  return pool.slice(0, count);
}
