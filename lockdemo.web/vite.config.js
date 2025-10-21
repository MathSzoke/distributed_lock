import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"

export default defineConfig({
    base: "./",
    publicDir: "public",
    plugins: [react()],
    server: {
        host: true,
        port: parseInt(process.env.VITE_PORT, 10) || 3001
    }
})
