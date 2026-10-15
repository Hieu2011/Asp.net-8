#!/usr/bin/env bash
# Gán tag image = sha cho app Coolify rồi gọi deploy. Dùng chung cho mọi app (ApiCore8, frontend...).
# Env cần: COOLIFY_URL, COOLIFY_TOKEN, COOLIFY_UUID, IMAGE_TAG, APP_LABEL
set -uo pipefail
# Secret copy-paste hay dính newline cuối → làm vỡ header (Coolify trả 400)
COOLIFY_TOKEN=$(printf '%s' "$COOLIFY_TOKEN" | tr -d '[:space:]')
COOLIFY_UUID=$(printf '%s' "$COOLIFY_UUID" | tr -d '[:space:]')
if [[ -z "$COOLIFY_UUID" ]]; then
  echo "::warning::Chưa cấu hình UUID app Coolify cho $APP_LABEL — image đã lên GHCR, bỏ qua bước deploy."
  exit 0
fi
# Mỗi bản 1 tag riêng → tab Rollback của Coolify quay về được
intStatus=$(curl -sS -o patch.json -w "%{http_code}" -X PATCH "$COOLIFY_URL/applications/$COOLIFY_UUID" \
  -H "Content-Type: application/json" -H "Accept: application/json" -H "Authorization: Bearer $COOLIFY_TOKEN" \
  -d "{\"docker_registry_image_tag\":\"$IMAGE_TAG\"}" || echo 000)
if [[ "$intStatus" != 2* ]]; then
  echo "::warning::Không gán được tag $IMAGE_TAG cho $APP_LABEL (HTTP $intStatus): $(cat patch.json 2>/dev/null) — vẫn deploy tiếp."
fi
# Coolify v4.3+: deploy bắt buộc POST (GET trả 405)
if curl -fsS -X POST "$COOLIFY_URL/deploy?uuid=$COOLIFY_UUID&force=false" -H "Authorization: Bearer $COOLIFY_TOKEN"; then
  echo "✅ Đã gọi Coolify deploy $APP_LABEL (tag $IMAGE_TAG)."
else
  echo "::warning::Không gọi được Coolify cho $APP_LABEL — image đã lên GHCR, vào Coolify qua Tailscale bấm Redeploy."
fi
