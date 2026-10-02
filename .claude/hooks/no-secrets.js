// PreToolUse hook (matcher: Bash) — chặn `git commit` nếu diff đã stage chứa pattern giống
// secret thật (password/connection string/API key...). Không chặn `git push` vì secret đã lọt
// vào history từ lúc commit rồi — chặn ở commit là đúng thời điểm duy nhất còn kịp.
const { execSync } = require('child_process');

const SECRET_PATTERNS = [
  /(password|pwd)\s*[:=]\s*["'][^"'\s]{4,}["']/i,
  /(secret|apikey|api_key|access_key|accesstoken|refreshtoken)\s*[:=]\s*["'][^"'\s]{6,}["']/i,
  /Password\s*=\s*[^;"'\s]{4,}/i, // connection string kiểu Postgres/SqlServer: Password=xxxx;
  /-----BEGIN (RSA |EC )?PRIVATE KEY-----/,
];

let input = '';
process.stdin.on('data', (d) => (input += d));
process.stdin.on('end', () => {
  let payload;
  try {
    payload = JSON.parse(input);
  } catch {
    process.exit(0); // Không parse được thì không chặn — tránh false positive khoá luôn dev
  }

  const command = payload?.tool_input?.command || '';
  if (!/git\s+commit/.test(command)) {
    process.exit(0);
  }

  let diff = '';
  try {
    diff = execSync('git diff --cached', { encoding: 'utf8', maxBuffer: 10 * 1024 * 1024 });
  } catch {
    process.exit(0); // Không lấy được diff (VD không phải repo) — bỏ qua, không chặn
  }

  for (const pattern of SECRET_PATTERNS) {
    const match = diff.match(pattern);
    if (match) {
      process.stderr.write(
        `[no-secrets] Phát hiện pattern giống secret thật trong staged diff: "${match[0].slice(0, 60)}"...\n` +
        `Nếu đây là giá trị test/rỗng thì bỏ qua bằng cách sửa lại pattern, còn nếu là secret thật ` +
        `hãy chuyển sang User Secrets/env var trước khi commit.\n`
      );
      process.exit(2); // exit 2 = block, stderr được đưa lại cho Claude/user xem lý do
    }
  }
  process.exit(0);
});
