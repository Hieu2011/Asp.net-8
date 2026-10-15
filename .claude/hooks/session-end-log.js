// SessionEnd: ghi 1 dòng tóm tắt phiên vào cuối TaskList/plan.md (mục "Nhật ký session").
// Chỉ thêm nhật ký — KHÔNG đổi trạng thái task (✅ chỉ đánh khi anh confirm). Lỗi gì cũng bỏ qua, không chặn việc thoát.
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const LOG_HEADING = '## Nhật ký session (tự động)';
const MAX_LOG_ENTRIES = 20;
const MAX_PROMPTS = 6;
const MAX_PROMPT_LENGTH = 70;

const readStdin = () => {
  try {
    return JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
  } catch {
    return {};
  }
};

// Lấy các yêu cầu anh gõ trong phiên: bỏ tool result, tin nhắn hệ thống chèn vào, bản tóm tắt khi compact
const collectPrompts = (strTranscriptPath) => {
  if (!strTranscriptPath || !fs.existsSync(strTranscriptPath)) return [];
  const lstPrompt = [];
  for (const strLine of fs.readFileSync(strTranscriptPath, 'utf8').split('\n')) {
    let objEntry;
    try {
      objEntry = JSON.parse(strLine);
    } catch {
      continue;
    }
    if (objEntry.type !== 'user' || objEntry.isMeta || objEntry.isCompactSummary) continue;
    const objContent = objEntry.message?.content;
    const strText = typeof objContent === 'string'
      ? objContent
      : Array.isArray(objContent) ? objContent.filter((x) => x.type === 'text').map((x) => x.text).join('\n') : '';
    const strClean = strText.replace(/<[^>]+>[\s\S]*?<\/[^>]+>/g, '').replace(/<[^>]+>/g, '').trim();
    const strFirstLine = strClean.split('\n').map((x) => x.replace(/^[-*\s]+/, '').trim()).find(Boolean);
    if (!strFirstLine || strFirstLine.startsWith('This session is being continued')) continue;
    const strShort = strFirstLine.length > MAX_PROMPT_LENGTH ? `${strFirstLine.slice(0, MAX_PROMPT_LENGTH)}…` : strFirstLine;
    if (!lstPrompt.includes(strShort)) lstPrompt.push(strShort);
  }
  return lstPrompt;
};

// Đếm file đang thay đổi (chưa commit) theo khu vực: src/AuthService, frontend, .claude...
const summarizeChanges = (strProjectDir) => {
  try {
    const strStatus = execFileSync('git', ['-C', strProjectDir, 'status', '--porcelain', '--untracked-files=all'], { encoding: 'utf8', timeout: 5000 });
    const dicArea = {};
    for (const strLine of strStatus.split('\n').filter(Boolean)) {
      const lstPart = strLine.slice(3).replace(/"/g, '').split('/');
      const strArea = lstPart[0] === 'src' && lstPart[1] ? `src/${lstPart[1]}` : lstPart[0];
      dicArea[strArea] = (dicArea[strArea] ?? 0) + 1;
    }
    return Object.entries(dicArea).sort((a, b) => b[1] - a[1]).map(([k, v]) => `${k} (${v})`).join(', ');
  } catch {
    return '';
  }
};

const main = () => {
  const objInput = readStdin();
  const strProjectDir = process.env.CLAUDE_PROJECT_DIR || objInput.cwd || process.cwd();
  const strPlanPath = path.join(strProjectDir, 'TaskList', 'plan.md');
  if (!fs.existsSync(strPlanPath)) return;

  const lstPrompt = collectPrompts(objInput.transcript_path);
  // Phiên rỗng (mở rồi tắt) thì không ghi
  if (lstPrompt.length === 0) return;

  const dtmNow = new Date();
  const strTime = `${dtmNow.getFullYear()}-${String(dtmNow.getMonth() + 1).padStart(2, '0')}-${String(dtmNow.getDate()).padStart(2, '0')} ${String(dtmNow.getHours()).padStart(2, '0')}:${String(dtmNow.getMinutes()).padStart(2, '0')}`;
  // Lấy các yêu cầu gần nhất (phiên resume nhiều ngày thì đầu transcript là việc cũ)
  const strEarlier = lstPrompt.length > MAX_PROMPTS ? `(+${lstPrompt.length - MAX_PROMPTS} trước đó) ` : '';
  const strChanges = summarizeChanges(strProjectDir);
  const strEntry = `- ${strTime} · ${lstPrompt.length} yêu cầu: ${strEarlier}${lstPrompt.slice(-MAX_PROMPTS).join('; ')}`
    + (strChanges ? ` · Đang đổi (chưa commit): ${strChanges}` : '');

  const strPlan = fs.readFileSync(strPlanPath, 'utf8').replace(/\r\n/g, '\n');
  const intHeading = strPlan.indexOf(LOG_HEADING);
  const strBody = intHeading === -1 ? strPlan.trimEnd() : strPlan.slice(0, intHeading).trimEnd();
  const lstOld = intHeading === -1 ? [] : strPlan.slice(intHeading + LOG_HEADING.length).split('\n').filter((x) => x.startsWith('- '));
  const lstLog = [strEntry, ...lstOld].slice(0, MAX_LOG_ENTRIES);
  fs.writeFileSync(strPlanPath, `${strBody}\n\n${LOG_HEADING}\n> Hook SessionEnd tự ghi, mới nhất ở trên, giữ ${MAX_LOG_ENTRIES} dòng. Không đổi trạng thái task.\n\n${lstLog.join('\n')}\n`, 'utf8');
};

try {
  main();
} catch (error) {
  console.error(`[session-end-log] ${error.message}`);
}
