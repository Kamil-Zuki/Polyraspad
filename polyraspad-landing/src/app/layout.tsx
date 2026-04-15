import type { Metadata, Viewport } from "next"
import { Inter, Noto_Sans_KR } from "next/font/google"
import "./globals.css"

const inter = Inter({
  subsets: ["latin", "cyrillic"],
  weight: ["400", "500", "600", "700", "800"],
  display: "swap",
})

const notoSansKr = Noto_Sans_KR({
  subsets: ["latin"],
  weight: ["400", "500", "700"],
  display: "swap",
  variable: "--font-noto-sans-kr",
})

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || "https://polyraspad.online"

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: "PVS.ai",
    template: "%s | PVS.ai",
  },
  description:
    "Выучи язык через контекст, FSRS и AI-ассистента. Платформа для изучения языков, маркетплейса колод и Creator Studio.",
  openGraph: {
    type: "website",
    locale: "ru_RU",
    siteName: "PVS.ai",
    title: "PVS.ai",
    description:
      "Выучи язык через контекст, FSRS и AI-ассистента. Платформа для изучения языков, маркетплейса колод и Creator Studio.",
    url: siteUrl,
  },
}

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  themeColor: "#0b0f19",
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="ru">
      <body className={`${inter.className} ${notoSansKr.variable} antialiased`}>{children}</body>
    </html>
  )
}
