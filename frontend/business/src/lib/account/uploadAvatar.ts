import {
  completePortalAvatarUpload,
  presignPortalAvatarUpload,
} from "@/lib/api/tenancyClient";

function inferImageContentType(fileName: string): string | null {
  const ext = fileName.split(".").pop()?.toLowerCase();
  switch (ext) {
    case "jpg":
    case "jpeg":
      return "image/jpeg";
    case "png":
      return "image/png";
    case "webp":
      return "image/webp";
    default:
      return null;
  }
}

export async function uploadPortalAvatar(file: File): Promise<string> {
  const contentType =
    file.type || inferImageContentType(file.name) || "application/octet-stream";
  const presign = await presignPortalAvatarUpload({
    fileName: file.name,
    contentType,
  });
  const uploadResponse = await fetch(presign.url, {
    method: presign.method || "PUT",
    headers: { "Content-Type": contentType },
    body: file,
  });
  if (!uploadResponse.ok) {
    throw new Error("Failed to upload avatar image.");
  }
  const completed = await completePortalAvatarUpload({ key: presign.key });
  return completed.avatarUrl;
}
