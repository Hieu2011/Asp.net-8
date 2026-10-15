// Healthcheck cho Coolify (HTTP, image alpine có sẵn wget)
export function GET() {
  return Response.json({ status: 'ok' });
}
