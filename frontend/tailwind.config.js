/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        page: '#F8FAFC',
        surface: '#FFFFFF',
        border: '#E2E8F0',
        text: {
          DEFAULT: '#0F172A',
          muted: '#64748B',
        },
        primary: {
          DEFAULT: '#2563EB',
          soft: '#EFF6FF',
          dark: '#1D4ED8',
        },
        success: {
          DEFAULT: '#16A34A',
          soft: '#DCFCE7',
          dark: '#15803D',
        },
        warning: {
          DEFAULT: '#D97706',
          soft: '#FEF3C7',
          dark: '#B45309',
        },
        danger: {
          DEFAULT: '#DC2626',
          soft: '#FEE2E2',
          dark: '#B91C1C',
        },
        info: {
          DEFAULT: '#7C3AED',
          soft: '#EDE9FE',
          dark: '#6D28D9',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
      },
      boxShadow: {
        card: '0 1px 3px 0 rgba(15, 23, 42, 0.06), 0 1px 2px 0 rgba(15, 23, 42, 0.04)',
        dropdown: '0 10px 15px -3px rgba(15, 23, 42, 0.08), 0 4px 6px -2px rgba(15, 23, 42, 0.05)',
      },
      borderRadius: {
        card: '10px',
        input: '8px',
      }
    },
  },
  plugins: [],
};
