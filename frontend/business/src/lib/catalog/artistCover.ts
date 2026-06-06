import {
  completeArtistCoverUpload,
  presignArtistCoverUpload,
  type CompleteArtistCoverUploadResponse,
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

export async function uploadArtistCover(
  artistId: string,
  file: File,
): Promise<CompleteArtistCoverUploadResponse> {
  const contentType =
    file.type || inferImageContentType(file.name) || "application/octet-stream";
  const presign = await presignArtistCoverUpload(artistId, {
    fileName: file.name,
    contentType,
  });
  const uploadResponse = await fetch(presign.url, {
    method: presign.method || "PUT",
    headers: { "Content-Type": contentType },
    body: file,
  });
  if (!uploadResponse.ok) {
    throw new Error("Failed to upload cover image.");
  }
  return completeArtistCoverUpload(artistId, { key: presign.key });
}
