import type { Metadata } from "next"
import Image from "next/image"
import { notFound } from "next/navigation"
import {
  ArrowRight,
  BookOpen,
  Bot,
  Brain,
  Bug,
  Check,
  CirclePlay,
  Coins,
  Globe,
  PenTool,
  Puzzle,
  Shield,
  Sparkles,
  TrendingUp,
  Wifi,
} from "lucide-react"
import { type LandingLocale, landingContent } from "../../lib/landing-content"
import { LanguageDropdown } from "../../components/language-dropdown"

const APP_URL = process.env.NEXT_PUBLIC_APP_URL || "https://app.polyraspad.online"

const featureIcons = [
  {
    icon: Puzzle,
    iconClassName: "text-[#8B5CF6] bg-[#8B5CF6]/20 border-[#8B5CF6]/30",
  },
  {
    icon: Brain,
    iconClassName: "text-[#3B82F6] bg-[#3B82F6]/20 border-[#3B82F6]/30",
  },
  {
    icon: Bot,
    iconClassName: "text-[#EC4899] bg-[#EC4899]/20 border-[#EC4899]/30",
  },
  {
    icon: Globe,
    iconClassName: "text-green-400 bg-green-500/20 border-green-500/30",
  },
] as const

const creatorIcons = [
  { icon: Shield, iconClassName: "text-[#8B5CF6]" },
  { icon: TrendingUp, iconClassName: "text-[#3B82F6]" },
  { icon: Coins, iconClassName: "text-green-400" },
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
  const creatorsTitleParts = content.creatorsTitle.split("\n")
  const localeLinks = (Object.keys(localeLabels) as LandingLocale[]).filter((item) => item !== resolvedLocale)

  return (
    <div className="relative min-h-screen overflow-x-hidden bg-[#0b0f19] text-gray-100">
      <div className="pointer-events-none fixed inset-0 z-0 overflow-hidden">
        <div className="landing-blob absolute left-1/4 top-0 h-96 w-96 rounded-full bg-[#8B5CF6]/20 blur-[100px]" />
        <div className="landing-blob landing-blob-delay-2 absolute right-1/4 top-40 h-96 w-96 rounded-full bg-[#3B82F6]/20 blur-[100px]" />
        <div className="landing-blob landing-blob-delay-4 absolute -bottom-32 left-1/3 h-96 w-96 rounded-full bg-[#EC4899]/10 blur-[100px]" />
      </div>

      <nav className="glass-panel fixed inset-x-0 top-0 z-50 border-x-0 border-b border-t-0 border-white/10 bg-[#0b0f19]/60">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 md:h-[4.5rem] md:px-6">
          <a href={`/${resolvedLocale}`} className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-[#8B5CF6] to-[#3B82F6] font-bold text-white shadow-[0_0_15px_rgba(139,92,246,0.4)]">
              P
            </div>
            <span className="text-lg font-bold tracking-tight text-white md:text-xl">PVS.ai</span>
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
            <a href={`${APP_URL}/auth`} className="hidden text-sm font-medium text-gray-300 transition hover:text-white md:block">
              {content.loginLabel}
            </a>
            <a href={`${APP_URL}/auth`} className="btn-primary rounded-lg px-4 py-2.5 text-sm font-bold text-white md:px-5">
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
              <span className="gradient-text">{content.heroAccent}</span>
            </h1>

            <p className="mx-auto mb-8 max-w-2xl text-base leading-relaxed text-gray-400 md:text-lg">
              {content.heroDescription}
            </p>

            <div className="flex flex-col items-center justify-center gap-4 sm:flex-row">
              <a
                href={`${APP_URL}/auth`}
                className="btn-primary flex w-full items-center justify-center gap-2 rounded-xl px-6 py-3.5 text-base font-bold text-white sm:w-auto md:text-lg"
              >
                {content.heroPrimaryCta}
                <ArrowRight className="h-4 w-4" />
              </a>
              <a
                href={`${APP_URL}/marketplace`}
                className="glass-panel flex w-full items-center justify-center gap-2 rounded-xl px-6 py-3.5 text-base font-bold text-white transition hover:bg-white/10 sm:w-auto md:text-lg"
              >
                <CirclePlay className="h-5 w-5 text-[#8B5CF6]" />
                {content.heroSecondaryCta}
              </a>
            </div>

            <p className="mt-5 text-sm text-gray-500">{content.heroSocialProof}</p>
          </div>

          <div className="relative mx-auto mt-12 max-w-4xl md:mt-14">
            {/* unoptimized: загрузка напрямую в браузере, без /_next/image — иначе частые таймауты к Unsplash на сервере */}
            <Image
              src="https://images.unsplash.com/photo-1618761714954-0b8cd0026356?q=80&w=2070&auto=format&fit=crop"
              alt={content.heroImageAlt}
              width={2070}
              height={1380}
              priority
              unoptimized
              sizes="(max-width: 896px) 100vw, 896px"
              className="relative z-0 h-auto w-full max-w-full rounded-2xl border border-white/10 opacity-60 shadow-2xl"
            />
            {/* Градиент поверх фото; pointer-events — чтобы не перехватывать клики */}
            <div
              className="pointer-events-none absolute inset-0 z-10 rounded-2xl bg-gradient-to-t from-[#0b0f19] via-transparent to-transparent"
              aria-hidden
            />
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
          <div className="mx-auto max-w-7xl">
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

        <section
          id="marketplace"
          className="landing-section overflow-hidden border-y border-white/5 bg-[#111625]/50 px-6 py-16 md:py-20"
        >
          <div className="mx-auto flex max-w-7xl flex-col items-center gap-10 md:gap-12 lg:flex-row">
            <div className="lg:w-1/2">
              <div className="mb-2 text-xs font-bold uppercase tracking-wider text-[#3B82F6] md:text-sm">
                {content.marketplaceEyebrow}
              </div>
              <h2 className="mb-5 text-2xl font-bold text-white md:text-4xl">{content.marketplaceTitle}</h2>
              <p className="mb-5 text-base leading-relaxed text-gray-400 md:text-lg">
                {content.marketplaceDescription}
              </p>
              <ul className="mb-7 space-y-3 text-sm text-gray-300 md:text-base">
                {content.marketplaceBenefits.map((item) => (
                  <li key={item.title} className="flex items-start gap-3">
                    <Check className="mt-1 h-5 w-5 text-[#8B5CF6]" />
                    <div>
                      <strong>{item.title}:</strong> {item.description}
                    </div>
                  </li>
                ))}
              </ul>
              <a href={`${APP_URL}/marketplace`} className="btn-primary inline-block rounded-lg px-6 py-3 font-bold text-white">
                {content.marketplaceCta}
              </a>
            </div>

            <div className="w-full lg:w-1/2">
              <div className="glass-panel rotate-2 rounded-2xl p-3 transition duration-500 hover:rotate-0 md:p-4">
                <Image
                  src="https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=800&q=80"
                  alt={content.marketplaceTitle}
                  width={800}
                  height={520}
                  unoptimized
                  className="mb-3 h-auto w-full rounded-xl opacity-80"
                />
                <div className="flex items-center justify-between px-2">
                  <div>
                    <div className="text-base font-bold text-white md:text-lg">Business English Pro</div>
                    <div className="text-xs text-gray-400">{content.marketplaceCardMeta}</div>
                  </div>
                  <div className="rounded border border-[#8B5CF6]/30 bg-[#8B5CF6]/20 px-3 py-1 font-bold text-[#8B5CF6]">
                    $19.99
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="creators" className="landing-section px-6 py-16 md:py-20">
          <div className="mx-auto max-w-4xl text-center">
            <PenTool className="mx-auto mb-5 h-10 w-10 text-[#EC4899]" />
            <h2 className="mb-5 text-2xl font-bold text-white md:text-4xl">
              {creatorsTitleParts[0]}
              <br />
              {creatorsTitleParts[1]}
            </h2>
            <p className="mb-8 text-base text-gray-400 md:text-lg">{content.creatorsDescription}</p>

            <div className="mb-8 grid grid-cols-1 gap-4 text-left md:grid-cols-3 md:gap-6">
              {content.creatorBenefits.map((item, index) => {
                const iconConfig = creatorIcons[index]
                const Icon = iconConfig.icon

                return (
                  <article key={item.title} className="glass-panel rounded-xl p-5">
                    <Icon className={`mb-3 h-5 w-5 ${iconConfig.iconClassName}`} />
                    <h4 className="mb-2 font-bold text-white">{item.title}</h4>
                    <p className="text-sm text-gray-400">{item.description}</p>
                  </article>
                )
              })}
            </div>

            <a
              href={`${APP_URL}/dashboard`}
              className="border-b border-[#EC4899] pb-1 font-bold text-[#EC4899] transition hover:text-white"
            >
              {content.creatorsCta}
            </a>
          </div>
        </section>

        <section className="landing-section border-y border-[#8B5CF6]/20 bg-[#8B5CF6]/10 py-12 md:py-14">
          <div className="mx-auto flex max-w-7xl flex-col items-center justify-center gap-6 px-6 text-center md:flex-row md:text-left">
            <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full border border-[#8B5CF6]/30 bg-[#0b0f19] text-[#8B5CF6]">
              <Wifi className="h-7 w-7" />
            </div>
            <div>
              <h3 className="mb-2 text-xl font-bold text-white md:text-2xl">{content.offlineTitle}</h3>
              <p className="text-sm text-gray-400 md:text-base">{content.offlineDescription}</p>
            </div>
          </div>
        </section>

        <section id="pricing" className="landing-section px-6 py-16 md:py-20">
          <div className="mx-auto max-w-7xl">
            <div className="mb-12 text-center">
              <h2 className="mb-3 text-2xl font-bold text-white md:text-4xl">{content.pricingTitle}</h2>
              <p className="text-base text-gray-400 md:text-lg">{content.pricingSubtitle}</p>
            </div>

            <div className="mx-auto grid max-w-5xl grid-cols-1 gap-6 md:grid-cols-3">
              {content.pricingPlans.map((plan) => (
                <article
                  key={plan.name}
                  className={
                    plan.name === "Pro"
                      ? "landing-pro-card flex flex-col rounded-2xl bg-[#111625] p-6 shadow-2xl shadow-purple-900/50 md:-translate-y-4"
                      : "glass-panel flex flex-col rounded-2xl p-6"
                  }
                >
                  {plan.name === "Pro" ? (
                    <div className="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-gradient-to-r from-[#8B5CF6] to-[#EC4899] px-3 py-1 text-[10px] font-bold uppercase tracking-widest text-white">
                      {content.pricingFeaturedBadge}
                    </div>
                  ) : null}

                  <h3 className="mb-2 text-lg font-bold text-white md:text-xl">{plan.name}</h3>
                  <div className="mb-1 text-2xl font-bold text-white md:text-3xl">
                    {plan.price}
                    {plan.suffix ? <span className="text-sm font-normal text-gray-400"> {plan.suffix}</span> : null}
                  </div>
                  {plan.note ? <p className="mb-6 text-xs text-gray-400">{plan.note}</p> : <div className="mb-5" />}

                  <ul className={`mb-6 flex-1 space-y-3 text-sm ${plan.name === "Pro" ? "text-gray-300" : "text-gray-400"}`}>
                    {plan.items.map((item) => (
                      <li key={item} className="flex items-start gap-2">
                        <Check className={`mt-0.5 h-4 w-4 ${plan.name === "Pro" ? "text-[#EC4899]" : "text-[#8B5CF6]"}`} />
                        <span>{item}</span>
                      </li>
                    ))}
                  </ul>

                  <a
                    href={plan.name === "Creator" ? `${APP_URL}/dashboard` : `${APP_URL}/auth`}
                    className={
                      plan.name === "Pro"
                        ? "btn-primary block w-full rounded-xl py-3 text-center font-bold text-white"
                        : "block w-full rounded-xl border border-white/20 py-3 text-center font-bold text-white transition hover:bg-white/10"
                    }
                  >
                    {plan.cta}
                  </a>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className="landing-section px-6 py-16 md:py-20">
          <div className="mx-auto max-w-4xl rounded-3xl border border-white/10 bg-gradient-to-br from-[#8B5CF6] to-indigo-900 p-8 text-center shadow-2xl shadow-[#8B5CF6]/20 md:p-10">
            <h2 className="mb-4 text-2xl font-bold text-white md:text-4xl">{content.finalTitle}</h2>
            <p className="mx-auto mb-6 max-w-2xl text-base text-indigo-200 md:text-lg">{content.finalDescription}</p>

            <form action={`${APP_URL}/auth`} method="get" className="mx-auto flex max-w-md flex-col items-center justify-center gap-3 sm:flex-row">
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
              <div className="flex h-6 w-6 items-center justify-center rounded bg-[#8B5CF6] text-xs font-bold text-white">P</div>
              <span className="font-bold text-white">PVS.ai</span>
            </div>
            <p className="mb-4">{content.footerDescription}</p>
            <div className="flex gap-4">
              <a
                href="https://github.com/Kamil-Zuki/Polyraspad"
                target="_blank"
                rel="noreferrer"
                className="transition hover:text-white"
                aria-label="GitHub repository"
              >
                <BookOpen className="h-5 w-5" />
              </a>
              <a
                href="https://github.com/Kamil-Zuki/Polyraspad/issues"
                target="_blank"
                rel="noreferrer"
                className="transition hover:text-white"
                aria-label="GitHub issues"
              >
                <Bug className="h-5 w-5" />
              </a>
              <a
                href="https://github.com/Kamil-Zuki/Polyraspad#readme"
                target="_blank"
                rel="noreferrer"
                className="transition hover:text-white"
                aria-label="Project README"
              >
                <BookOpen className="h-5 w-5" />
              </a>
            </div>
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
          &copy; 2026 PVS.ai. All rights reserved.
        </div>
      </footer>
    </div>
  )
}
