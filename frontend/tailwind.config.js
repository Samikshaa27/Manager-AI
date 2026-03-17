/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#D946EF', // fuchsia-500
          foreground: '#FFFFFF',
          hover: '#C026D3', // fuchsia-600
        },
        secondary: {
          DEFAULT: '#8B5CF6', // violet-500
          foreground: '#FFFFFF',
        },
        background: '#FAF5FF', // fuchsia-50
        surface: 'rgba(255, 255, 255, 0.7)',
        text: {
          DEFAULT: '#1E293B', // slate-800
          muted: '#64748B', // slate-500
        },
        border: 'rgba(217, 70, 239, 0.1)', // primary/10
        glass: {
          tab: 'rgba(255, 255, 255, 0.5)',
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        heading: ['Outfit', 'sans-serif'],
      },
      backgroundImage: {
        'gradient-radial': 'radial-gradient(var(--tw-gradient-stops))',
        'soft-gradient': 'linear-gradient(135deg, #FAF5FF 0%, #FDF2F8 100%)',
      },
      animation: {
        'float': 'float 6s ease-in-out infinite',
        'pulse-slow': 'pulse 4s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-10px)' },
        }
      }
    },
  },
  plugins: [],
}
