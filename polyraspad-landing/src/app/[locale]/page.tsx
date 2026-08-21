import type { Metadata } from "next"
import { notFound } from "next/navigation"
import {
  AppWindow,
  ArrowRight,
  BookOpen,
  Brain,
  Check,
  CirclePlay,
  Globe,
  Puzzle,
  Sparkles,
} from "lucide-react"
import { type LandingLocale, landingContent } from "../../lib/landing-content"
import { LanguageDropdown } from "../../components/language-dropdown"

const APP_URL = process.env.NEXT_PUBLIC_APP_URL || "https://app.polyraspad.online"

const featureIcons = [
  {
    icon: BookOpen,
    iconClassName: "text-[#8B5CF6] bg-[#8B5CF6]/20 border-[#8B5CF6]/30",
  },
  {
    icon: Brain,
    iconClassName: "text-[#3B82F6] bg-[#3B82F6]/20 border-[#3B82F6]/30",
  },
  {
    icon: AppWindow,
    iconClassName: "text-green-400 bg-green-500/20 border-green-500/30",
  },
] as const

const localeLabels: Record<LandingLocale, string> = {
  ru: "RU",
  en: "EN",
  ko: "KO",
}

const localeOpenGraph: Record<LandingLocale, string> = {
  ru: "ru_RU",
  en: "en_US",
  ko: "ko_KR",
}

function getLocale(locale: string): LandingLocale | null {
  if (locale === "ru" || locale === "en" || locale === "ko") {
    return locale
  }

  return null
}

export function generateStaticParams() {
  return [{ locale: "ru" }, { locale: "en" }, { locale: "ko" }]
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>
}): Promise<Metadata> {
  const { locale } = await params
  const resolvedLocale = getLocale(locale)

  if (!resolvedLocale) {
    return {}
  }

  const content = landingContent[resolvedLocale]
  const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || "https://polyraspad.online"
  const localizedUrl = `${siteUrl}/${resolvedLocale}`

  return {
    title: content.metadata.title,
    description: content.metadata.description,
    alternates: {
      canonical: localizedUrl,
      languages: {
        ru: `${siteUrl}/ru`,
        en: `${siteUrl}/en`,
        ko: `${siteUrl}/ko`,
      },
    },
    openGraph: {
      title: content.metadata.title,
      description: content.metadata.description,
      url: localizedUrl,
      locale: localeOpenGraph[resolvedLocale],
    },
  }
}

function FooterLink({
  href,
  label,
  external,
}: {
  href: string
  label: string
  external?: boolean
}) {
  const resolvedHref = href.startsWith("/") ? `${APP_URL}${href}` : href

  return (
    <a
      href={resolvedHref}
      target={external ? "_blank" : undefined}
      rel={external ? "noreferrer" : undefined}
      className="transition hover:text-white"
    >
      {label}
    </a>
  )
}

