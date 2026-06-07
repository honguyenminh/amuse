import { ApiError } from "@/lib/api/types";

export function isNotFoundError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 400;
}
