import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  transpilePackages: ["@amuse/catalog-text"],
};

export default nextConfig;
