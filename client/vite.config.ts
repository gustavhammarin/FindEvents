import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import mkcert from 'vite-plugin-mkcert'
import tailwindcss from "@tailwindcss/vite"
import path from "path"


// https://vite.dev/config/
export default defineConfig({
  server: {
    port: 3000
  },
  plugins: [react(), mkcert(), tailwindcss()],
    resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },

})
