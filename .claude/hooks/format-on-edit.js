// PostToolUse hook (matcher: Edit|Write) — tự format file vừa sửa: .cs → dotnet format, frontend/ → prettier. Best-effort, không
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
  const isFrontend = /[\\/]frontend[\\/](?!node_modules|\.next)/.test(filePath) && /\.(tsx?|mjs|css|json|md)$/.test(filePath);
  if (!filePath.endsWith('.cs') && !isFrontend) {
    process.exit(0);
  }

  try {
    // Frontend: prettier của chính project (cwd frontend để đọc .prettierrc)
    const command = isFrontend ? `pnpm exec prettier --write "${filePath}"` : `dotnet format All.sln --include "${filePath}"`;
    execSync(command, {
      encoding: 'utf8',
      stdio: 'pipe',
      timeout: 30000,
      cwd: isFrontend ? filePath.replace(/([\\/]frontend)[\\/].*$/, '$1') : undefined,
    });
  } catch {
    // Best-effort — format lỗi (VD file có lỗi syntax tạm thời giữa lúc đang sửa) thì bỏ qua, không báo lỗi ồn ào
  }
  process.exit(0);
});
