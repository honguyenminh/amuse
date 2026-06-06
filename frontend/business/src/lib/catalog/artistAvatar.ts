import {
  completeArtistAvatarUpload,
  presignArtistAvatarUpload,
  type CompleteArtistAvatarUploadResponse,
} from "@/lib/api/catalogClient";

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

export async function uploadArtistAvatar(
  artistId: string,
  file: File,
): Promise<CompleteArtistAvatarUploadResponse> {
  const contentType =
    file.type || inferImageContentType(file.name) || "application/octet-stream";
  const presign = await presignArtistAvatarUpload(artistId, {
    fileName: file.name,
    contentType,
  });
  const uploadResponse = await fetch(presign.url, {
    method: presign.method || "PUT",
    headers: { "Content-Type": contentType },
    body: file,
  });
  if (!uploadResponse.ok) {
    throw new Error("Failed to upload profile picture.");
  }
  return completeArtistAvatarUpload(artistId, { key: presign.key });
}
