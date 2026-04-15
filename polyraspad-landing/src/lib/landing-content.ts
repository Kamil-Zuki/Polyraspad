export type LandingLocale = "ru" | "en" | "ko"

export type LandingContent = {
  metadata: {
    title: string
    description: string
  }
  languageLabel: string
  navItems: Array<{ href: string; label: string }>
  loginLabel: string
  startFreeLabel: string
  badge: string
  heroTitle: string
  heroAccent: string
  heroDescription: string
  heroPrimaryCta: string
  heroSecondaryCta: string
  heroSocialProof: string
  heroImageAlt: string
  problemTitle: string
  problemDescription: string
  problemAccent: string
  featuresTitle: string
  featuresSubtitle: string
  features: Array<{ title: string; description: string }>
  marketplaceEyebrow: string
  marketplaceTitle: string
  marketplaceDescription: string
  marketplaceBenefits: Array<{ title: string; description: string }>
  marketplaceCta: string
  marketplaceCardMeta: string
  creatorsTitle: string
  creatorsDescription: string
  creatorBenefits: Array<{ title: string; description: string }>
  creatorsCta: string
  offlineTitle: string
  offlineDescription: string
  pricingTitle: string
  pricingSubtitle: string
  pricingFeaturedBadge: string
  pricingPlans: Array<{
    name: string
    price: string
    suffix?: string
    note?: string
    cta: string
    items: string[]
  }>
  finalTitle: string
  finalDescription: string
  finalPlaceholder: string
  finalButton: string
  footerDescription: string
  footerGroups: Array<{
    title: string
    links: Array<{ href: string; label: string; external?: boolean }>
  }>
}

