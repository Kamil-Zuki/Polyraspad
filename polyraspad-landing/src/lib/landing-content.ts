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
  heroSocialProof: string
  heroImageAlt: string
  problemTitle: string
  problemDescription: string
  problemAccent: string
  featuresTitle: string
  featuresSubtitle: string
  features: Array<{ title: string; description: string }>
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
      title: "Polyraspad",
      description:
        "Polyraspad — платформа для изучения языков через чтение: ридер, sentence mining, FSRS-повторения и расширение для браузера.",
    },
    languageLabel: "RU",
    navItems: [
      { href: "#features", label: "Методика" },
    ],
    loginLabel: "Войти",
    startFreeLabel: "Начать бесплатно",
    badge: "LingQ-style reader, sentence mining и FSRS",
    heroTitle: "Изучение языков через чтение и контекст.",
    heroAccent: "Ридер, колоды, проекты и интервальные повторения.",
    heroDescription:
      "Polyraspad помогает читать тексты, собирать словарный запас из реального контекста и эффективно повторять слова с помощью алгоритма FSRS.",
    heroPrimaryCta: "Начать использование",
    heroSocialProof: "Подходит для личных словарей и интенсивного погружения в язык",
    heroImageAlt: "Интерфейс платформы Polyraspad",
    problemTitle: "Словарь, ридер и повторения часто существуют в разных приложениях.",
    problemDescription:
      "Обычно вы читаете в одном месте, ищете перевод в другом, а повторяете карточки в Anki. Polyraspad объединяет эти процессы: от чтения текста и захвата субтитров до генерации аудио и повторений.",
    problemAccent: "Глобальная цель проекта — комплексное развитие 4 навыков языка (чтение, аудирование, письмо, речь). Текущая MVP-версия закладывает для этого фундамент — строгую инфраструктуру для sentence mining.",
    featuresTitle: "Архитектура платформы",
    featuresSubtitle: "Набор инструментов для эффективного погружения в язык.",
    features: [
      {
        title: "LingQ-style Reader",
        description:
          'Читайте тексты, выделяйте неизвестные слова и фразы. Система отслеживает ваш словарный запас и подсвечивает новые термины.',
      },
      {
        title: "Повторения на базе FSRS",
        description:
          "В основе расписания повторений лежит современный алгоритм FSRS, обеспечивающий точное вычисление интервалов памяти.",
      },
      {
        title: "Browser Capture Extension",
        description:
          "Собирайте материал прямо из браузера: захват субтитров, аудио и текста для быстрого создания карточек.",
      },
    ],
    pricingTitle: "Варианты использования (MVP)",
    pricingSubtitle: "Проект находится на стадии MVP.",
    pricingFeaturedBadge: "Cloud",
    pricingPlans: [
      {
        name: "Cloud MVP",
        price: "800 ₽",
        suffix: "/ мес",
        note: "Управляемая облачная версия",
        cta: "Создать аккаунт",
        items: [
          "Не нужно настраивать сервер",
          "Базовый перевод фразы в контексте",
          "Синхронизация прогресса FSRS",
          "Резервное копирование",
        ],
      },
    ],
    finalTitle: "Единый контур для карточек, словаря и контекстного чтения.",
    finalDescription:
      "Начните с пустого проекта, импортируйте существующие карточки из Anki или соберите новый словарь из любимых текстов.",
    finalPlaceholder: "Ваш email",
    finalButton: "Зарегистрироваться",
    footerDescription: "Платформа для работы с текстами, контекстным словарем и FSRS повторениями.",
    footerGroups: [
      {
        title: "Продукт",
        links: [
          { href: "#features", label: "Возможности" },
        ],
      },
      {
        title: "Ресурсы",
        links: [
          { href: "/import", label: "Импорт из Anki" },
          {
            href: "https://github.com/open-spaced-repetition/fsrs4anki",
            label: "О FSRS",
            external: true,
          },
        ],
      },

    ],
  },
  en: {
    metadata: {
      title: "Polyraspad",
      description:
        "Polyraspad is a language learning platform featuring a reader, sentence mining, FSRS review scheduling, and a browser extension.",
    },
    languageLabel: "EN",
    navItems: [
      { href: "#features", label: "Features" },
    ],
    loginLabel: "Log in",
    startFreeLabel: "Start for free",
    badge: "LingQ-style reader, sentence mining, and FSRS",
    heroTitle: "Learn languages through reading and context.",
    heroAccent: "Reader, decks, projects, and spaced repetition.",
    heroDescription:
      "Polyraspad helps you read texts, mine vocabulary from real context, and review words effectively using the FSRS algorithm.",
    heroPrimaryCta: "Get started",
    heroSocialProof: "Built for personal vocabulary building and deep language immersion",
    heroImageAlt: "Polyraspad platform interface",
    problemTitle: "Reading, mining, and reviewing often happen in different apps.",
    problemDescription:
      "Usually, you read in one place, look up translations in another, and review cards in Anki. Polyraspad connects these flows: from reading and capturing subtitles to generating audio and reviewing.",
    problemAccent: "The global vision is the comprehensive development of 4 language skills (reading, listening, writing, speaking). The current MVP lays the foundation: a strict infrastructure for sentence mining.",
    featuresTitle: "Platform Architecture",
    featuresSubtitle: "A set of tools for effective language immersion.",
    features: [
      {
        title: "LingQ-style Reader",
        description:
          'Read texts, highlight unknown words and phrases. The system tracks your vocabulary and highlights new terms automatically.',
      },
      {
        title: "FSRS-based Reviews",
        description:
          "Review scheduling is powered by the modern FSRS algorithm, providing accurate memory interval calculations.",
      },
      {
        title: "Browser Capture Extension",
        description:
          "Collect material directly from your browser: capture subtitles, audio, and text to quickly create flashcards.",
      },
    ],
    pricingTitle: "Usage Options (MVP)",
    pricingSubtitle: "The project is currently in the MVP stage.",
    pricingFeaturedBadge: "Cloud",
    pricingPlans: [
      {
        name: "Cloud MVP",
        price: "$8",
        suffix: "/ month",
        note: "Managed cloud version",
        cta: "Create account",
        items: [
          "No server setup required",
          "Basic contextual translation",
          "FSRS progress sync",
          "Automated backups",
        ],
      },
    ],
    finalTitle: "A single workflow for cards, vocabulary, and contextual reading.",
    finalDescription:
      "Start with an empty project, import existing Anki cards, or build a new vocabulary from your favorite texts.",
    finalPlaceholder: "Your email",
    finalButton: "Sign up",
    footerDescription: "A platform for texts, contextual vocabulary, and FSRS spaced repetition.",
    footerGroups: [
      {
        title: "Product",
        links: [
          { href: "#features", label: "Features" },
        ],
      },
      {
        title: "Resources",
        links: [
          { href: "/import", label: "Import from Anki" },
          {
            href: "https://github.com/open-spaced-repetition/fsrs4anki",
            label: "About FSRS",
            external: true,
          },
        ],
      },

    ],
  },
  ko: {
    metadata: {
      title: "Polyraspad",
      description:
        "Polyraspad는 리더, 문장 마이닝, FSRS 복습 일정 및 브라우저 확장 기능을 제공하는 어휘 학습 플랫폼입니다.",
    },
    languageLabel: "KO",
    navItems: [
      { href: "#features", label: "학습 방식" },
    ],
    loginLabel: "로그인",
    startFreeLabel: "무료로 시작하기",
    badge: "LingQ-style 리더, 문장 마이닝 및 FSRS",
    heroTitle: "읽기와 문맥을 통한 언어 학습.",
    heroAccent: "리더, 덱, 프로젝트 및 간격 반복.",
    heroDescription:
      "Polyraspad는 텍스트를 읽고, 실제 문맥에서 어휘를 수집하며, FSRS 알고리즘을 사용하여 단어를 효과적으로 복습하도록 돕습니다.",
    heroPrimaryCta: "시작하기",
    heroSocialProof: "개인 어휘 구축 및 깊이 있는 언어 몰입을 위한 구조",
    heroImageAlt: "Polyraspad 플랫폼 인터페이스",
    problemTitle: "읽기, 어휘 수집, 복습은 종종 다른 앱에서 일어납니다.",
    problemDescription:
      "보통 한 곳에서 읽고, 다른 곳에서 번역을 찾고, Anki에서 카드를 복습합니다. Polyraspad는 텍스트 읽기와 자막 캡처부터 오디오 생성 및 복습까지 이 흐름을 하나로 연결합니다.",
    problemAccent: "글로벌 비전은 4가지 언어 능력(읽기, 듣기, 쓰기, 말하기)의 종합적인 개발입니다. 현재 MVP는 그 토대인 문장 마이닝을 위한 엄격한 인프라를 제공합니다.",
    featuresTitle: "플랫폼 아키텍처",
    featuresSubtitle: "효과적인 언어 몰입을 위한 도구 세트.",
    features: [
      {
        title: "LingQ-style 리더",
        description:
          '텍스트를 읽고 모르는 단어와 문장을 강조 표시하세요. 시스템이 어휘력을 추적하고 새로운 단어를 자동으로 강조합니다.',
      },
      {
        title: "FSRS 기반 복습",
        description:
          "복습 일정은 최신 FSRS 알고리즘으로 구동되어 정확한 기억 간격 계산을 제공합니다.",
      },
      {
        title: "브라우저 캡처 확장",
        description:
          "브라우저에서 직접 자료를 수집하세요. 자막, 오디오, 텍스트를 캡처하여 플래시카드를 빠르게 만듭니다.",
      },
    ],
    pricingTitle: "사용 옵션 (MVP)",
    pricingSubtitle: "현재 MVP 단계입니다.",
    pricingFeaturedBadge: "Cloud",
    pricingPlans: [
      {
        name: "Cloud MVP",
        price: "$8",
        suffix: "/ 월",
        note: "관리형 클라우드 버전",
        cta: "계정 만들기",
        items: [
          "서버 설정 필요 없음",
          "기본 문맥 번역",
          "FSRS 진행률 동기화",
          "자동 백업",
        ],
      },
    ],
    finalTitle: "카드, 어휘, 문맥 읽기를 위한 단일 워크플로.",
    finalDescription:
      "빈 프로젝트로 시작하거나, 기존 Anki 카드를 가져오거나, 좋아하는 텍스트로 새 어휘를 구축하세요.",
    finalPlaceholder: "이메일 주소",
    finalButton: "가입하기",
    footerDescription: "텍스트, 문맥 어휘 및 FSRS 간격 반복을 위한 플랫폼입니다.",
    footerGroups: [
      {
        title: "제품",
        links: [
          { href: "#features", label: "기능" },
        ],
      },
      {
        title: "리소스",
        links: [
          { href: "/import", label: "Anki에서 가져오기" },
          {
            href: "https://github.com/open-spaced-repetition/fsrs4anki",
            label: "FSRS 소개",
            external: true,
          },
        ],
      },

    ],
  },
}
