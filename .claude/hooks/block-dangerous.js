// PreToolUse hook (matcher: Bash) — chặn cứng các lệnh phá huỷ dữ liệu/lịch sử git nguy hiểm,
// độc lập với việc Claude có "nhớ" phải hỏi trước hay không.
const DANGEROUS_PATTERNS = [
  { re: /git\s+push\s+.*(--force(?!-with-lease)|(?<!--force-with-lease\s)-f\b)/, why: 'force push (ghi đè history remote)' },
  { re: /git\s+reset\s+--hard/, why: 'reset --hard (mất thay đổi chưa commit)' },
  { re: /git\s+clean\s+.*-[a-z]*d[a-z]*f|git\s+clean\s+.*-[a-z]*f[a-z]*d/i, why: 'git clean -fd (xoá file chưa track vĩnh viễn)' },
  { re: /rm\s+-rf\s+(\/|~|\*|\.\s*$)/, why: 'rm -rf trên phạm vi rộng (root/home/wildcard)' },
  { re: /DROP\s+(TABLE|DATABASE)|TRUNCATE\s+TABLE/i, why: 'DROP/TRUNCATE trực tiếp qua shell' },
];

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
  for (const { re, why } of DANGEROUS_PATTERNS) {
    if (re.test(command)) {
      process.stderr.write(
        `[block-dangerous] Lệnh bị chặn: "${command}"\n` +
        `Lý do: ${why}. Đây là thao tác khó/không thể hoàn tác — hỏi user xác nhận rõ ràng trước ` +
        `khi thử lại (nếu thật sự cần, dùng lệnh cụ thể hơn thay vì wildcard/force).\n`
      );
      process.exit(2);
    }
  }
  process.exit(0);
});
