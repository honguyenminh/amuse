export function getApiBaseUrl(): string {
  return (
    process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ??
    "http://localhost:5000"
  );
}

export const WEB_CLIENT_HEADER = "web";