export default async function LocalizedHomePage({
  params,
}: {
  params: Promise<{ locale: string }>
}) {
  const { locale } = await params
  const resolvedLocale = getLocale(locale)

  if (!resolvedLocale) {
    notFound()
  }

  const content = landingContent[resolvedLocale]
  const localeLinks = (Object.keys(localeLabels) as LandingLocale[]).filter((item) => item !== resolvedLocale)

  return (
    <div className="relative min-h-screen overflow-x-hidden bg-[#0b0f19] text-gray-100">
      <div className="pointer-events-none fixed inset-0 z-0 overflow-hidden">
      </div>

      <nav className="glass-panel fixed inset-x-0 top-0 z-50 border-x-0 border-b border-t-0 border-white/10 bg-[#0b0f19]/60">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 md:h-[4.5rem] md:px-6">
          <a href={`/${resolvedLocale}`} className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-600 font-bold text-white">
              P
            </div>
            <span className="text-lg font-bold tracking-tight text-white md:text-xl">Polyraspad</span>
          </a>

          <div className="hidden items-center gap-6 text-sm font-medium text-gray-300 md:flex">
            {content.navItems.map((item) => (
              <a key={item.href} href={item.href} className="transition hover:text-white">
                {item.label}
              </a>
            ))}
          </div>

          <div className="flex items-center gap-3">
            <LanguageDropdown
              currentLabel={localeLabels[resolvedLocale]}
              options={localeLinks.map((item) => ({
                href: `/${item}`,
                label: localeLabels[item],
              }))}
            />
            <a href={`${APP_URL}/auth?mode=login`} className="hidden text-sm font-medium text-gray-300 transition hover:text-white md:block">
              {content.loginLabel}
            </a>
            <a href={`${APP_URL}/auth?mode=register`} className="btn-primary rounded-lg px-4 py-2.5 text-sm font-bold text-white md:px-5">
              {content.startFreeLabel}
            </a>
          </div>
        </div>
      </nav>

      <main className="relative z-10">
        <section className="landing-section px-6 pb-14 pt-28 text-center md:pb-16 md:pt-32">
          <div className="mx-auto max-w-4xl">
            <div className="mb-5 inline-flex items-center gap-2 rounded-full border border-[#8B5CF6]/30 bg-[#8B5CF6]/10 px-3 py-1.5 text-xs font-bold uppercase tracking-wider text-[#8B5CF6]">
              <Sparkles className="h-4 w-4" />
              {content.badge}
            </div>

            <h1 className="mb-5 text-4xl font-extrabold leading-tight tracking-tight text-white md:text-6xl">
              {content.heroTitle}
              <br />
              <span className="text-blue-400">{content.heroAccent}</span>
            </h1>

            <p className="mx-auto mb-8 max-w-2xl text-base leading-relaxed text-gray-400 md:text-lg">
              {content.heroDescription}
            </p>

            <div className="flex flex-col items-center justify-center gap-4 sm:flex-row">
              <a
                href={`${APP_URL}/auth?mode=register`}
                className="btn-primary flex w-full items-center justify-center gap-2 rounded-xl px-6 py-3.5 text-base font-bold text-white sm:w-auto md:text-lg"
              >
                {content.heroPrimaryCta}
                <ArrowRight className="h-4 w-4" />
              </a>
            </div>

            <p className="mt-5 text-sm text-gray-500">{content.heroSocialProof}</p>
          </div>


        </section>

        <section className="landing-section border-y border-white/5 bg-[#111625]/30 py-16 md:py-20">
          <div className="mx-auto max-w-4xl px-6 text-center">
            <h2 className="mb-5 text-2xl font-bold text-white md:text-3xl">{content.problemTitle}</h2>
            <p className="text-base leading-relaxed text-gray-400 md:text-lg">{content.problemDescription}</p>
            <p className="mt-4 text-lg font-medium text-[#8B5CF6] md:text-xl">{content.problemAccent}</p>
          </div>
        </section>

        <section id="features" className="landing-section px-6 py-16 md:py-20">
          <div className="mx-auto max-w-5xl">
            <div className="mb-12 text-center">
              <h2 className="mb-3 text-2xl font-bold text-white md:text-4xl">{content.featuresTitle}</h2>
              <p className="text-base text-gray-400 md:text-lg">{content.featuresSubtitle}</p>
            </div>

            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              {content.features.map((feature, index) => {
                const iconConfig = featureIcons[index]
                const Icon = iconConfig.icon

                return (
                  <article key={feature.title} className="glass-panel rounded-2xl p-6">
                    <div
                      className={`mb-4 flex h-12 w-12 items-center justify-center rounded-xl border text-xl ${iconConfig.iconClassName}`}
                    >
                      <Icon className="h-5 w-5" />
                    </div>
                    <h3 className="mb-3 text-lg font-bold text-white md:text-xl">{feature.title}</h3>
                    <p className="text-sm leading-relaxed text-gray-400 md:text-base">{feature.description}</p>
                  </article>
                )
              })}
            </div>
          </div>
        </section>


        <section className="landing-section px-6 py-16 md:py-20">
          <div className="mx-auto max-w-4xl rounded-3xl border border-white/10 bg-white/5 p-8 text-center md:p-10">
            <h2 className="mb-4 text-2xl font-bold text-white md:text-4xl">{content.finalTitle}</h2>
            <p className="mx-auto mb-6 max-w-2xl text-base text-indigo-200 md:text-lg">{content.finalDescription}</p>

            <form action={`${APP_URL}/auth`} method="get" className="mx-auto flex max-w-md flex-col items-center justify-center gap-3 sm:flex-row">
              <input type="hidden" name="mode" value="register" />
              <input
                type="email"
                name="email"
                placeholder={content.finalPlaceholder}
                className="w-full rounded-xl border border-white/20 bg-black/30 px-4 py-3 text-white placeholder-indigo-300 focus:border-white focus:outline-none"
              />
              <button
                type="submit"
                className="w-full whitespace-nowrap rounded-xl bg-white px-6 py-3 font-bold text-[#8B5CF6] transition hover:bg-gray-100 sm:w-auto"
              >
                {content.finalButton}
              </button>
            </form>
          </div>
        </section>
      </main>

      <footer className="border-t border-white/5 bg-[#0b0f19] px-6 pb-6 pt-12 text-sm text-gray-500">
        <div className="mx-auto mb-10 grid max-w-7xl grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <div className="mb-4 flex items-center gap-2">
              <div className="flex h-6 w-6 items-center justify-center rounded bg-blue-600 text-xs font-bold text-white">P</div>
              <span className="font-bold text-white">Polyraspad</span>
            </div>
            <p className="mb-4">{content.footerDescription}</p>
          </div>

          {content.footerGroups.map((group) => (
            <div key={group.title}>
              <h4 className="mb-4 text-xs font-bold uppercase tracking-wider text-white">{group.title}</h4>
              <ul className="space-y-2">
                {group.links.map((link) => (
                  <li key={link.label}>
                    <FooterLink href={link.href} label={link.label} external={link.external} />
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mx-auto max-w-7xl border-t border-white/5 pt-8 text-center">
          &copy; {new Date().getFullYear()} Polyraspad. All rights reserved.
        </div>
      </footer>
    </div>
  )
}
