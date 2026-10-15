import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// API URL za dev proxy (podesi VITE_API_URL ako backend ne radi na 5266).
const target = process.env.VITE_API_URL || "http://localhost:5266";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": { target, changeOrigin: true },
      "/uploads": { target, changeOrigin: true },
    },
  },
});
