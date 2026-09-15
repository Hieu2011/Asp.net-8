// PreToolUse hook (matcher: Bash) — trước khi cho `git push`, bắt buộc build + test phải pass.
// Biến rule "sửa code xong phải build/test" (working-agreements.md) từ tự giác thành ép buộc.
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

  const command = payload?.tool_input?.command || '';
  if (!/git\s+push/.test(command)) {
    process.exit(0);
  }

  try {
    execSync('dotnet build WebApiCore8.sln --configuration Release', {
      encoding: 'utf8',
      stdio: 'pipe',
    });
  } catch (err) {
    process.stderr.write(
      `[test-gate] Build FAIL — không cho push.\n${(err.stdout || '').toString().slice(-2000)}\n`
    );
    process.exit(2);
  }

  try {
    execSync('dotnet test WebApiCore8.sln --configuration Release --no-build', {
      encoding: 'utf8',
      stdio: 'pipe',
    });
  } catch (err) {
    process.stderr.write(
      `[test-gate] Test FAIL — không cho push.\n${(err.stdout || '').toString().slice(-2000)}\n`
    );
    process.exit(2);
  }

  process.exit(0);
});