export const landingContent: Record<LandingLocale, LandingContent> = {
  ru: {
    metadata: {
      title: "PVS.ai",
      description:
        "PVS.ai — сервис для изучения слов и фраз в контексте: проекты, колоды, FSRS-повторения, импорт, маркетплейс и AI-инструменты для работы с карточками.",
    },
    languageLabel: "RU",
    navItems: [
      { href: "#features", label: "Методика" },
      { href: "#marketplace", label: "Библиотека" },
      { href: "#creators", label: "Для авторов" },
      { href: "#pricing", label: "Тарифы" },
    ],
    loginLabel: "Войти",
    startFreeLabel: "Начать бесплатно",
    badge: "Sentence mining, FSRS и инструменты для работы с карточками",
    heroTitle: "Сервис для словаря, который растет из реального языка.",
    heroAccent: "Фразы, проекты, колоды и повторения в одной системе.",
    heroDescription:
      "PVS.ai помогает собирать карточки из фраз, хранить их по проектам и колодам, учить через FSRS и дополнять переводом, грамматическими подсказками и примерами там, где это действительно нужно.",
    heroPrimaryCta: "Создать аккаунт",
    heroSecondaryCta: "Открыть библиотеку",
    heroSocialProof: "Подходит для личных словарей, учебных колод и авторских курсов",
    heroImageAlt: "Интерфейс платформы PVS.ai",
    problemTitle: "Обычные словари и карточки часто расходятся по разным инструментам.",
    problemDescription:
      "Где-то хранится список слов, где-то примеры, где-то импорт, где-то повторения. В итоге материал распадается. PVS.ai собирает эти части в один рабочий поток: от фразы и источника до очереди обучения и статистики.",
    problemAccent: "Главная идея проекта — не «геймификация», а удобная инфраструктура для личного словаря и повторения.",
    featuresTitle: "Что здесь есть по сути",
    featuresSubtitle: "Не общий «language app», а набор конкретных инструментов вокруг карточек и контекста.",
    features: [
      {
        title: "Карточка строится вокруг фразы",
        description:
          'Базовая единица здесь — не отдельное слово, а фраза с целевым словом. Это ближе к sentence mining и лучше подходит для cloze-карточек.',
      },
      {
        title: "Повторения на базе FSRS",
        description:
          "Проект хранит прогресс и интервалы повторения отдельно по проектам и колодам. В основе расписания — FSRS, а не ручные интервалы.",
      },
      {
        title: "AI не вместо системы, а как вспомогательный слой",
        description:
          "AI используется для перевода, пояснения грамматики и генерации примеров. Это полезно в редакторе карточек, но не подменяет саму учебную модель.",
      },
      {
        title: "Есть импорт, reader и захват материала",
        description:
          "Карточки можно собирать из текста, импорта и внешних источников. Для этого в проекте предусмотрены reader-сценарии, import и Capture API.",
      },
    ],
    marketplaceEyebrow: "Публичные колоды и маркетплейс",
    marketplaceTitle: "Библиотека готовых материалов",
    marketplaceDescription:
      "Если не хочется собирать всё вручную, можно взять готовые колоды. Проект поддерживает превью, обновления контента, права доступа и авторские материалы.",
    marketplaceBenefits: [
      {
        title: "Smart Preview",
        description: "Пройдите мини-урок до покупки и сразу оцените качество аудио, перевода и структуры колоды.",
      },
      {
        title: "Синхронизация",
        description: "Получайте правки от авторов без потери прогресса повторений и локальной истории обучения.",
      },
      {
        title: "Коллаборация",
        description: "Нашли ошибку в материале? Предложите исправление и станьте соавтором курса.",
      },
    ],
    marketplaceCta: "Перейти в библиотеку",
    marketplaceCardMeta: "by Elena Teach • 4.9 ★",
    creatorsTitle: "Делаете учебные материалы?\nИх можно публиковать и поддерживать здесь.",
    creatorsDescription:
      "PVS поддерживает не только личные колоды, но и публичные наборы: публикацию, предложения изменений, статистику и коммерческие ограничения для платного контента.",
    creatorBenefits: [
      {
        title: "DRM-защита",
        description: "Платный контент нельзя просто скопировать и выложить в открытый доступ.",
      },
      {
        title: "Аналитика",
        description: "Видно, на каких карточках ученики ошибаются чаще и где материал проседает.",
      },
      {
        title: "Монетизация",
        description: "Платные колоды и курсы тоже предусмотрены, но это часть общей системы публикации, а не отдельный продукт поверх неё.",
      },
    ],
    creatorsCta: "Узнать больше о Creator Studio",
    offlineTitle: "Есть offline-first сценарий для учебных сессий.",
    offlineDescription:
      "Ответы и прогресс можно сохранять локально и синхронизировать позже. Это важно для длинных сессий и мобильного использования.",
    pricingTitle: "Базовые тарифы",
    pricingSubtitle: "Бесплатный режим для личного использования и расширенные функции для AI и авторов.",
    pricingFeaturedBadge: "Самый популярный",
    pricingPlans: [
      {
        name: "Learner",
        price: "Бесплатно",
        cta: "Выбрать",
        items: [
          "Безлимитное создание карточек",
          "Алгоритм FSRS",
          "Офлайн-режим",
          "Доступ к бесплатным колодам",
        ],
      },
      {
        name: "Pro",
        price: "$8",
        suffix: "/ мес",
        note: "Для серьезного погружения",
        cta: "Попробовать 7 дней",
        items: [
          "Все возможности Free",
          "Безлимитный AI-ассистент",
          "Генерация Neural TTS Audio",
          "Продвинутая статистика и heatmaps",
        ],
      },
      {
        name: "Creator",
        price: "% с продаж",
        cta: "Открыть студию",
        items: [
          "Доступ к Creator Studio",
          "Публикация платных курсов",
          "Защита контента",
          "Аналитика вовлеченности",
        ],
      },
    ],
    finalTitle: "Если нужен единый контур для карточек, словаря и повторений — можно начать здесь.",
    finalDescription:
      "Начните с пустого проекта, импортируйте существующие карточки или соберите новый словарь из текста и собственных источников.",
    finalPlaceholder: "Ваш email",
    finalButton: "Создать аккаунт",
    footerDescription: "Сервис для работы с карточками, контекстом, повторениями и публикацией учебных материалов.",
    footerGroups: [
      {
        title: "Продукт",
        links: [
          { href: "#features", label: "Особенности" },
          { href: "/marketplace", label: "Маркетплейс" },
          { href: "#creators", label: "Для преподавателей" },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20REST%20API.md",
            label: "Capture API",
            external: true,
          },
        ],
      },
      {
        title: "Ресурсы",
        links: [
          { href: "https://github.com/Kamil-Zuki/Polyraspad#readme", label: "README проекта", external: true },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D1%81%D0%BD%D0%BE%D0%B2%D0%BD%D1%8B%D0%B5%20%D0%B2%D0%BE%D0%B7%D0%BC%D0%BE%D0%B6%D0%BD%D0%BE%D1%81%D1%82%D0%B8.md",
            label: "База знаний",
            external: true,
          },
          { href: "/import", label: "Импорт из Anki" },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%A0%D0%B5%D0%B2%D0%B8%D0%B7%D0%B8%D1%8F-%D0%BF%D0%B0%D0%B9%D0%BF%D0%BB%D0%B0%D0%B9%D0%BD-%D0%BE%D0%B1%D1%83%D1%87%D0%B5%D0%BD%D0%B8%D1%8F-FSRS.md",
            label: "Алгоритм FSRS",
            external: true,
          },
        ],
      },
      {
        title: "Документация",
        links: [
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20REST%20API.md",
            label: "REST API",
            external: true,
          },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20gRPC.md",
            label: "gRPC",
            external: true,
          },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/polyraspad-frontend-backend-overview.md",
            label: "Архитектура",
            external: true,
          },
          { href: "https://github.com/Kamil-Zuki/Polyraspad/pulls", label: "Pull Requests", external: true },
        ],
      },
    ],
  },
  en: {
    metadata: {
      title: "PVS.ai",
      description:
        "PVS.ai is a service for learning words and phrases in context: projects, decks, FSRS review scheduling, import flows, marketplace, and AI tools around cards.",
    },
    languageLabel: "EN",
    navItems: [
      { href: "#features", label: "Method" },
      { href: "#marketplace", label: "Library" },
      { href: "#creators", label: "For Creators" },
      { href: "#pricing", label: "Pricing" },
    ],
    loginLabel: "Log in",
    startFreeLabel: "Start for free",
    badge: "Sentence mining, FSRS, and tools built around cards",
    heroTitle: "A vocabulary system built from real language.",
    heroAccent: "Phrases, decks, projects, and review in one place.",
    heroDescription:
      "PVS.ai helps you collect cards from phrases, organize them into projects and decks, review them with FSRS, and add translation, grammar help, and examples where they are actually useful.",
    heroPrimaryCta: "Create account",
    heroSecondaryCta: "Open library",
    heroSocialProof: "Useful for personal vocabulary, study decks, and authored courses",
    heroImageAlt: "PVS.ai platform interface",
    problemTitle: "Vocabulary work often gets split across too many tools.",
    problemDescription:
      "One app stores words, another one handles examples, another one imports content, and another one schedules reviews. PVS.ai is meant to connect those steps into one working flow.",
    problemAccent: "The project is less about gamification and more about infrastructure for a personal vocabulary system.",
    featuresTitle: "What the project actually does",
    featuresSubtitle: "Not a generic language app, but a focused system around cards, context, and review.",
    features: [
      {
        title: "Cards are built around phrases",
        description:
          'The core unit here is not an isolated word but a sentence with a target token. That fits sentence mining and works well for cloze-based review.',
      },
      {
        title: "Review scheduling is based on FSRS",
        description:
          "Projects and decks keep their own review state and queue logic. FSRS is used to schedule repetition instead of fixed manual intervals.",
      },
      {
        title: "AI is a support layer, not the product itself",
        description:
          "AI is used for translation, grammar explanation, and example generation. It helps while editing cards, but it does not replace the learning model.",
      },
      {
        title: "Import, reader, and capture flows are part of the system",
        description:
          "Cards can come from text analysis, imports, and external sources. The project includes reader scenarios, import flows, and Capture API for that.",
      },
    ],
    marketplaceEyebrow: "Public decks and marketplace",
    marketplaceTitle: "A library of ready-made materials",
    marketplaceDescription:
      "If you do not want to build everything from scratch, you can start with existing decks. The project supports previews, entitlement handling, content updates, and authored materials.",
    marketplaceBenefits: [
      {
        title: "Smart Preview",
        description: "Try a mini-lesson before purchase and evaluate the audio, translation quality, and deck structure.",
      },
      {
        title: "Sync",
        description: "Receive author fixes without losing your repetition history or local learning progress.",
      },
      {
        title: "Collaboration",
        description: "Found a mistake? Suggest a fix and become a contributor to the course.",
      },
    ],
    marketplaceCta: "Go to library",
    marketplaceCardMeta: "by Elena Teach • 4.9 ★",
    creatorsTitle: "Do you build study materials?\nYou can publish and maintain them here.",
    creatorsDescription:
      "PVS is not only for private decks. It also supports public content, contribution flows, moderation, statistics, and commercial restrictions for paid materials.",
    creatorBenefits: [
      {
        title: "DRM protection",
        description: "Paid content cannot be casually copied and reposted for free.",
      },
      {
        title: "Analytics",
        description: "See where learners make the most mistakes and where the material breaks down.",
      },
      {
        title: "Monetization",
        description: "Paid decks and courses are supported too, but they sit inside the same publication workflow rather than being a separate product.",
      },
    ],
    creatorsCta: "Read more about Creator Studio",
    offlineTitle: "There is an offline-first path for study sessions.",
    offlineDescription:
      "Answers and progress can be stored locally and synced later. That matters for long sessions and mobile use.",
    pricingTitle: "Basic plans",
    pricingSubtitle: "A free layer for personal use and paid features for AI-heavy work and authors.",
    pricingFeaturedBadge: "Most popular",
    pricingPlans: [
      {
        name: "Learner",
        price: "Free",
        cta: "Choose plan",
        items: [
          "Unlimited card creation",
          "FSRS scheduling",
          "Offline mode",
          "Access to free decks",
        ],
      },
      {
        name: "Pro",
        price: "$8",
        suffix: "/ month",
        note: "For serious immersion",
        cta: "Try 7 days",
        items: [
          "Everything in Free",
          "Unlimited AI assistant",
          "Neural TTS audio generation",
          "Advanced stats and heatmaps",
        ],
      },
      {
        name: "Creator",
        price: "% of sales",
        cta: "Open studio",
        items: [
          "Access to Creator Studio",
          "Publish paid courses",
          "Content protection",
          "Engagement analytics",
        ],
      },
    ],
    finalTitle: "If you want one system for cards, vocabulary, and review, this is the entry point.",
    finalDescription:
      "Start with an empty project, import existing cards, or build a new vocabulary workflow from text and your own sources.",
    finalPlaceholder: "Your email",
    finalButton: "Create account",
    footerDescription: "A service for cards, context, repetition, and publishing study materials.",
    footerGroups: [
      {
        title: "Product",
        links: [
          { href: "#features", label: "Features" },
          { href: "/marketplace", label: "Marketplace" },
          { href: "#creators", label: "For teachers" },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20REST%20API.md",
            label: "Capture API",
            external: true,
          },
        ],
      },
      {
        title: "Resources",
        links: [
          { href: "https://github.com/Kamil-Zuki/Polyraspad#readme", label: "Project README", external: true },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D1%81%D0%BD%D0%BE%D0%B2%D0%BD%D1%8B%D0%B5%20%D0%B2%D0%BE%D0%B7%D0%BC%D0%BE%D0%B6%D0%BD%D0%BE%D1%81%D1%82%D0%B8.md",
            label: "Knowledge base",
            external: true,
          },
          { href: "/import", label: "Import from Anki" },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%A0%D0%B5%D0%B2%D0%B8%D0%B7%D0%B8%D1%8F-%D0%BF%D0%B0%D0%B9%D0%BF%D0%BB%D0%B0%D0%B9%D0%BD-%D0%BE%D0%B1%D1%83%D1%87%D0%B5%D0%BD%D0%B8%D1%8F-FSRS.md",
            label: "FSRS algorithm",
            external: true,
          },
        ],
      },
      {
        title: "Docs",
        links: [
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20REST%20API.md",
            label: "REST API",
            external: true,
          },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20gRPC.md",
            label: "gRPC",
            external: true,
          },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/polyraspad-frontend-backend-overview.md",
            label: "Architecture",
            external: true,
          },
          { href: "https://github.com/Kamil-Zuki/Polyraspad/pulls", label: "Pull Requests", external: true },
        ],
      },
    ],
  },
  ko: {
    metadata: {
      title: "PVS.ai",
      description:
        "PVS.ai는 문맥 기반 카드, 프로젝트와 덱, FSRS 복습, 가져오기 흐름, 마켓플레이스, AI 보조 도구를 포함한 어휘 학습 서비스입니다.",
    },
    languageLabel: "KO",
    navItems: [
      { href: "#features", label: "학습 방식" },
      { href: "#marketplace", label: "라이브러리" },
      { href: "#creators", label: "크리에이터용" },
      { href: "#pricing", label: "요금제" },
    ],
    loginLabel: "로그인",
    startFreeLabel: "무료로 시작하기",
    badge: "sentence mining, FSRS, 카드 중심 도구",
    heroTitle: "실제 언어에서 자라는 어휘 시스템.",
    heroAccent: "문장, 덱, 프로젝트, 복습을 한곳에서 관리합니다.",
    heroDescription:
      "PVS.ai는 문장에서 카드를 만들고, 프로젝트와 덱으로 정리하고, FSRS로 복습하고, 필요한 경우 번역·문법 설명·예문 생성까지 이어서 처리할 수 있게 합니다.",
    heroPrimaryCta: "계정 만들기",
    heroSecondaryCta: "라이브러리 열기",
    heroSocialProof: "개인 어휘장, 학습 덱, 제작자용 코스 운영에 맞는 구조",
    heroImageAlt: "PVS.ai 플랫폼 인터페이스",
    problemTitle: "어휘 학습은 보통 너무 많은 도구로 나뉘어 있습니다.",
    problemDescription:
      "단어 저장은 한 곳, 예문은 다른 곳, 가져오기는 또 다른 곳, 복습 스케줄은 또 따로 있는 경우가 많습니다. PVS.ai는 이런 과정을 하나의 흐름으로 묶으려는 프로젝트입니다.",
    problemAccent: "핵심은 화려한 학습 앱이 아니라 개인 어휘 시스템을 위한 인프라입니다.",
    featuresTitle: "이 프로젝트가 실제로 하는 일",
    featuresSubtitle: "일반적인 언어 앱이 아니라 카드, 문맥, 복습에 집중한 시스템입니다.",
    features: [
      {
        title: "카드는 문장을 중심으로 만들어집니다",
        description:
          '여기서 기본 단위는 단어 목록이 아니라 목표 단어가 들어 있는 문장입니다. sentence mining과 cloze 복습에 더 잘 맞는 구조입니다.',
      },
      {
        title: "복습 스케줄은 FSRS 기반입니다",
        description:
          "프로젝트와 덱별로 복습 상태와 대기열이 관리되며, 고정 간격 대신 FSRS를 기준으로 반복 시점을 계산합니다.",
      },
      {
        title: "AI는 보조 기능입니다",
        description:
          "AI는 번역, 문법 설명, 예문 생성에 쓰입니다. 카드 편집과 보강에는 유용하지만, 제품의 핵심 자체를 대체하지는 않습니다.",
      },
      {
        title: "가져오기, reader, capture 흐름이 포함됩니다",
        description:
          "텍스트 분석, 가져오기, 외부 소스에서의 수집을 통해 카드를 만들 수 있습니다. 이를 위해 reader 시나리오, import, Capture API가 포함됩니다.",
      },
    ],
    marketplaceEyebrow: "공개 덱과 마켓플레이스",
    marketplaceTitle: "준비된 학습 자료 라이브러리",
    marketplaceDescription:
      "모든 카드를 처음부터 직접 만들 필요는 없습니다. 미리보기, 접근 권한, 업데이트가 있는 공개 덱과 제작자 자료로 시작할 수 있습니다.",
    marketplaceBenefits: [
      {
        title: "Smart Preview",
        description: "구매 전에 미니 레슨을 체험하고 오디오, 번역 품질, 덱 구성을 바로 확인하세요.",
      },
      {
        title: "동기화",
        description: "작성자의 수정 사항을 받아도 복습 기록과 학습 진행 상태는 유지됩니다.",
      },
      {
        title: "협업",
        description: "오류를 발견했나요? 수정 제안을 보내고 코스의 기여자가 될 수 있습니다.",
      },
    ],
    marketplaceCta: "라이브러리로 이동",
    marketplaceCardMeta: "Elena Teach • 평점 4.9 ★",
    creatorsTitle: "학습 자료를 만드나요?\n여기서 게시하고 관리할 수 있습니다.",
    creatorsDescription:
      "PVS는 개인용 덱만 다루지 않습니다. 공개 콘텐츠, 수정 제안 흐름, 통계, 유료 자료의 제한 정책까지 함께 지원합니다.",
    creatorBenefits: [
      {
        title: "DRM 보호",
        description: "유료 콘텐츠가 쉽게 복사되어 무료로 재배포되지 않도록 보호합니다.",
      },
      {
        title: "분석",
        description: "학습자가 어디서 가장 많이 틀리는지, 어떤 부분이 약한지 확인할 수 있습니다.",
      },
      {
        title: "수익화",
        description: "유료 덱과 코스도 지원하지만, 별도 상품이 아니라 같은 게시 시스템 안에서 동작합니다.",
      },
    ],
    creatorsCta: "Creator Studio 더 보기",
    offlineTitle: "학습 세션은 offline-first 흐름을 지원합니다.",
    offlineDescription:
      "답변과 진행 상태를 로컬에 저장하고 나중에 동기화할 수 있습니다. 긴 학습 세션이나 모바일 사용에 중요합니다.",
    pricingTitle: "기본 요금제",
    pricingSubtitle: "개인 사용을 위한 무료 레이어와, AI 및 제작자 기능을 위한 유료 옵션입니다.",
    pricingFeaturedBadge: "가장 인기 있는 플랜",
    pricingPlans: [
      {
        name: "Learner",
        price: "무료",
        cta: "선택하기",
        items: [
          "무제한 카드 생성",
          "FSRS 스케줄링",
          "오프라인 모드",
          "무료 덱 이용",
        ],
      },
      {
        name: "Pro",
        price: "$8",
        suffix: "/ 월",
        note: "깊이 있는 몰입 학습용",
        cta: "7일 체험하기",
        items: [
          "Free의 모든 기능",
          "무제한 AI 어시스턴트",
          "Neural TTS 오디오 생성",
          "고급 통계와 히트맵",
        ],
      },
      {
        name: "Creator",
        price: "매출의 %",
        cta: "스튜디오 열기",
        items: [
          "Creator Studio 이용",
          "유료 코스 게시",
          "콘텐츠 보호",
          "참여도 분석",
        ],
      },
    ],
    finalTitle: "카드, 어휘장, 복습을 한 시스템에서 관리하고 싶다면 여기서 시작할 수 있습니다.",
    finalDescription:
      "빈 프로젝트에서 시작하거나, 기존 카드를 가져오거나, 텍스트와 자신의 자료에서 새 어휘 흐름을 만들 수 있습니다.",
    finalPlaceholder: "이메일 주소",
    finalButton: "계정 만들기",
    footerDescription: "카드, 문맥, 반복 학습, 학습 자료 게시를 위한 서비스입니다.",
    footerGroups: [
      {
        title: "제품",
        links: [
          { href: "#features", label: "기능" },
          { href: "/marketplace", label: "마켓플레이스" },
          { href: "#creators", label: "교사용" },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20REST%20API.md",
            label: "Capture API",
            external: true,
          },
        ],
      },
      {
        title: "리소스",
        links: [
          { href: "https://github.com/Kamil-Zuki/Polyraspad#readme", label: "프로젝트 README", external: true },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D1%81%D0%BD%D0%BE%D0%B2%D0%BD%D1%8B%D0%B5%20%D0%B2%D0%BE%D0%B7%D0%BC%D0%BE%D0%B6%D0%BD%D0%BE%D1%81%D1%82%D0%B8.md",
            label: "지식 베이스",
            external: true,
          },
          { href: "/import", label: "Anki에서 가져오기" },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%A0%D0%B5%D0%B2%D0%B8%D0%B7%D0%B8%D1%8F-%D0%BF%D0%B0%D0%B9%D0%BF%D0%BB%D0%B0%D0%B9%D0%BD-%D0%BE%D0%B1%D1%83%D1%87%D0%B5%D0%BD%D0%B8%D1%8F-FSRS.md",
            label: "FSRS 알고리즘",
            external: true,
          },
        ],
      },
      {
        title: "문서",
        links: [
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20REST%20API.md",
            label: "REST API",
            external: true,
          },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5%20gRPC.md",
            label: "gRPC",
            external: true,
          },
          {
            href: "https://github.com/Kamil-Zuki/Polyraspad/blob/main/Docs/polyraspad-frontend-backend-overview.md",
            label: "아키텍처",
            external: true,
          },
          { href: "https://github.com/Kamil-Zuki/Polyraspad/pulls", label: "Pull Requests", external: true },
        ],
      },
    ],
  },
}
