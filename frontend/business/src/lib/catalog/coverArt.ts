import {
  completeReleaseCoverUpload,
  presignReleaseCoverUpload,
  type CompleteReleaseCoverUploadResponse,
} from "@/lib/api/catalogClient";
import { parseBlob } from "music-metadata";

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

function mimeToExtension(mimeType: string): string {
  switch (mimeType.toLowerCase()) {
    case "image/png":
      return ".png";
    case "image/webp":
      return ".webp";
    default:
      return ".jpg";
  }
}

export async function uploadReleaseCoverArt(
  releaseId: string,
  file: File,
): Promise<CompleteReleaseCoverUploadResponse> {
  const contentType =
    file.type || inferImageContentType(file.name) || "application/octet-stream";
  const presign = await presignReleaseCoverUpload(releaseId, {
    fileName: file.name,
    contentType,
  });
  const uploadResponse = await fetch(presign.url, {
    method: presign.method || "PUT",
    headers: { "Content-Type": contentType },
    body: file,
  });
  if (!uploadResponse.ok) {
    throw new Error("Failed to upload cover file.");
  }
  return completeReleaseCoverUpload(releaseId, { key: presign.key });
}

export async function extractEmbeddedCoverArt(file: File): Promise<File | null> {
  try {
    const metadata = await parseBlob(file, { skipCovers: false });
    const picture = metadata.common.picture?.[0];
    if (!picture?.data?.length) {
      return null;
    }

    const mimeType = picture.format || "image/jpeg";
    return new File(
      [Uint8Array.from(picture.data)],
      `embedded-cover${mimeToExtension(mimeType)}`,
      { type: mimeType },
    );
  } catch {
    return null;
  }
}

export async function extractEmbeddedCoverArtFromFiles(
  files: Iterable<File>,
): Promise<{ cover: File; sourceFileName: string } | null> {
  for (const file of files) {
    const cover = await extractEmbeddedCoverArt(file);
    if (cover) {
      return { cover, sourceFileName: file.name };
    }
  }
  return null;
}
