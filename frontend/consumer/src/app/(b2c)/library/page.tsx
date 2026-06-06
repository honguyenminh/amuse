import { redirect } from "next/navigation";
import { libraryPlaylistsPath } from "@/lib/discovery/paths";

export default function LibraryPage() {
  redirect(libraryPlaylistsPath);
}
