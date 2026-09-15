// PostToolUse hook (matcher: Edit|Write) — tự dotnet format file .cs vừa sửa. Best-effort, không
// chặn gì (luôn exit 0) vì đây chỉ là tiện ích giữ style, không phải an toàn/bảo mật.
const { execSync } = require('child_process');

let input = '';
process.stdin.on('data', (d) => (input += d));
process.stdin.on('end', () => {
  let payload;
  try {
    payload = JSON.parse(input);
  } catch {
    process.exit(0);
  }

  const filePath = payload?.tool_input?.file_path || '';
  if (!filePath.endsWith('.cs')) {
    process.exit(0);
  }

  try {
    execSync(`dotnet format WebApiCore8.sln --include "${filePath}"`, {
      encoding: 'utf8',
      stdio: 'pipe',
      timeout: 30000,
    });
  } catch {
    // Best-effort — format lỗi (VD file có lỗi syntax tạm thời giữa lúc đang sửa) thì bỏ qua, không báo lỗi ồn ào
  }
  process.exit(0);
});
