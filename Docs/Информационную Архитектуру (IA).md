---
title: "IA: архитектура и навигация (PVS)"
aliases: ["Information Architecture", "IA PVS"]
tags: [polyraspad, docs, ia, ux, navigation]
version: 2.0
---

> Исходный ввод: консолидация обсуждений по навигации и поддержке медиа (как в Anki), прежде чем утверждать визуальный слой (цвета, шрифты).

# **Дизайн-система PVS: Часть 1\. Архитектура и Навигация**

**Версия:** 2.0 (Updated)  
**Концепция:** "Leonardo.ai for Languages" — темный, иммерсивный интерфейс профессионального инструмента.

---

## **1\. Структура навигации (Sitemap)**

Приложение разделено на два логических уровня:

### **Уровень 1: Глобальный (Launcher)**

*Экран входа и выбора контекста. Здесь нет сайдбара.*

1. **/projects (Projects Hub)** — Стартовая страница.  
   * Сетка карточек проектов (языков).  
   * Кнопка «Создать новый проект».  
   * Сводная статистика профиля (общий словарный запас).  
2.   
3. **/profile (Global Settings)** — Настройки аккаунта, биллинг, смена пароля.

---

### **Уровень 2: Рабочее пространство (Workspace)**

*Основной интерфейс. Слева всегда зафиксирован **Sidebar** (кроме режима обучения).*

#### **Секция A: Learning (Обучение)**

1. **/dashboard (Home)** — Командный центр.  
   * Виджеты: Серия (Streak), Дневной прогресс (Кольца), Heatmap.  
   * Hero-баннер: Кнопка «Продолжить обучение» (Study Now).  
   * Сетка последних активных колод.  
2.   
3. **/library (Library)** — Управление контентом.  
   * Древовидная структура (Папки / Колоды).  
   * Фильтры (Мои / Скачанные / Публичные).  
   * Действия: Создать папку, Настройки колоды.  
4.   
5. **/browser (Card Browser)** — Таблица всех карточек.  
   * Поиск, массовые операции, теги.  
6. 

#### **Секция B: Studio (Создание)**

1. **/editor (Card Editor)** — Конструктор карточек (Anki-style \+ AI).  
   * Ручной ввод (Текст \+ Медиа).  
   * AI-помощник (сворачиваемая панель).  
2.   
3. **/reader (Reader)** — Читалка текстов.  
   * Анализ текста, подсветка статусов слов, Click-to-Mine.  
4.   
5. **/import (Import)** — Массовая загрузка (CSV, Anki).

#### **Секция C: Community (Маркетплейс)**

1. **/marketplace (Marketplace)** — Витрина курсов.  
   * Сетка товаров, умные фильтры.  
2.   
3. **/marketplace/product/:id (Product Page)** — Страница товара.  
   * Детали, отзывы, демо-режим.  
4. 

#### **Секция D: Focus Mode (Без интерфейса)**

1. **/study (Session)** — Режим урока.  
   * Интерфейс на весь экран, сайдбар скрыт.  
   * Карточка (Вопрос/Ответ), SRS-кнопки.  
2. 

---

## **2\. Детализация ключевых экранов**

### **Экран: Card Editor (Редактор карточек)**

Это главный инструмент для пополнения базы (аналог кнопки "Add" в Anki, но современнее).

**Структура экрана:**

* **Header:** Выбор колоды (Dropdown), Кнопка «Сохранить» (Ctrl+S).  
* **Левая колонка (Основная форма):**  
  * **Sentence (Фраза):** Текстовое поле. При выделении слова мышкой оно автоматически становится Target.  
  * **Target (Цель):** Поле ввода (автозаполняется при выделении).  
  * **Translation (Перевод):** Поле ввода. Рядом кнопка «AI Translate» (волшебная палочка).  
  * **Media Section (Медиа):**  
    * **Image Zone:** Drag-and-drop область. Поддержка вставки из буфера обмена (Ctrl+V).  
    * **Audio Zone:**  
      * Кнопка «Загрузить файл».  
      * Кнопка «Записать микрофон».  
      * Чекбокс «Auto TTS» (Генерировать роботом, если файла нет).  
    *   
  *   
  * **Notes:** Дополнительные заметки.  
*   
* **Правая колонка (AI Assistant):**  
  * *Состояние:* Сворачиваемая (Collapsible).  
  * *Инструменты:*  
    * «Сгенерировать примеры» (если пользователь ввел только слово).  
    * «Объяснить грамматику».  
    * «Найти картинку» (поиск в Unsplash/Google).  
  *   
* 

---

### **Экран: Sidebar (Боковое меню)**

Навигационный хаб в стиле Leonardo.ai.

1. **Логотип.**  
2. **Project Switcher:** Выпадающее меню с флагом текущего языка.  
3. **Streak Widget:** Яркий элемент с анимацией огня и прогресс-баром дня.  
4. **Группа LEARNING:** Home, Library, Browser.  
   * *Study Button:* Вынесена отдельно или ярко выделена в этой группе.  
5.   
6. **Группа STUDIO:** Editor (Create), Reader, Import.  
7. **Группа COMMUNITY:** Marketplace, Subscriptions.  
8. **Footer:** Аватар пользователя, кнопка Settings.

---

## **3\. User Flows (Сценарии использования)**

### **Сценарий 1: Ручной майнинг (Manual Mining)**

*Для пользователей, переносящих контент из книг или Anki вручную.*

1. Пользователь нажимает **"+" (Create)** в сайдбаре.  
2. Открывается **/editor**.  
3. **Ввод:**  
   * Вставляет фразу: *"The engine failed."*  
   * Кликает на *"failed"* (система помечает его как Target).  
   * Пишет перевод: *"Двигатель отказал."*  
4.   
5. **Медиа (Опционально):**  
   * Нажимает кнопку микрофона 🎙️, произносит фразу, нажимает стоп. Аудио прикрепилось.  
   * Вставляет картинку из буфера обмена (Ctrl+V). Картинка прикрепилась.  
6.   
7. **Сохранение:** Нажимает Ctrl+Enter.  
8. Система показывает тост "Saved\!", очищает поля, фокус возвращается в поле Sentence.

### **Сценарий 2: AI-Assisted Майнинг**

*Когда лень придумывать контекст.*

1. Открывает **/editor**.  
2. Вводит в поле Target слово: *"Inevitably"*.  
3. Разворачивает правую панель **AI Assistant**.  
4. Нажимает **"Generate Context"**.  
5. AI предлагает 3 варианта. Пользователь кликает на *"Death comes inevitably"*.  
6. Поля Sentence и Translation заполняются автоматически.  
7. Сохраняет.

### **Сценарий 3: Ежедневное обучение**

1. Заходит на **/dashboard**.  
2. Видит виджет: "150 cards due".  
3. Нажимает большую кнопку **Play**.  
4. Интерфейс меняется на **Study Mode** (без отвлекающих факторов).  
5. Проходит очередь.  
6. В конце видит экран "Summary" с обновленным Стриком.

---

**Все ли верно в этой структуре?** Если да, то теперь мы можем переходить к **Части 2: Визуальный стиль** (определение цветов, типографики и компонентов), чтобы я мог начать генерировать HTML-код с учетом этих требований.

Прошу прощения за сбой в форматировании. Исправляю таблицу и привожу полный, аккуратный документ **Этапа 2**.

---

# **Дизайн-система PVS: Визуальный стиль**

**Концепция:** «Deep Immersion» (Глубокое погружение).  
**Референс:** Leonardo.ai / Linear.app.  
**Атмосфера:** Профессиональный, технологичный, темный интерфейс.

---

## **1\. Цветовая палитра (Color Palette)**

Мы не используем чистый черный (\#000000). База — глубокие сине-серые оттенки (Navy/Slate), которые создают ощущение глубины.

### **A. Backgrounds (Фоны)**

| Роль | HEX | Описание | Tailwind Class |
| :---- | :---- | :---- | :---- |
| **App Background** | \#0B0F15 | Самый глубокий слой. Основной фон приложения. | bg-\[\#0B0F15\] |
| **Surface (Card)** | \#131927 | Фон карточек, сайдбара, панелей. | bg-\[\#131927\] |
| **Surface Hover** | \#1C2438 | Цвет панели при наведении курсора. | hover:bg-\[\#1C2438\] |
| **Border** | \#FFFFFF (8%) | Тонкие разделители. | border-white/10 |

### **B. Brand Accents (Бренд и Акценты)**

Мы используем градиенты для придания интерфейсу «энергии» и свечения.

| Роль | HEX / Значение | Описание | Tailwind Class |
| :---- | :---- | :---- | :---- |
| **Primary** | \#8B5CF6 | Насыщенный фиолетовый. Основной акцент. | text-violet-500 / bg-violet-500 |
| **Secondary** | \#3B82F6 | Яркий синий. Для вторичных действий. | text-blue-500 / bg-blue-500 |
| **Gradient** | Violet → Blue | Для главных кнопок и логотипа. | bg-gradient-to-r from-violet-600 to-blue-600 |
| **Glow** | Violet (30%) | Цвет неонового свечения (тень). | shadow-\[0\_0\_20px\_rgba(139,92,246,0.3)\] |

### **C. Semantic Colors (SRS / Статусы)**

Цвета для кнопок оценки (FSRS) и статусов слов. Они адаптированы для темного фона (более светлые и мягкие, чем стандартные).

| Статус | Цвет | Значение | Tailwind Class |
| :---- | :---- | :---- | :---- |
| **Again** | **Rose** | Забыл / Ошибка / Удаление. | text-rose-400, bg-rose-500/10 |
| **Hard** | **Amber** | Сложно / Внимание. | text-amber-400, bg-amber-500/10 |
| **Good** | **Emerald** | Хорошо / Выучено / Успех. | text-emerald-400, bg-emerald-500/10 |
| **Easy** | **Cyan** | Легко / Новое слово. | text-cyan-400, bg-cyan-500/10 |

### **D. Text (Типографика)**

| Роль | HEX | Описание | Tailwind Class |
| :---- | :---- | :---- | :---- |
| **Primary Text** | \#F3F4F6 | Заголовки, основной контент. (Почти белый) | text-gray-100 |
| **Secondary** | \#9CA3AF | Описания, подписи. (Серый) | text-gray-400 |
| **Disabled** | \#4B5563 | Неактивные элементы. (Темно-серый) | text-gray-600 |

---

## **2\. Базовые компоненты (Atoms)**

Эти элементы будут использоваться на всех страницах.

### **1\. Glass Panel (Стеклянная панель)**

Используется для карточек, списков, модальных окон.

* **Стиль:** Полупрозрачность \+ Размытие (Blur) \+ Тонкая обводка.

**CSS:**  
 code CSS  
downloadcontent\_copy  
expand\_less  
background: rgba(19, 25, 39, 0.7); /\* Surface с прозрачностью \*/  
backdrop-filter: blur(12px);  
border: 1px solid rgba(255, 255, 255, 0.08);

* border-radius: 16px;

### **2\. Buttons (Кнопки)**

* **Primary (CTA):** Градиентный фон \+ Свечение (Glow). Текст белый, жирный.  
* **Secondary (Ghost):** Прозрачный фон, белая обводка (10%). При наведении фон становится белым (5%).

### **3\. Inputs (Поля ввода)**

В стиле Leonardo поля ввода должны выглядеть «утопленными» в интерфейс.

* **Стиль:** Темнее фона (Deep Space), без видимых границ в спокойном состоянии. При фокусе — фиолетовая обводка.

**CSS:**  
 code CSS  
downloadcontent\_copy  
expand\_less  
background: \#0B0F15;  
border: 1px solid rgba(255,255,255, 0.05);  
color: white;

* border-radius: 10px;

---

## **3\. Глобальный Layout (Сетка)**

Фиксированная структура для всех страниц внутри Workspace.

code Code  
downloadcontent\_copy  
expand\_less  
\+----------------------------------------------------+  
| SIDEBAR (W: 260px) |  HEADER (H: 64px)             |  
|                    |  \- Хлебные крошки             |  
| \- Логотип          |  \- Поиск                      |  
| \- Выбор проекта    |  \- Профиль                    |  
| \- Навигация        |-------------------------------|  
| \- Виджет Стрика    |  MAIN CONTENT (Scrollable)    |  
| \- Профиль          |                               |  
|                    |  \- Заголовок страницы         |  
|                    |  \- Контент                    |  
|                    |                               |  
\+--------------------+-------------------------------+  
---

Теперь, когда стили зафиксированы, мы можем переходить к **Этапу 3: Верстка компонентов и Страниц**.

С чего начнем? С **Главного Лейаута (Sidebar \+ Header)**, чтобы у нас была "рамка" для всего остального?

Отлично. Приступаем к **Этапу 3: Верстка**.

Мы начинаем с 

## **Глобального Лейаута (Main Layout)**. 

Это «скелет» нашего приложения, который будет неизменным на всех страницах (кроме режима обучения). Он включает в себя **Sidebar** (Сайдбар) и **Header** (Верхнюю панель).

Я написал код, строго следуя нашей Дизайн-системе (цвета \#0B0F15, \#131927, градиенты, шрифты).

### **Инструкция:**

1. Создай файл layout.html.  
2. Вставь туда код ниже.  
3. Открой через **Live Server**.

Этот файл станет твоей основой. В будущем мы будем просто менять содержимое тега \<main\>, оставляя рамку неизменной.

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Main Layout\</title\>  
      
    \<\!-- 1\. Подключаем Tailwind \--\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
      
    \<\!-- 2\. Подключаем Иконки (FontAwesome) \--\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
      
    \<\!-- 3\. Настраиваем Дизайн-систему (Цвета и Шрифты) \--\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: {  
                            bg: '\#0B0F15',      /\* Deep Space \*/  
                            surface: '\#131927', /\* Card/Sidebar Surface \*/  
                            hover: '\#1C2438',   /\* Hover State \*/  
                            border: 'rgba(255, 255, 255, 0.08)', /\* Divider \*/  
                        },  
                        brand: {  
                            primary: '\#8B5CF6',   /\* Violet \*/  
                            secondary: '\#3B82F6', /\* Blue \*/  
                            pink: '\#EC4899',  
                        },  
                        status: {  
                            success: '\#10B981',  
                            warning: '\#F59E0B',  
                            error: '\#EF4444',  
                            info: '\#06B6D4'  
                        }  
                    },  
                    fontFamily: {  
                        sans: \['Inter', 'system-ui', 'sans-serif'\],  
                    },  
                    boxShadow: {  
                        'glow': '0 0 20px rgba(139, 92, 246, 0.15)',  
                    }  
                }  
            }  
        }  
    \</script\>

    \<\!-- 4\. Кастомные стили \--\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');

        /\* Тонкий скроллбар \*/  
        ::-webkit-scrollbar { width: 6px; height: 6px; }  
        ::-webkit-scrollbar-track { background: \#0B0F15; }  
        ::-webkit-scrollbar-thumb { background: \#1C2438; border-radius: 3px; }  
        ::-webkit-scrollbar-thumb:hover { background: \#8B5CF6; }

        /\* Эффект стекла \*/  
        .glass {  
            background: rgba(19, 25, 39, 0.7);  
            backdrop-filter: blur(12px);  
            \-webkit-backdrop-filter: blur(12px);  
        }

        /\* Градиентный текст \*/  
        .text-gradient {  
            background: linear-gradient(to right, \#8B5CF6, \#3B82F6);  
            \-webkit-background-clip: text;  
            \-webkit-text-fill-color: transparent;  
        }

        /\* Активная навигация \*/  
        .nav-item.active {  
            background: linear-gradient(90deg, rgba(139, 92, 246, 0.1) 0%, transparent 100%);  
            border-left: 3px solid \#8B5CF6;  
            color: \#F3F4F6;  
        }  
        .nav-item.active i { color: \#8B5CF6; }  
    \</style\>  
\</head\>

\<\!-- BODY: Глубокий фон, светлый текст \--\>  
\<body class="bg-app-bg text-gray-400 font-sans h-screen flex overflow-hidden selection:bg-brand-primary selection:text-white"\>

    \<\!-- \==================== SIDEBAR (260px) \==================== \--\>  
    \<aside class="w-\[260px\] bg-app-surface border-r border-app-border flex flex-col flex-shrink-0 z-30"\>  
          
        \<\!-- Logo \--\>  
        \<div class="h-16 flex items-center px-6 border-b border-app-border"\>  
            \<div class="w-8 h-8 rounded-lg bg-gradient-to-br from-brand-primary to-brand-secondary flex items-center justify-center text-white font-bold text-lg mr-3 shadow-glow"\>  
                P  
            \</div\>  
            \<span class="text-white font-bold text-lg tracking-tight"\>PVS.ai\</span\>  
        \</div\>

        \<\!-- Scrollable Content \--\>  
        \<div class="flex-1 overflow-y-auto py-6 px-3 space-y-8"\>  
              
            \<\!-- Project Switcher \--\>  
            \<div class="px-2"\>  
                \<button class="w-full bg-app-bg hover:bg-app-hover border border-app-border rounded-xl p-3 flex items-center justify-between group transition-all duration-200"\>  
                    \<div class="flex items-center gap-3"\>  
                        \<div class="w-8 h-8 rounded bg-indigo-900/40 flex items-center justify-center text-lg border border-white/5"\>  
                            🇬🇧  
                        \</div\>  
                        \<div class="text-left"\>  
                            \<div class="text-\[10px\] uppercase font-bold text-gray-500 group-hover:text-brand-primary transition-colors"\>Current Project\</div\>  
                            \<div class="text-sm font-bold text-gray-100"\>English C1\</div\>  
                        \</div\>  
                    \</div\>  
                    \<i class="fas fa-chevron-down text-xs"\>\</i\>  
                \</button\>  
            \</div\>

            \<\!-- Navigation Groups \--\>  
            \<nav class="space-y-1"\>  
                \<div class="px-4 mb-2 text-\[10px\] font-bold text-gray-500 uppercase tracking-wider"\>Learning\</div\>  
                  
                \<\!-- Active Item Example \--\>  
                \<a href="\#" class="nav-item active flex items-center gap-3 px-4 py-2.5 rounded-r-lg transition-all duration-200"\>  
                    \<i class="fas fa-home w-5 text-center"\>\</i\>  
                    \<span class="text-sm font-medium"\>Dashboard\</span\>  
                \</a\>

                \<a href="\#" class="nav-item flex items-center gap-3 px-4 py-2.5 rounded-r-lg hover:text-gray-100 hover:bg-white/5 transition-all duration-200 group"\>  
                    \<i class="fas fa-layer-group w-5 text-center group-hover:text-brand-secondary transition-colors"\>\</i\>  
                    \<span class="text-sm font-medium"\>Library\</span\>  
                \</a\>

                \<a href="\#" class="nav-item flex items-center gap-3 px-4 py-2.5 rounded-r-lg hover:text-gray-100 hover:bg-white/5 transition-all duration-200 group"\>  
                    \<i class="fas fa-search w-5 text-center group-hover:text-white transition-colors"\>\</i\>  
                    \<span class="text-sm font-medium"\>Browser\</span\>  
                \</a\>  
            \</nav\>

            \<nav class="space-y-1"\>  
                \<div class="px-4 mb-2 text-\[10px\] font-bold text-gray-500 uppercase tracking-wider"\>Studio\</div\>  
                  
                \<a href="\#" class="nav-item flex items-center gap-3 px-4 py-2.5 rounded-r-lg hover:text-gray-100 hover:bg-white/5 transition-all duration-200 group"\>  
                    \<i class="fas fa-plus-circle w-5 text-center group-hover:text-brand-primary transition-colors"\>\</i\>  
                    \<span class="text-sm font-medium"\>Create Card\</span\>  
                \</a\>  
                \<a href="\#" class="nav-item flex items-center gap-3 px-4 py-2.5 rounded-r-lg hover:text-gray-100 hover:bg-white/5 transition-all duration-200 group"\>  
                    \<i class="fas fa-book-reader w-5 text-center group-hover:text-brand-pink transition-colors"\>\</i\>  
                    \<span class="text-sm font-medium"\>Reader\</span\>  
                \</a\>  
            \</nav\>

            \<nav class="space-y-1"\>  
                \<div class="px-4 mb-2 text-\[10px\] font-bold text-gray-500 uppercase tracking-wider"\>Community\</div\>  
                  
                \<a href="\#" class="nav-item flex items-center gap-3 px-4 py-2.5 rounded-r-lg hover:text-gray-100 hover:bg-white/5 transition-all duration-200 group"\>  
                    \<i class="fas fa-store w-5 text-center group-hover:text-yellow-400 transition-colors"\>\</i\>  
                    \<span class="text-sm font-medium"\>Marketplace\</span\>  
                \</a\>  
            \</nav\>

        \</div\>

        \<\!-- Streak Widget (Sticky Bottom) \--\>  
        \<div class="p-4 border-t border-app-border bg-app-surface/50 backdrop-blur-sm"\>  
             \<div class="rounded-xl bg-gradient-to-r from-brand-primary/10 to-brand-secondary/10 border border-brand-primary/20 p-3 relative overflow-hidden group"\>  
                \<\!-- Glow Effect \--\>  
                \<div class="absolute \-right-4 \-top-4 w-12 h-12 bg-brand-primary/20 blur-xl rounded-full group-hover:bg-brand-primary/30 transition duration-500"\>\</div\>

                \<div class="flex justify-between items-center mb-2 relative z-10"\>  
                    \<span class="text-\[10px\] font-bold text-brand-primary uppercase tracking-widest"\>Streak\</span\>  
                    \<div class="flex items-center gap-1.5 text-orange-400"\>  
                        \<i class="fas fa-fire text-sm animate-pulse"\>\</i\>  
                        \<span class="font-bold text-white text-sm"\>12\</span\>  
                    \</div\>  
                \</div\>  
                  
                \<div class="w-full bg-app-bg h-1.5 rounded-full overflow-hidden relative z-10"\>  
                    \<div class="h-full bg-gradient-to-r from-orange-400 to-red-500 w-\[65%\] rounded-full shadow-\[0\_0\_8px\_rgba(249,115,22,0.6)\]"\>\</div\>  
                \</div\>  
                \<div class="flex justify-between text-\[9px\] text-gray-500 mt-1.5 relative z-10"\>  
                    \<span\>Daily Goal\</span\>  
                    \<span class="text-gray-300"\>13 / 20\</span\>  
                \</div\>  
            \</div\>

            \<\!-- Profile Mini \--\>  
            \<div class="mt-4 flex items-center gap-3 px-1 cursor-pointer hover:opacity-80 transition"\>  
                \<div class="relative"\>  
                    \<img src="https://i.pravatar.cc/150?u=1" class="w-8 h-8 rounded-full border border-gray-600"\>  
                    \<div class="absolute bottom-0 right-0 w-2.5 h-2.5 bg-status-success border-2 border-app-surface rounded-full"\>\</div\>  
                \</div\>  
                \<div class="flex-1 min-w-0"\>  
                    \<div class="text-sm font-medium text-white truncate"\>Kamil Karatov\</div\>  
                    \<div class="text-\[10px\] text-gray-500"\>Pro Plan\</div\>  
                \</div\>  
                \<i class="fas fa-cog text-gray-600 hover:text-white transition"\>\</i\>  
            \</div\>  
        \</div\>  
    \</aside\>

    \<\!-- \==================== MAIN AREA \==================== \--\>  
    \<div class="flex-1 flex flex-col min-w-0 bg-app-bg relative"\>  
          
        \<\!-- HEADER (64px) \--\>  
        \<header class="h-16 glass border-b border-app-border flex items-center justify-between px-8 sticky top-0 z-20"\>  
              
            \<\!-- Breadcrumbs / Title \--\>  
            \<div class="flex items-center gap-2 text-sm"\>  
                \<span class="text-gray-500 hover:text-white transition cursor-pointer"\>Project\</span\>  
                \<i class="fas fa-chevron-right text-\[10px\] text-gray-700"\>\</i\>  
                \<span class="text-gray-100 font-semibold"\>Dashboard\</span\>  
            \</div\>

            \<\!-- Global Search \--\>  
            \<div class="relative w-96 group"\>  
                \<div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none"\>  
                    \<i class="fas fa-search text-gray-600 group-focus-within:text-brand-primary transition-colors"\>\</i\>  
                \</div\>  
                \<input type="text"   
                       class="block w-full pl-10 pr-3 py-2 border border-app-border rounded-lg leading-5 bg-app-surface text-gray-300 placeholder-gray-600 focus:outline-none focus:border-brand-primary focus:ring-1 focus:ring-brand-primary sm:text-sm transition-all duration-200"   
                       placeholder="Search cards, decks or tags..."\>  
                \<div class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none"\>  
                    \<span class="text-gray-600 text-xs border border-gray-700 rounded px-1.5 py-0.5"\>Ctrl+K\</span\>  
                \</div\>  
            \</div\>

            \<\!-- Header Actions \--\>  
            \<div class="flex items-center gap-4"\>  
                \<button class="relative p-2 text-gray-400 hover:text-white transition"\>  
                    \<i class="far fa-bell"\>\</i\>  
                    \<span class="absolute top-1.5 right-2 w-2 h-2 bg-brand-pink rounded-full border border-app-surface"\>\</span\>  
                \</button\>  
                  
                \<button class="bg-gradient-to-r from-brand-primary to-brand-secondary text-white px-4 py-2 rounded-lg text-sm font-bold shadow-glow hover:brightness-110 transition active:scale-95 flex items-center gap-2"\>  
                    \<i class="fas fa-play text-xs"\>\</i\> Study  
                \</button\>  
            \</div\>  
        \</header\>

        \<\!-- CONTENT SCROLL AREA \--\>  
        \<main class="flex-1 overflow-y-auto p-8 relative"\>  
            \<\!-- Background Gradient Decoration \--\>  
            \<div class="absolute top-0 left-0 w-full h-96 bg-gradient-to-b from-brand-primary/5 to-transparent pointer-events-none"\>\</div\>

            \<\!-- DYNAMIC CONTENT GOES HERE \--\>  
            \<div class="relative z-10 border-2 border-dashed border-white/10 rounded-2xl h-full flex flex-col items-center justify-center text-gray-600 gap-4"\>  
                \<i class="fas fa-layer-group text-4xl opacity-20"\>\</i\>  
                \<span\>Content will be loaded here\</span\>  
            \</div\>

        \</main\>

    \</div\>

\</body\>  
\</html\>

### **Что у нас получилось?**

1. **Sidebar:** Четкая иерархия, виджет Стрика с неоновым свечением, профиль внизу.  
2. **Header:** Стеклянный эффект, поле поиска в стиле "Command Palette", яркая кнопка призыва к действию (Study).  
3. **Контейнер:** Центральная область main с декоративным градиентом сверху, чтобы экран не выглядел плоским.

**Это наша "рамка".** Все остальные страницы (Dashboard, Library, Editor) будут просто вставляться внутрь блока DYNAMIC CONTENT GOES HERE.

Готов двигаться дальше к наполнению **Dashboard** (Главной страницы)?

Отлично. Переходим к наполнению 

## **Dashboard (Главная страница)**.

Это «Командный центр» пользователя. В стиле Leonardo.ai он должен выглядеть не как скучная таблица, а как **панель управления космическим кораблем**.

### **Ключевые элементы (согласно требованиям):**

1. **Hero Section:** Персонализированное приветствие и главный призыв к действию (Продолжить обучение).  
2. **Daily Progress (SR-ANL-03):** Кольца активности или прогресс-бары (Новые слова, Повторения).  
3. **Activity Heatmap (SR-ANL-02):** График активности (как на GitHub), показывающий дисциплину.  
4. **Recent Decks:** Быстрый доступ к последним колодам.

---

### **Инструкция:**

1. Создай файл dashboard.html.  
2. Скопируй код **Layout** (из предыдущего шага) и **замени** содержимое тега \<main\> на код ниже.  
3. Или просто используй этот полный код (я объединил Layout \+ Dashboard для удобства просмотра).

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Dashboard\</title\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: { bg: '\#0B0F15', surface: '\#131927', hover: '\#1C2438', border: 'rgba(255, 255, 255, 0.08)' },  
                        brand: { primary: '\#8B5CF6', secondary: '\#3B82F6', pink: '\#EC4899' },  
                        status: { success: '\#10B981', warning: '\#F59E0B', error: '\#EF4444' }  
                    },  
                    fontFamily: { sans: \['Inter', 'sans-serif'\] },  
                    boxShadow: { 'glow': '0 0 20px rgba(139, 92, 246, 0.15)' }  
                }  
            }  
        }  
    \</script\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');  
        ::-webkit-scrollbar { width: 6px; height: 6px; }  
        ::-webkit-scrollbar-track { background: \#0B0F15; }  
        ::-webkit-scrollbar-thumb { background: \#1C2438; border-radius: 3px; }  
        .glass { background: rgba(19, 25, 39, 0.7); backdrop-filter: blur(12px); border: 1px solid rgba(255, 255, 255, 0.08); }  
        .card-hover:hover { border-color: rgba(139, 92, 246, 0.5); transform: translateY(-2px); box-shadow: 0 10px 30px \-10px rgba(139, 92, 246, 0.2); }  
          
        /\* Heatmap Grid \*/  
        .heatmap-grid { display: grid; grid-template-rows: repeat(7, 1fr); grid-auto-flow: column; gap: 4px; }  
        .h-cell { width: 10px; height: 10px; border-radius: 2px; }  
        .l-0 { background: \#1C2438; }  
        .l-1 { background: rgba(139, 92, 246, 0.2); }  
        .l-2 { background: rgba(139, 92, 246, 0.5); }  
        .l-3 { background: rgba(139, 92, 246, 0.8); }  
        .l-4 { background: \#8B5CF6; box-shadow: 0 0 5px \#8B5CF6; }  
    \</style\>  
\</head\>  
\<body class="bg-app-bg text-gray-400 font-sans h-screen flex overflow-hidden"\>

    \<\!-- SIDEBAR (Placeholder from Layout) \--\>  
    \<aside class="w-\[260px\] bg-app-surface border-r border-app-border flex flex-col z-30"\>  
        \<div class="h-16 flex items-center px-6 border-b border-app-border"\>  
            \<div class="w-8 h-8 rounded-lg bg-gradient-to-br from-brand-primary to-brand-secondary flex items-center justify-center text-white font-bold text-lg mr-3 shadow-glow"\>P\</div\>  
            \<span class="text-white font-bold text-lg"\>PVS.ai\</span\>  
        \</div\>  
        \<div class="p-6"\>  
            \<div class="text-xs uppercase font-bold text-gray-600 mb-2"\>Menu Placeholder\</div\>  
            \<div class="h-2 w-full bg-app-hover rounded mb-2"\>\</div\>  
            \<div class="h-2 w-3/4 bg-app-hover rounded"\>\</div\>  
        \</div\>  
    \</aside\>

    \<\!-- MAIN CONTENT \--\>  
    \<div class="flex-1 flex flex-col min-w-0 bg-app-bg relative"\>  
          
        \<\!-- HEADER \--\>  
        \<header class="h-16 glass border-b border-app-border flex items-center justify-between px-8 sticky top-0 z-20"\>  
            \<div class="text-sm text-gray-500"\>  
                Project / \<span class="text-gray-200 font-semibold"\>Dashboard\</span\>  
            \</div\>  
            \<div class="flex items-center gap-4"\>  
                \<button class="bg-gradient-to-r from-brand-primary to-brand-secondary text-white px-4 py-2 rounded-lg text-sm font-bold shadow-glow hover:brightness-110 transition"\>  
                    \<i class="fas fa-play mr-2"\>\</i\> Study Now  
                \</button\>  
            \</div\>  
        \</header\>

        \<\!-- DASHBOARD CONTENT \--\>  
        \<main class="flex-1 overflow-y-auto p-8 relative"\>  
            \<div class="absolute top-0 left-0 w-full h-96 bg-gradient-to-b from-brand-primary/5 to-transparent pointer-events-none"\>\</div\>

            \<div class="max-w-6xl mx-auto space-y-8 relative z-10"\>

                \<\!-- 1\. HERO SECTION \--\>  
                \<section class="flex justify-between items-end"\>  
                    \<div\>  
                        \<h1 class="text-3xl font-bold text-white mb-2"\>Good Evening, Kamil\</h1\>  
                        \<p class="text-gray-400"\>You're on a \<span class="text-brand-primary font-bold"\>12-day streak\</span\>\! Keep the momentum going.\</p\>  
                    \</div\>  
                    \<div class="flex gap-4"\>  
                         \<\!-- Quick Stats \--\>  
                         \<div class="px-4 py-2 bg-app-surface border border-app-border rounded-lg text-xs"\>  
                             \<span class="block text-gray-500"\>Vocabulary Size\</span\>  
                             \<strong class="text-white text-lg"\>2,540\</strong\>  
                         \</div\>  
                         \<div class="px-4 py-2 bg-app-surface border border-app-border rounded-lg text-xs"\>  
                             \<span class="block text-gray-500"\>Retention\</span\>  
                             \<strong class="text-status-success text-lg"\>94%\</strong\>  
                         \</div\>  
                    \</div\>  
                \</section\>

                \<\!-- 2\. DAILY GOALS GRID \--\>  
                \<section class="grid grid-cols-1 md:grid-cols-3 gap-6"\>  
                      
                    \<\!-- Goal: Reviews \--\>  
                    \<div class="glass p-6 rounded-2xl border border-app-border relative overflow-hidden group"\>  
                        \<div class="absolute right-0 top-0 p-4 opacity-10 group-hover:opacity-20 transition"\>  
                            \<i class="fas fa-sync-alt text-6xl text-brand-secondary"\>\</i\>  
                        \</div\>  
                        \<div class="flex justify-between items-start mb-4"\>  
                            \<div\>  
                                \<div class="text-xs font-bold text-brand-secondary uppercase tracking-widest mb-1"\>Reviews\</div\>  
                                \<div class="text-2xl font-bold text-white"\>45 \<span class="text-sm text-gray-500 font-normal"\>/ 100\</span\>\</div\>  
                            \</div\>  
                        \</div\>  
                        \<div class="w-full bg-app-bg h-2 rounded-full overflow-hidden"\>  
                            \<div class="bg-brand-secondary h-full w-\[45%\] shadow-\[0\_0\_10px\_rgba(59,130,246,0.5)\]"\>\</div\>  
                        \</div\>  
                        \<div class="mt-4 text-xs text-gray-400"\>  
                            55 cards remaining to maintain memory.  
                        \</div\>  
                    \</div\>

                    \<\!-- Goal: New Words \--\>  
                    \<div class="glass p-6 rounded-2xl border border-app-border relative overflow-hidden group"\>  
                        \<div class="absolute right-0 top-0 p-4 opacity-10 group-hover:opacity-20 transition"\>  
                            \<i class="fas fa-plus-circle text-6xl text-brand-primary"\>\</i\>  
                        \</div\>  
                        \<div class="flex justify-between items-start mb-4"\>  
                            \<div\>  
                                \<div class="text-xs font-bold text-brand-primary uppercase tracking-widest mb-1"\>New Words\</div\>  
                                \<div class="text-2xl font-bold text-white"\>13 \<span class="text-sm text-gray-500 font-normal"\>/ 20\</span\>\</div\>  
                            \</div\>  
                            \<div class="w-8 h-8 rounded-full bg-brand-primary/20 text-brand-primary flex items-center justify-center"\>  
                                \<i class="fas fa-check"\>\</i\>  
                            \</div\>  
                        \</div\>  
                        \<div class="w-full bg-app-bg h-2 rounded-full overflow-hidden"\>  
                            \<div class="bg-brand-primary h-full w-\[65%\] shadow-\[0\_0\_10px\_rgba(139,92,246,0.5)\]"\>\</div\>  
                        \</div\>  
                        \<div class="mt-4 text-xs text-gray-400"\>  
                            Great pace\! 7 more to reach daily target.  
                        \</div\>  
                    \</div\>

                    \<\!-- Hero Banner \--\>  
                    \<div class="relative rounded-2xl overflow-hidden border border-app-border group cursor-pointer"\>  
                        \<img src="https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=600\&q=80" class="absolute inset-0 w-full h-full object-cover opacity-50 group-hover:scale-105 transition duration-700"\>  
                        \<div class="absolute inset-0 bg-gradient-to-t from-app-bg via-app-bg/80 to-transparent"\>\</div\>  
                          
                        \<div class="absolute bottom-0 left-0 p-6"\>  
                            \<div class="flex items-center gap-2 mb-2"\>  
                                \<span class="px-2 py-1 bg-brand-pink/20 text-brand-pink text-\[10px\] font-bold rounded uppercase backdrop-blur-md border border-brand-pink/30"\>Continue\</span\>  
                            \</div\>  
                            \<h3 class="text-lg font-bold text-white mb-1"\>English Idioms\</h3\>  
                            \<p class="text-xs text-gray-400 mb-4"\>You stopped at card \#142\</p\>  
                            \<button class="bg-white text-app-bg px-4 py-2 rounded-lg text-xs font-bold hover:bg-gray-200 transition"\>Resume Session\</button\>  
                        \</div\>  
                    \</div\>

                \</section\>

                \<\!-- 3\. HEATMAP (SR-ANL-02) \--\>  
                \<section class="glass p-6 rounded-2xl border border-app-border"\>  
                    \<div class="flex justify-between items-center mb-6"\>  
                        \<h3 class="text-sm font-bold text-gray-300"\>Activity Log\</h3\>  
                        \<div class="flex gap-2 text-xs"\>  
                            \<div class="flex items-center gap-1"\>\<div class="w-3 h-3 rounded-sm bg-app-hover"\>\</div\> \<span class="text-gray-500"\>Less\</span\>\</div\>  
                            \<div class="flex items-center gap-1"\>\<div class="w-3 h-3 rounded-sm bg-brand-primary"\>\</div\> \<span class="text-gray-500"\>More\</span\>\</div\>  
                        \</div\>  
                    \</div\>  
                      
                    \<\!-- FAKE HEATMAP GRID \--\>  
                    \<div class="overflow-x-auto pb-2"\>  
                        \<div class="heatmap-grid min-w-\[600px\]"\>  
                            \<\!-- Generating random cells for demo \--\>  
                            \<script\>  
                                for(let i=0; i\<300; i++) {  
                                    const intensity \= Math.random() \> 0.7 ? Math.floor(Math.random() \* 5\) : 0;  
                                    document.write(\`\<div class="h-cell l-${intensity}" title="Activity: ${intensity}"\>\</div\>\`);  
                                }  
                            \</script\>  
                        \</div\>  
                    \</div\>  
                \</section\>

                \<\!-- 4\. RECENT DECKS \--\>  
                \<section\>  
                    \<div class="flex justify-between items-center mb-4"\>  
                        \<h3 class="text-sm font-bold text-gray-300 uppercase tracking-widest"\>Recent Decks\</h3\>  
                        \<a href="\#" class="text-xs text-brand-primary hover:text-white transition"\>View Library \-\>\</a\>  
                    \</div\>  
                      
                    \<div class="grid grid-cols-1 md:grid-cols-4 gap-4"\>  
                          
                        \<\!-- Deck 1 \--\>  
                        \<div class="bg-app-surface rounded-xl overflow-hidden border border-app-border card-hover group cursor-pointer transition-all duration-300"\>  
                            \<div class="h-32 bg-gray-800 relative"\>  
                                \<img src="https://images.unsplash.com/photo-1543269865-cbf427effbad?w=400\&q=80" class="w-full h-full object-cover opacity-60 group-hover:opacity-80 transition"\>  
                                \<div class="absolute top-2 right-2 bg-black/60 text-white text-\[10px\] px-1.5 py-0.5 rounded backdrop-blur"\>  
                                    \<i class="fas fa-layer-group text-brand-primary mr-1"\>\</i\> 120  
                                \</div\>  
                            \</div\>  
                            \<div class="p-4"\>  
                                \<h4 class="text-white font-bold text-sm mb-1 truncate group-hover:text-brand-primary transition"\>Business English\</h4\>  
                                \<div class="flex items-center justify-between text-xs text-gray-500 mt-3"\>  
                                    \<span\>Due: \<span class="text-status-warning"\>15\</span\>\</span\>  
                                    \<span\>New: \<span class="text-brand-secondary"\>5\</span\>\</span\>  
                                \</div\>  
                            \</div\>  
                        \</div\>

                         \<\!-- Deck 2 \--\>  
                         \<div class="bg-app-surface rounded-xl overflow-hidden border border-app-border card-hover group cursor-pointer transition-all duration-300"\>  
                            \<div class="h-32 bg-gray-800 relative"\>  
                                \<img src="https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=400\&q=80" class="w-full h-full object-cover opacity-60 group-hover:opacity-80 transition"\>  
                            \</div\>  
                            \<div class="p-4"\>  
                                \<h4 class="text-white font-bold text-sm mb-1 truncate group-hover:text-brand-primary transition"\>Finance Basics\</h4\>  
                                \<div class="flex items-center justify-between text-xs text-gray-500 mt-3"\>  
                                    \<span class="text-status-success"\>\<i class="fas fa-check mr-1"\>\</i\> Completed\</span\>  
                                \</div\>  
                            \</div\>  
                        \</div\>

                         \<\!-- Add New Deck \--\>  
                         \<div class="bg-app-surface/50 rounded-xl border border-dashed border-app-border hover:border-brand-primary/50 hover:bg-app-surface transition cursor-pointer flex flex-col items-center justify-center h-full min-h-\[200px\] group text-center p-4"\>  
                            \<div class="w-10 h-10 rounded-full bg-white/5 flex items-center justify-center mb-2 group-hover:bg-brand-primary group-hover:text-white text-gray-500 transition duration-300"\>  
                                \<i class="fas fa-plus"\>\</i\>  
                            \</div\>  
                            \<span class="text-sm font-medium text-gray-400 group-hover:text-white transition"\>Create Deck\</span\>  
                        \</div\>

                    \</div\>  
                \</section\>

            \</div\>  
        \</main\>  
    \</div\>  
\</body\>  
\</html\>

Это **финальный дизайн Dashboard**. Он соответствует всем требованиям (Heatmap, Daily Goals, Recent) и выглядит как современный SaaS.

Отлично. Мы утвердили **Дизайн-систему** и сверстали **Dashboard** (Главную).

Теперь нам нужно последовательно отрисовать остальные ключевые экраны, чтобы у тебя был полный комплект UI для передачи фронтенд-разработчикам.

Предлагаю такой порядок:

1. **Library (Библиотека)** — Управление контентом (папки, колоды).  
2. **Card Editor (Редактор)** — Создание карточек (Ручное \+ AI).  
3. **Study Session (Урок)** — Режим фокусировки (без сайдбара).  
4. **Marketplace (Витрина)** — Поиск и покупка курсов.

Давай начнем с **Библиотеки**. Это место, где пользователь проводит много времени, организуя свои знания.

---

### **Экран: Library (Библиотека)**

**Особенности дизайна (Leonardo Style):**

* **Иерархия:** Мы визуально разделяем **Папки** (Folders) и **Колоды** (Decks).  
* **Breadcrumbs (Хлебные крошки):** Сверху показываем путь: English C1 \> Grammar \> Tenses.  
* **Статистика:** Сверху добавляем "Project Stats Banner" — мотивационный блок с общим прогрессом по проекту (SR-ANL-01).

Сохрани этот код как library.html.

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Library\</title\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: { bg: '\#0B0F15', surface: '\#131927', hover: '\#1C2438', border: 'rgba(255, 255, 255, 0.08)' },  
                        brand: { primary: '\#8B5CF6', secondary: '\#3B82F6', pink: '\#EC4899' },  
                        status: { success: '\#10B981', warning: '\#F59E0B' }  
                    },  
                    fontFamily: { sans: \['Inter', 'sans-serif'\] },  
                    boxShadow: { 'glow': '0 0 20px rgba(139, 92, 246, 0.15)' }  
                }  
            }  
        }  
    \</script\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');  
        ::-webkit-scrollbar { width: 6px; }  
        ::-webkit-scrollbar-track { background: \#0B0F15; }  
        ::-webkit-scrollbar-thumb { background: \#1C2438; border-radius: 3px; }  
        .glass { background: rgba(19, 25, 39, 0.7); backdrop-filter: blur(12px); border: 1px solid rgba(255, 255, 255, 0.08); }  
        .glass-hover:hover { background: rgba(139, 92, 246, 0.1); border-color: rgba(139, 92, 246, 0.3); }  
    \</style\>  
\</head\>  
\<body class="bg-app-bg text-gray-400 font-sans h-screen flex overflow-hidden"\>

    \<\!-- SIDEBAR (Placeholder) \--\>  
    \<aside class="w-\[260px\] bg-app-surface border-r border-app-border flex flex-col z-30"\>  
        \<div class="h-16 flex items-center px-6 border-b border-app-border"\>  
            \<div class="w-8 h-8 rounded-lg bg-gradient-to-br from-brand-primary to-brand-secondary flex items-center justify-center text-white font-bold text-lg mr-3 shadow-glow"\>P\</div\>  
            \<span class="text-white font-bold text-lg"\>PVS.ai\</span\>  
        \</div\>  
        \<div class="p-6"\>  
            \<div class="text-xs uppercase font-bold text-gray-600 mb-2"\>Sidebar Active: Library\</div\>  
            \<div class="h-2 w-full bg-app-hover rounded mb-2"\>\</div\>  
            \<div class="h-2 w-3/4 bg-brand-primary/20 rounded"\>\</div\>  
        \</div\>  
    \</aside\>

    \<\!-- MAIN CONTENT \--\>  
    \<div class="flex-1 flex flex-col min-w-0 bg-app-bg relative"\>  
          
        \<\!-- HEADER (Breadcrumbs & Actions) \--\>  
        \<header class="h-16 glass border-b border-app-border flex items-center justify-between px-8 sticky top-0 z-20"\>  
            \<\!-- Breadcrumbs \--\>  
            \<div class="flex items-center gap-2 text-sm"\>  
                \<span class="text-gray-500 hover:text-white transition cursor-pointer"\>Project\</span\>  
                \<i class="fas fa-chevron-right text-\[10px\] text-gray-700"\>\</i\>  
                \<span class="text-gray-300 hover:text-white transition cursor-pointer"\>English C1\</span\>  
                \<i class="fas fa-chevron-right text-\[10px\] text-gray-700"\>\</i\>  
                \<span class="text-white font-semibold flex items-center gap-2"\>  
                    \<i class="fas fa-layer-group text-brand-primary"\>\</i\> Library  
                \</span\>  
            \</div\>

            \<\!-- Search & Add \--\>  
            \<div class="flex items-center gap-3"\>  
                \<div class="relative group"\>  
                    \<i class="fas fa-search absolute left-3 top-2.5 text-gray-600 group-focus-within:text-brand-primary transition"\>\</i\>  
                    \<input type="text" placeholder="Filter decks..." class="bg-app-bg border border-app-border rounded-lg pl-9 pr-4 py-1.5 text-sm text-white focus:border-brand-primary focus:outline-none w-48 transition-all focus:w-64"\>  
                \</div\>  
                \<div class="h-6 w-px bg-app-border mx-1"\>\</div\>  
                \<button class="bg-app-hover hover:bg-white/10 text-white px-3 py-1.5 rounded-lg text-xs font-bold border border-app-border transition"\>  
                    \<i class="fas fa-folder-plus mr-1"\>\</i\> New Folder  
                \</button\>  
                \<button class="bg-brand-primary hover:brightness-110 text-white px-3 py-1.5 rounded-lg text-xs font-bold shadow-glow transition"\>  
                    \<i class="fas fa-plus mr-1"\>\</i\> New Deck  
                \</button\>  
            \</div\>  
        \</header\>

        \<\!-- SCROLL AREA \--\>  
        \<main class="flex-1 overflow-y-auto p-8 relative"\>  
              
            \<\!-- PROJECT STATS BANNER \--\>  
            \<div class="w-full bg-gradient-to-r from-app-surface to-app-bg border border-app-border rounded-2xl p-6 mb-10 relative overflow-hidden flex items-center justify-between"\>  
                \<div class="absolute right-0 top-0 w-96 h-full bg-gradient-to-l from-brand-primary/10 to-transparent pointer-events-none"\>\</div\>  
                  
                \<div class="flex gap-12 z-10"\>  
                    \<div\>  
                        \<div class="text-gray-500 text-\[10px\] uppercase font-bold tracking-wider mb-1"\>Total Lemmas\</div\>  
                        \<div class="text-2xl font-bold text-white"\>2,543\</div\>  
                    \</div\>  
                    \<div\>  
                        \<div class="text-gray-500 text-\[10px\] uppercase font-bold tracking-wider mb-1"\>Mature (Known)\</div\>  
                        \<div class="text-2xl font-bold text-status-success"\>1,850 \<span class="text-xs font-medium text-gray-500 opacity-60"\>/ B1\</span\>\</div\>  
                    \</div\>  
                    \<div\>  
                        \<div class="text-gray-500 text-\[10px\] uppercase font-bold tracking-wider mb-1"\>Learning\</div\>  
                        \<div class="text-2xl font-bold text-brand-secondary"\>350\</div\>  
                    \</div\>  
                \</div\>  
                \<div class="z-10"\>  
                    \<button class="text-xs text-gray-400 hover:text-white flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-white/5 transition"\>  
                        \<i class="fas fa-chart-pie"\>\</i\> View Analytics  
                    \</button\>  
                \</div\>  
            \</div\>

            \<\!-- FOLDERS GRID \--\>  
            \<section class="mb-8"\>  
                \<div class="flex items-center justify-between mb-4"\>  
                    \<h2 class="text-xs font-bold text-gray-500 uppercase tracking-widest flex items-center gap-2"\>  
                        \<i class="fas fa-folder text-brand-secondary"\>\</i\> Folders  
                    \</h2\>  
                \</div\>  
                  
                \<div class="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4"\>  
                    \<\!-- Folder Item \--\>  
                    \<div class="glass glass-hover p-4 rounded-xl flex items-center gap-4 cursor-pointer group transition-all duration-300"\>  
                        \<div class="w-10 h-10 bg-app-bg border border-app-border rounded-lg flex items-center justify-center text-gray-500 group-hover:text-brand-secondary group-hover:border-brand-secondary/50 transition"\>  
                            \<i class="fas fa-folder text-lg"\>\</i\>  
                        \</div\>  
                        \<div class="flex-1 min-w-0"\>  
                            \<div class="text-sm font-bold text-gray-200 group-hover:text-white truncate"\>Grammar Rules\</div\>  
                            \<div class="text-\[10px\] text-gray-500"\>12 decks • 450 cards\</div\>  
                        \</div\>  
                        \<i class="fas fa-chevron-right text-\[10px\] text-gray-600 group-hover:text-white transition"\>\</i\>  
                    \</div\>

                    \<\!-- Folder Item \--\>  
                    \<div class="glass glass-hover p-4 rounded-xl flex items-center gap-4 cursor-pointer group transition-all duration-300"\>  
                        \<div class="w-10 h-10 bg-app-bg border border-app-border rounded-lg flex items-center justify-center text-gray-500 group-hover:text-brand-pink group-hover:border-brand-pink/50 transition"\>  
                            \<i class="fas fa-film text-lg"\>\</i\>  
                        \</div\>  
                        \<div class="flex-1 min-w-0"\>  
                            \<div class="text-sm font-bold text-gray-200 group-hover:text-white truncate"\>TV Series\</div\>  
                            \<div class="text-\[10px\] text-gray-500"\>5 decks • 2,100 cards\</div\>  
                        \</div\>  
                        \<i class="fas fa-chevron-right text-\[10px\] text-gray-600 group-hover:text-white transition"\>\</i\>  
                    \</div\>  
                \</div\>  
            \</section\>

            \<\!-- DECKS GRID \--\>  
            \<section\>  
                \<div class="flex items-center justify-between mb-4"\>  
                    \<h2 class="text-xs font-bold text-gray-500 uppercase tracking-widest flex items-center gap-2"\>  
                        \<i class="fas fa-layer-group text-brand-primary"\>\</i\> Root Decks  
                    \</h2\>  
                \</div\>

                \<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6"\>  
                      
                    \<\!-- Deck Card (Standard) \--\>  
                    \<div class="glass rounded-xl overflow-hidden hover:-translate-y-1 transition-all duration-300 border border-app-border hover:border-brand-primary/50 group cursor-pointer"\>  
                        \<\!-- Cover \--\>  
                        \<div class="h-32 bg-app-bg relative overflow-hidden"\>  
                            \<img src="https://images.unsplash.com/photo-1543269865-cbf427effbad?w=400\&q=80" class="w-full h-full object-cover opacity-60 group-hover:opacity-100 transition duration-500"\>  
                            \<div class="absolute inset-0 bg-gradient-to-t from-app-surface to-transparent"\>\</div\>  
                              
                            \<\!-- Menu \--\>  
                            \<button class="absolute top-2 right-2 p-1.5 bg-black/60 rounded text-gray-300 hover:text-white opacity-0 group-hover:opacity-100 transition backdrop-blur"\>  
                                \<i class="fas fa-ellipsis-h"\>\</i\>  
                            \</button\>  
                        \</div\>

                        \<\!-- Content \--\>  
                        \<div class="p-4 pt-2"\>  
                            \<div class="flex justify-between items-start mb-2"\>  
                                \<h3 class="text-base font-bold text-white leading-tight group-hover:text-brand-primary transition"\>Business English\</h3\>  
                            \</div\>  
                              
                            \<\!-- Progress Bar \--\>  
                            \<div class="w-full h-1 bg-app-bg rounded-full mb-3 overflow-hidden"\>  
                                \<div class="h-full bg-brand-primary w-\[45%\]"\>\</div\>  
                            \</div\>

                            \<\!-- Footer \--\>  
                            \<div class="flex items-center justify-between text-\[10px\] text-gray-500 pt-2 border-t border-app-border"\>  
                                \<div class="flex gap-3"\>  
                                    \<span class="flex items-center gap-1"\>\<i class="fas fa-clone"\>\</i\> 120\</span\>  
                                    \<span class="flex items-center gap-1 text-status-warning"\>\<i class="fas fa-clock"\>\</i\> 15 due\</span\>  
                                \</div\>  
                                \<\!-- Hover Action \--\>  
                                \<button class="text-white hover:text-brand-primary transition opacity-0 group-hover:opacity-100"\>  
                                    \<i class="fas fa-play"\>\</i\> Study  
                                \</button\>  
                            \</div\>  
                        \</div\>  
                    \</div\>

                    \<\!-- Deck Card (Marketplace Purchase) \--\>  
                    \<div class="glass rounded-xl overflow-hidden hover:-translate-y-1 transition-all duration-300 border border-app-border hover:border-brand-secondary/50 group cursor-pointer"\>  
                        \<div class="h-32 bg-app-bg relative overflow-hidden"\>  
                            \<img src="https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=400\&q=80" class="w-full h-full object-cover opacity-60 group-hover:opacity-100 transition duration-500"\>  
                            \<div class="absolute inset-0 bg-gradient-to-t from-app-surface to-transparent"\>\</div\>  
                            \<div class="absolute top-2 left-2 bg-brand-secondary/90 text-white text-\[9px\] font-bold px-1.5 py-0.5 rounded backdrop-blur uppercase"\>  
                                Purchased  
                            \</div\>  
                        \</div\>

                        \<div class="p-4 pt-2"\>  
                            \<h3 class="text-base font-bold text-white leading-tight mb-2 group-hover:text-brand-secondary transition"\>Finance Terms\</h3\>  
                              
                            \<div class="w-full h-1 bg-app-bg rounded-full mb-3 overflow-hidden"\>  
                                \<div class="h-full bg-brand-secondary w-\[10%\]"\>\</div\>  
                            \</div\>

                            \<div class="flex items-center justify-between text-\[10px\] text-gray-500 pt-2 border-t border-app-border"\>  
                                \<div class="flex gap-3"\>  
                                    \<span class="flex items-center gap-1"\>\<i class="fas fa-clone"\>\</i\> 450\</span\>  
                                    \<span class="flex items-center gap-1 text-status-success"\>\<i class="fas fa-check"\>\</i\> Done\</span\>  
                                \</div\>  
                            \</div\>  
                        \</div\>  
                    \</div\>

                \</div\>  
            \</section\>

        \</div\>  
    \</div\>  
\</body\>  
\</html\>

Как тебе страница? Если все нравится, мы можем переходить к следующей — **Card Editor** или **Study Session**.

Отлично. Для следующего шага я предлагаю сделать **Card Editor (Редактор карточек)**. Это более сложная страница, которая демонстрирует много нашего функционала (ручной ввод, медиа, AI-помощник), и важно убедиться, что её дизайн удобен и интуитивен.

---

### **Экран: Card Editor (Редактор карточек)**

**Особенности дизайна (Leonardo Style \+ Anki Functionality):**

* **Две колонки:** Левая — основная форма ввода, правая — сворачиваемая панель AI Assistant (как в Leonardo).  
* **Sentence Mining:** Основной акцент на полях Sentence и Target Word.  
* **Медиа:** Интегрированные зоны для загрузки изображений и аудио, включая Auto TTS.  
* **AI Assistant:** Пассивно предлагает помощь, не отвлекая.  
* **Header:** Четкое указание, в какую колоду добавляется карточка.

Сохрани этот код как editor\_page.html.

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Card Editor\</title\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: { bg: '\#0B0F15', surface: '\#131927', hover: '\#1C2438', border: 'rgba(255, 255, 255, 0.08)' },  
                        brand: { primary: '\#8B5CF6', secondary: '\#3B82F6', pink: '\#EC4899' },  
                        status: { success: '\#10B981' }  
                    },  
                    fontFamily: { sans: \['Inter', 'sans-serif'\] },  
                    boxShadow: { 'glow': '0 0 20px rgba(139, 92, 246, 0.15)' }  
                }  
            }  
        }  
    \</script\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');  
        ::-webkit-scrollbar { width: 6px; height: 6px; }  
        ::-webkit-scrollbar-track { background: \#0B0F15; }  
        ::-webkit-scrollbar-thumb { background: \#1C2438; border-radius: 3px; }  
        .glass { background: rgba(19, 25, 39, 0.7); backdrop-filter: blur(12px); border: 1px solid rgba(255, 255, 255, 0.08); }  
        .input-dark { background-color: \#080c14; border: 1px solid rgba(255,255,255,0.1); color: white; transition: all 0.2s; }  
        .input-dark:focus { border-color: \#8B5CF6; box-shadow: 0 0 0 1px \#8B5CF6; outline: none; }  
        .dropzone { border: 2px dashed rgba(255,255,255,0.1); background: rgba(255,255,255,0.02); transition: all 0.2s; }  
        .dropzone:hover { border-color: \#8B5CF6; background: rgba(139, 92, 246, 0.05); color: \#8B5CF6; }  
        .btn-primary { background: linear-gradient(135deg, \#8B5CF6 0%, \#6366F1 100%); transition: all 0.2s; }  
        .btn-primary:hover { filter: brightness(1.1); }  
        .btn-primary:active { transform: scale(0.98); }  
    \</style\>  
\</head\>  
\<body class="h-screen flex flex-col overflow-hidden bg-app-bg text-gray-400 font-sans"\>

    \<\!-- HEADER \--\>  
    \<header class="h-16 glass border-b border-app-border flex items-center justify-between px-6 z-20"\>  
        \<div class="flex items-center gap-4"\>  
            \<button class="text-gray-400 hover:text-white transition"\>\<i class="fas fa-arrow-left"\>\</i\>\</button\>  
            \<h1 class="text-lg font-bold text-white"\>Create Card\</h1\>  
            \<div class="h-6 w-px bg-app-border mx-2"\>\</div\>  
              
            \<\!-- Deck Selector for where to save \--\>  
            \<div class="relative group"\>  
                \<button class="flex items-center gap-2 text-sm text-gray-300 hover:text-white bg-app-bg px-3 py-1.5 rounded-lg border border-app-border transition"\>  
                    \<i class="fas fa-folder text-brand-primary"\>\</i\>  
                    \<span\>English Vocabulary\</span\>  
                    \<i class="fas fa-chevron-down text-xs ml-1"\>\</i\>  
                \</button\>  
            \</div\>  
        \</div\>

        \<div class="flex items-center gap-3"\>  
            \<span class="text-xs text-gray-500 mr-2"\>Changes saved\</span\>  
            \<button class="btn-primary text-white px-6 py-2 rounded-lg text-sm font-bold shadow-glow"\>  
                Save Card  
            \</button\>  
        \</div\>  
    \</header\>

    \<\!-- MAIN WORKSPACE \--\>  
    \<div class="flex-1 flex overflow-hidden"\>  
          
        \<\!-- LEFT: Manual Input Form (Scrollable) \--\>  
        \<div class="flex-1 overflow-y-auto p-8 relative custom-scroll"\>  
            \<div class="absolute top-0 left-0 w-full h-48 bg-gradient-to-b from-brand-primary/5 to-transparent pointer-events-none"\>\</div\>

            \<div class="max-w-3xl mx-auto space-y-8 relative z-10"\>  
                  
                \<\!-- 1\. Sentence (Front) \--\>  
                \<section class="glass p-6 rounded-2xl"\>  
                    \<label class="block text-xs font-bold text-gray-500 uppercase tracking-wider mb-2"\>Front (Sentence)\</label\>  
                    \<div class="relative group"\>  
                        \<textarea class="input-dark w-full p-4 rounded-xl text-lg min-h-\[120px\] resize-none" placeholder="Type or paste your sentence here..."\>\</textarea\>  
                        \<div class="absolute bottom-3 right-3 text-xs text-gray-600"\>  
                            Highlight word to set Target  
                        \</div\>  
                    \</div\>  
                    \<p class="text-xs text-gray-500 mt-2"\>Example: "He decided to \<strong class="text-brand-primary"\>address\</strong\> the issue."\</p\>  
                \</section\>

                \<\!-- 2\. Target & Translation \--\>  
                \<div class="grid grid-cols-2 gap-6"\>  
                    \<section class="glass p-6 rounded-2xl"\>  
                        \<label class="block text-xs font-bold text-gray-500 uppercase tracking-wider mb-2"\>Target Word\</label\>  
                        \<input type="text" class="input-dark w-full p-3 rounded-lg" value="address" placeholder="Auto-filled..."\>  
                        \<p class="text-xs text-gray-500 mt-2"\>This is the word you are focusing on.\</p\>  
                    \</section\>  
                    \<section class="glass p-6 rounded-2xl"\>  
                        \<div class="flex justify-between items-center mb-2"\>  
                            \<label class="block text-xs font-bold text-gray-500 uppercase tracking-wider"\>Back (Meaning)\</label\>  
                            \<button class="text-xs text-brand-primary hover:text-white transition"\>\<i class="fas fa-magic mr-1"\>\</i\> AI Translate\</button\>  
                        \</div\>  
                        \<input type="text" class="input-dark w-full p-3 rounded-lg" value="заняться (проблемой)" placeholder="Translation..."\>  
                        \<p class="text-xs text-gray-500 mt-2"\>Translation of the target word in context.\</p\>  
                    \</section\>  
                \</div\>

                \<\!-- 3\. Media (Anki Style) \--\>  
                \<section class="glass p-6 rounded-2xl"\>  
                    \<label class="block text-xs font-bold text-gray-500 uppercase tracking-wider mb-3"\>Media Attachments\</label\>  
                    \<div class="grid grid-cols-2 gap-4"\>  
                          
                        \<\!-- Image Dropzone (SR-VOC-03) \--\>  
                        \<div class="dropzone rounded-xl h-32 flex flex-col items-center justify-center cursor-pointer group"\>  
                            \<div class="w-10 h-10 rounded-full bg-app-bg border border-app-border flex items-center justify-center mb-2 text-gray-500 group-hover:text-brand-primary group-hover:bg-brand-primary/10 transition"\>  
                                \<i class="fas fa-image text-lg"\>\</i\>  
                            \</div\>  
                            \<span class="text-xs font-medium text-gray-400 group-hover:text-white"\>Drop image or Paste (Ctrl+V)\</span\>  
                        \</div\>

                        \<\!-- Audio Dropzone (SR-VOC-03) \--\>  
                        \<div class="dropzone rounded-xl h-32 flex flex-col items-center justify-center cursor-pointer group relative"\>  
                            \<div class="w-10 h-10 rounded-full bg-app-bg border border-app-border flex items-center justify-center mb-2 text-gray-500 group-hover:text-brand-secondary group-hover:bg-brand-secondary/10 transition"\>  
                                \<i class="fas fa-microphone text-lg"\>\</i\>  
                            \</div\>  
                            \<span class="text-xs font-medium text-gray-400 group-hover:text-white"\>Upload audio or Record\</span\>  
                              
                            \<\!-- Auto TTS Toggle \--\>  
                            \<label class="absolute top-2 right-2 flex items-center gap-2 cursor-pointer bg-app-bg px-2 py-1 rounded border border-app-border"\>  
                                \<span class="text-\[10px\] text-gray-500"\>Auto TTS\</span\>  
                                \<input type="checkbox" checked class="accent-brand-primary h-3 w-3 rounded bg-app-bg border-white/20"\>  
                            \</label\>  
                        \</div\>  
                    \</div\>  
                \</section\>

                \<\!-- 4\. Source Meta (SR-VOC-03) \--\>  
                \<section class="glass p-6 rounded-2xl"\>  
                    \<label class="block text-xs font-bold text-gray-500 uppercase tracking-wider mb-2"\>Source Information\</label\>  
                    \<div class="flex flex-col gap-4"\>  
                        \<\!-- Example of YouTube Source \--\>  
                        \<div class="bg-app-bg p-4 rounded-lg border border-app-border flex items-center gap-3"\>  
                            \<div class="w-10 h-10 bg-red-600/20 text-red-500 rounded-lg flex items-center justify-center border border-red-500/20"\>  
                                \<i class="fab fa-youtube text-xl"\>\</i\>  
                            \</div\>  
                            \<div class="flex-1 min-w-0"\>  
                                \<div class="text-xs text-gray-500 uppercase font-bold"\>YouTube Video\</div\>  
                                \<div class="text-sm text-white truncate"\>Kurzgesagt – In a Nutshell: Why we do what we do\</div\>  
                                \<div class="text-xs text-gray-400"\>Timestamp: 04:20\</div\>  
                            \</div\>  
                            \<button class="text-gray-600 hover:text-white transition"\>\<i class="fas fa-times"\>\</i\>\</button\>  
                        \</div\>  
                        \<\!-- Add more source options \--\>  
                        \<button class="bg-app-bg hover:bg-app-hover border border-app-border px-4 py-2 rounded-lg text-sm text-gray-400 hover:text-white transition flex items-center justify-center gap-2"\>  
                            \<i class="fas fa-plus"\>\</i\> Add Source  
                        \</button\>  
                    \</div\>  
                \</section\>

            \</div\>  
        \</div\>

        \<\!-- RIGHT: AI Assistant (Collapsible) \--\>  
        \<aside class="w-96 bg-app-surface border-l border-app-border flex flex-col flex-shrink-0 z-10 transition-all duration-300"\>  
            \<div class="p-4 border-b border-app-border flex justify-between items-center"\>  
                \<span class="text-sm font-bold text-gray-100 flex items-center gap-2"\>  
                    \<i class="fas fa-robot text-brand-primary"\>\</i\> AI Assistant  
                \</span\>  
                \<button class="text-gray-600 hover:text-white transition"\>  
                    \<i class="fas fa-chevron-right"\>\</i\> \<\!-- Icon to collapse/expand \--\>  
                \</button\>  
            \</div\>

            \<div class="flex-1 overflow-y-auto p-4 space-y-6 custom-scroll"\>  
                  
                \<\!-- Context Generator (SR-AI-01) \--\>  
                \<div\>  
                    \<div class="text-xs font-bold text-gray-500 uppercase mb-3"\>Context Generator\</div\>  
                    \<div class="space-y-2"\>  
                        \<div class="glass p-3 rounded-lg border border-app-border hover:border-brand-primary/50 cursor-pointer transition group"\>  
                            \<p class="text-sm text-gray-100 mb-1"\>"Success is \<span class="text-brand-primary font-bold"\>inevitable\</span\>."\</p\>  
                            \<p class="text-xs text-gray-400"\>Успех неизбежен.\</p\>  
                            \<div class="mt-2 text-\[10px\] text-gray-600 group-hover:text-brand-primary flex items-center gap-1 transition"\>  
                                \<i class="fas fa-plus-circle"\>\</i\> Use this  
                            \</div\>  
                        \</div\>  
                        \<button class="w-full py-2 border border-dashed border-app-border rounded-lg text-xs text-gray-500 hover:text-brand-primary hover:border-brand-primary/50 transition"\>  
                            Generate more examples...  
                        \</button\>  
                    \</div\>  
                \</div\>

                \<\!-- Grammar Explainer (SR-AI-02) \--\>  
                \<div\>  
                    \<div class="text-xs font-bold text-gray-500 uppercase mb-3"\>Grammar Explainer\</div\>  
                    \<div class="glass p-3 rounded-lg border border-app-border text-sm text-gray-400"\>  
                        \<p class="mb-2"\>\<strong\>Inevitably\</strong\> is an adverb formed from the adjective \<em\>inevitable\</em\>. It modifies the verb.\</p\>  
                        \<button class="mt-2 text-xs text-brand-primary font-medium hover:text-white transition"\>Add to notes\</button\>  
                    \</div\>  
                \</div\>

                \<\!-- AI Image/Audio Suggestion \--\>  
                \<div\>  
                    \<div class="text-xs font-bold text-gray-500 uppercase mb-3"\>Media Suggestions\</div\>  
                    \<div class="space-y-2"\>  
                        \<div class="dropzone rounded-lg h-24 flex-col gap-1"\>  
                            \<i class="fas fa-image text-lg"\>\</i\>  
                            \<span class="text-xs font-medium"\>Suggest Image\</span\>  
                        \</div\>  
                        \<div class="dropzone rounded-lg h-24 flex-col gap-1"\>  
                            \<i class="fas fa-volume-up text-lg"\>\</i\>  
                            \<span class="text-xs font-medium"\>Suggest Audio\</span\>  
                        \</div\>  
                    \</div\>  
                \</div\>

            \</div\>  
        \</aside\>

    \</div\>

\</body\>  
\</html\>

### **Что у нас получилось:**

* **Дизайн:** Полностью соответствует нашей темной, "стеклянной" концепции.  
* **Функциональность:** Есть поля для предложения, целевого слова, перевода, зоны для перетаскивания изображений и аудио, а также секция для метаданных источника.  
* **AI-ассистент:** Справа, с кнопками генерации и пояснений. Он сворачиваемый, чтобы не мешать.

**Теперь у нас есть отличный дизайн для Card Editor.** Готов переходить к следующей странице, **Study Session (Урок)**? Это критически важный экран, который должен быть максимально "без отвлечений".

Отлично. Переходим к самому важному экрану — **Study Session (Режим обучения)**.

Это **Focus Mode**. Здесь мы убираем Сайдбар, Поиск и все лишние элементы. Пользователь должен остаться один на один с контентом.

### **Дизайн-концепция (Leonardo Style)**

* **Атмосфера:** Темный фон, фокус на центре. Карточка «парит» в пространстве с легким свечением.  
* **Типографика:** Огромный, читаемый текст для фразы.  
* **SRS Кнопки:** Большие, удобные для нажатия (Fitts's Law), с цветовым кодированием (Semantic Colors из нашей дизайн-системы), но не "вырвиглазные", а приглушенные.  
* **Клавиатура:** Подсказки горячих клавиш (1, 2, 3, 4, Space).

Сохрани этот код как study\_session.html.

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Study Session\</title\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: { bg: '\#0B0F15', surface: '\#131927', hover: '\#1C2438', border: 'rgba(255, 255, 255, 0.08)' },  
                        brand: { primary: '\#8B5CF6', secondary: '\#3B82F6' },  
                        status: {  
                            again: '\#F43F5E',  // Rose-500  
                            hard: '\#F59E0B',   // Amber-500  
                            good: '\#10B981',   // Emerald-500  
                            easy: '\#06B6D4'    // Cyan-500  
                        }  
                    },  
                    fontFamily: { sans: \['Inter', 'sans-serif'\] },  
                    boxShadow: { 'glow': '0 0 40px rgba(139, 92, 246, 0.1)' }  
                }  
            }  
        }  
    \</script\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');  
          
        .glass-card {  
            background: rgba(19, 25, 39, 0.8);  
            backdrop-filter: blur(20px);  
            border: 1px solid rgba(255, 255, 255, 0.08);  
            box-shadow: 0 25px 50px \-12px rgba(0, 0, 0, 0.5);  
        }

        /\* Анимация появления \*/  
        @keyframes fadeUp {  
            from { opacity: 0; transform: translateY(10px); }  
            to { opacity: 1; transform: translateY(0); }  
        }  
        .animate-enter { animation: fadeUp 0.4s ease-out forwards; }  
          
        /\* Кнопки оценки \*/  
        .srs-btn {  
            background: rgba(255,255,255,0.03);  
            border: 1px solid rgba(255,255,255,0.05);  
            transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);  
        }  
        .srs-btn:hover { transform: translateY(-2px); }  
        .srs-btn:active { transform: scale(0.98); }  
          
        /\* Цвета ховеров для кнопок \*/  
        .hover-again:hover { background: rgba(244, 63, 94, 0.1); border-color: rgba(244, 63, 94, 0.3); }  
        .hover-hard:hover { background: rgba(245, 158, 11, 0.1); border-color: rgba(245, 158, 11, 0.3); }  
        .hover-good:hover { background: rgba(16, 185, 129, 0.1); border-color: rgba(16, 185, 129, 0.3); }  
        .hover-easy:hover { background: rgba(6, 182, 212, 0.1); border-color: rgba(6, 182, 212, 0.3); }  
    \</style\>  
\</head\>  
\<body class="bg-app-bg text-gray-200 font-sans h-screen flex flex-col relative overflow-hidden"\>

    \<\!-- Ambient Background Light \--\>  
    \<div class="absolute top-\[-20%\] left-1/2 \-translate-x-1/2 w-\[600px\] h-\[600px\] bg-brand-primary/10 rounded-full blur-\[120px\] pointer-events-none"\>\</div\>

    \<\!-- 1\. TOP BAR (Progress & Exit) \--\>  
    \<header class="h-16 flex items-center justify-between px-8 z-20 relative"\>  
        \<\!-- Progress \--\>  
        \<div class="flex items-center gap-4 w-1/3"\>  
            \<span class="text-sm font-mono text-gray-400"\>12 \<span class="text-gray-600"\>/\</span\> 50\</span\>  
            \<div class="h-1.5 w-32 bg-app-surface rounded-full overflow-hidden"\>  
                \<div class="h-full bg-gradient-to-r from-brand-primary to-brand-secondary w-\[24%\] shadow-\[0\_0\_8px\_rgba(139,92,246,0.5)\]"\>\</div\>  
            \</div\>  
        \</div\>

        \<\!-- Deck Info \--\>  
        \<div class="text-sm font-medium text-gray-400"\>  
            English C1 \<span class="mx-2 text-gray-700"\>•\</span\> Business Idioms  
        \</div\>

        \<\!-- Controls \--\>  
        \<div class="flex items-center gap-4 w-1/3 justify-end"\>  
             \<button class="w-8 h-8 rounded-lg flex items-center justify-center text-gray-500 hover:text-white hover:bg-white/10 transition" title="Settings"\>  
                \<i class="fas fa-cog"\>\</i\>  
            \</button\>  
            \<button class="w-8 h-8 rounded-lg flex items-center justify-center text-gray-500 hover:text-white hover:bg-white/10 transition" title="Exit"\>  
                \<i class="fas fa-times text-lg"\>\</i\>  
            \</button\>  
        \</div\>  
    \</header\>

    \<\!-- 2\. MAIN CARD AREA \--\>  
    \<main class="flex-1 flex flex-col items-center justify-center p-6 relative z-10"\>  
          
        \<\!-- THE CARD \--\>  
        \<div class="glass-card w-full max-w-3xl min-h-\[400px\] rounded-3xl p-10 flex flex-col items-center text-center animate-enter shadow-glow relative"\>  
              
            \<\!-- Tags / Meta (Top Right) \--\>  
            \<div class="absolute top-6 right-6 flex gap-2"\>  
                \<span class="px-2 py-1 rounded bg-app-bg border border-app-border text-\[10px\] text-gray-500 uppercase tracking-wider font-bold"\>New Word\</span\>  
            \</div\>

            \<\!-- Source Context (Top Left) \--\>  
            \<div class="absolute top-6 left-6 flex items-center gap-2 text-xs text-gray-500 hover:text-brand-primary cursor-pointer transition"\>  
                \<i class="fab fa-youtube text-red-500"\>\</i\>  
                \<span\>TED Talk (12:45)\</span\>  
                \<i class="fas fa-external-link-alt text-\[10px\] ml-1"\>\</i\>  
            \</div\>

            \<\!-- Content Container \--\>  
            \<div class="flex-1 flex flex-col items-center justify-center w-full mt-8 mb-8"\>  
                  
                \<\!-- Media Placeholder (Optional, hidden if none) \--\>  
                \<\!-- \<img src="..." class="h-40 rounded-lg mb-6 border border-white/10"\> \--\>

                \<\!-- Sentence (Front) \--\>  
                \<h2 class="text-3xl md:text-4xl leading-tight font-medium text-white mb-6"\>  
                    "Success is not final, failure is not \<span class="text-brand-primary border-b-2 border-brand-primary/50 pb-1"\>fatal\</span\>."  
                \</h2\>

                \<\!-- Translation (Back \- Revealed) \--\>  
                \<div class="text-lg text-gray-400 font-light max-w-xl"\>  
                    "Успех не окончателен, неудача не \<span class="text-gray-200 font-medium"\>смертельна\</span\>."  
                \</div\>

                \<\!-- Grammar / Notes (Back \- Revealed) \--\>  
                \<div class="mt-8 p-4 bg-app-bg/50 border border-app-border rounded-xl text-sm text-left w-full max-w-lg"\>  
                    \<div class="text-\[10px\] text-gray-500 uppercase font-bold mb-1"\>Note\</div\>  
                    \<p class="text-gray-300"\>\<span class="text-brand-secondary"\>Fatal\</span\> — causing death. Often confused with "fateful" (судьбоносный).\</p\>  
                \</div\>

                \<\!-- Audio Button \--\>  
                \<button class="mt-8 w-12 h-12 rounded-full bg-app-surface border border-brand-secondary/30 text-brand-secondary flex items-center justify-center hover:bg-brand-secondary hover:text-white transition shadow-lg hover:shadow-brand-secondary/50"\>  
                    \<i class="fas fa-volume-up text-lg"\>\</i\>  
                \</button\>  
            \</div\>

        \</div\>

    \</main\>

    \<\!-- 3\. CONTROLS (Footer) \--\>  
    \<footer class="h-32 flex flex-col items-center justify-center pb-8 z-20"\>  
          
        \<\!-- SRS Actions \--\>  
        \<div class="grid grid-cols-4 gap-4 w-full max-w-2xl px-4"\>  
              
            \<\!-- Again \--\>  
            \<button class="srs-btn hover-again p-3 rounded-xl flex flex-col items-center group"\>  
                \<span class="text-xs font-bold text-gray-500 uppercase mb-1 group-hover:text-status-again transition"\>Again\</span\>  
                \<span class="text-lg font-bold text-status-again"\>1m\</span\>  
                \<span class="text-\[10px\] text-gray-600 mt-1 kbd"\>Key: 1\</span\>  
            \</button\>

            \<\!-- Hard \--\>  
            \<button class="srs-btn hover-hard p-3 rounded-xl flex flex-col items-center group"\>  
                \<span class="text-xs font-bold text-gray-500 uppercase mb-1 group-hover:text-status-hard transition"\>Hard\</span\>  
                \<span class="text-lg font-bold text-status-hard"\>2d\</span\>  
                \<span class="text-\[10px\] text-gray-600 mt-1 kbd"\>Key: 2\</span\>  
            \</button\>

            \<\!-- Good \--\>  
            \<button class="srs-btn hover-good p-3 rounded-xl flex flex-col items-center group border-brand-primary/30 bg-brand-primary/5"\> \<\!-- Default highlight? \--\>  
                \<span class="text-xs font-bold text-gray-500 uppercase mb-1 group-hover:text-status-good transition"\>Good\</span\>  
                \<span class="text-lg font-bold text-status-good"\>5d\</span\>  
                \<span class="text-\[10px\] text-gray-600 mt-1 kbd"\>Key: 3\</span\>  
            \</button\>

            \<\!-- Easy \--\>  
            \<button class="srs-btn hover-easy p-3 rounded-xl flex flex-col items-center group"\>  
                \<span class="text-xs font-bold text-gray-500 uppercase mb-1 group-hover:text-status-easy transition"\>Easy\</span\>  
                \<span class="text-lg font-bold text-status-easy"\>14d\</span\>  
                \<span class="text-\[10px\] text-gray-600 mt-1 kbd"\>Key: 4\</span\>  
            \</button\>

        \</div\>

        \<\!-- Undo Action \--\>  
        \<button class="mt-4 text-gray-500 hover:text-white text-xs flex items-center gap-2 transition opacity-50 hover:opacity-100"\>  
            \<i class="fas fa-undo"\>\</i\> Undo last action (Ctrl+Z)  
        \</button\>

    \</footer\>

\</body\>  
\</html\>

Этот дизайн фокусирует пользователя на контенте, используя пространство и типографику. Цвета кнопок соответствуют семантике FSRS (Красный \-\> Синий), но выполнены в стиле "неон на темном".

Мы проделали отличную работу. У нас готовы **Dashboard**, **Library**, **Card Editor** и **Study Session**.

Теперь нам нужно закрыть блок **Community (Маркетплейс)** и экран **входа в проект (Projects Hub)**.

Предлагаю следующий порядок:

1. **Marketplace (Витрина):** Как выглядит поиск и выбор курсов.  
2. **Product Page (Страница товара):** Детальная страница курса с отзывами и кнопкой покупки.  
3. **Projects Hub (Лаунчер):** Самый первый экран после логина, где мы выбираем язык.

Давай начнем с **Marketplace**. Это должна быть красивая витрина в стиле "магазина ассетов" (как в Unity Store или Leonardo Community Feed).

---

### **Экран: Marketplace (Витрина)**

**Особенности дизайна:**

* **Сетка:** Плотная сетка карточек с яркими обложками.  
* **Фильтры:** Сайдбар справа или слева (фасетный поиск: Цена, Уровень, Теги).  
* **Карточка товара:** Отличается от карточки колоды — есть **Цена**, **Рейтинг (Звезды)** и **Автор**.

Сохрани как marketplace.html.

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Marketplace\</title\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: { bg: '\#0B0F15', surface: '\#131927', hover: '\#1C2438', border: 'rgba(255, 255, 255, 0.08)' },  
                        brand: { primary: '\#8B5CF6', secondary: '\#3B82F6', pink: '\#EC4899', green: '\#10B981', yellow: '\#F59E0B' }  
                    },  
                    fontFamily: { sans: \['Inter', 'sans-serif'\] },  
                    boxShadow: { 'glow': '0 0 20px rgba(139, 92, 246, 0.15)' }  
                }  
            }  
        }  
    \</script\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');  
        ::-webkit-scrollbar { width: 6px; }  
        ::-webkit-scrollbar-track { background: \#0B0F15; }  
        ::-webkit-scrollbar-thumb { background: \#1C2438; border-radius: 3px; }  
        .glass { background: rgba(19, 25, 39, 0.7); backdrop-filter: blur(12px); border: 1px solid rgba(255, 255, 255, 0.08); }  
        .glass-tag { background: rgba(255, 255, 255, 0.1); backdrop-filter: blur(4px); border: 1px solid rgba(255, 255, 255, 0.1); }  
          
        .market-card { transition: all 0.3s ease; border: 1px solid rgba(255,255,255,0.05); }  
        .market-card:hover { transform: translateY(-4px); border-color: rgba(139, 92, 246, 0.4); box-shadow: 0 10px 40px \-10px rgba(0,0,0,0.5); }  
    \</style\>  
\</head\>  
\<body class="bg-app-bg text-gray-400 font-sans h-screen flex overflow-hidden"\>

    \<\!-- SIDEBAR (Placeholder) \--\>  
    \<aside class="w-\[260px\] bg-app-surface border-r border-app-border flex flex-col z-30"\>  
        \<div class="h-16 flex items-center px-6 border-b border-app-border"\>  
            \<div class="w-8 h-8 rounded-lg bg-gradient-to-br from-brand-primary to-brand-secondary flex items-center justify-center text-white font-bold text-lg mr-3 shadow-glow"\>P\</div\>  
            \<span class="text-white font-bold text-lg"\>PVS.ai\</span\>  
        \</div\>  
        \<div class="p-6"\>  
            \<div class="text-xs uppercase font-bold text-brand-primary mb-2"\>Marketplace Active\</div\>  
            \<div class="h-2 w-full bg-app-hover rounded mb-2"\>\</div\>  
        \</div\>  
    \</aside\>

    \<\!-- MAIN CONTENT \--\>  
    \<div class="flex-1 flex flex-col min-w-0 bg-app-bg relative"\>  
          
        \<\!-- HEADER \--\>  
        \<header class="h-20 glass border-b border-app-border flex items-center justify-between px-8 sticky top-0 z-20"\>  
            \<div class="flex items-center gap-4 flex-1"\>  
                \<div class="relative w-full max-w-xl"\>  
                    \<i class="fas fa-search absolute left-4 top-3 text-gray-500"\>\</i\>  
                    \<input type="text" placeholder="Search for decks, topics, or authors..."   
                           class="w-full bg-app-bg border border-app-border rounded-xl pl-11 pr-4 py-2.5 text-sm text-white focus:border-brand-primary focus:ring-1 focus:ring-brand-primary transition-all"\>  
                \</div\>  
            \</div\>  
              
            \<div class="flex items-center gap-4"\>  
                \<button class="flex items-center gap-2 text-sm text-gray-300 hover:text-white transition"\>  
                    \<i class="fas fa-filter"\>\</i\> Filters  
                \</button\>  
                \<div class="h-6 w-px bg-app-border"\>\</div\>  
                \<button class="bg-app-surface hover:bg-app-hover border border-app-border text-white px-4 py-2 rounded-lg text-sm font-medium transition"\>  
                    \<i class="fas fa-upload mr-2"\>\</i\> Sell Your Deck  
                \</button\>  
            \</div\>  
        \</header\>

        \<\!-- CONTENT AREA \--\>  
        \<div class="flex-1 flex overflow-hidden"\>  
              
            \<\!-- LEFT: FILTERS SIDEBAR \--\>  
            \<div class="w-64 bg-app-bg border-r border-app-border p-6 overflow-y-auto hidden lg:block"\>  
                  
                \<div class="mb-8"\>  
                    \<h3 class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-4"\>Category\</h3\>  
                    \<div class="space-y-2"\>  
                        \<label class="flex items-center gap-3 cursor-pointer group"\>  
                            \<input type="checkbox" checked class="w-4 h-4 rounded border-app-border bg-app-surface text-brand-primary focus:ring-offset-app-bg"\>  
                            \<span class="text-sm text-gray-300 group-hover:text-white transition"\>Languages\</span\>  
                        \</label\>  
                        \<label class="flex items-center gap-3 cursor-pointer group"\>  
                            \<input type="checkbox" class="w-4 h-4 rounded border-app-border bg-app-surface text-brand-primary focus:ring-offset-app-bg"\>  
                            \<span class="text-sm text-gray-300 group-hover:text-white transition"\>Medicine\</span\>  
                        \</label\>  
                        \<label class="flex items-center gap-3 cursor-pointer group"\>  
                            \<input type="checkbox" class="w-4 h-4 rounded border-app-border bg-app-surface text-brand-primary focus:ring-offset-app-bg"\>  
                            \<span class="text-sm text-gray-300 group-hover:text-white transition"\>Programming\</span\>  
                        \</label\>  
                    \</div\>  
                \</div\>

                \<div class="mb-8"\>  
                    \<h3 class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-4"\>Level (CEFR)\</h3\>  
                    \<div class="flex flex-wrap gap-2"\>  
                        \<button class="px-3 py-1 bg-brand-primary/20 text-brand-primary border border-brand-primary/30 rounded-lg text-xs font-bold"\>A1\</button\>  
                        \<button class="px-3 py-1 bg-app-surface text-gray-400 border border-app-border rounded-lg text-xs font-medium hover:text-white hover:border-gray-500 transition"\>A2\</button\>  
                        \<button class="px-3 py-1 bg-app-surface text-gray-400 border border-app-border rounded-lg text-xs font-medium hover:text-white hover:border-gray-500 transition"\>B1\</button\>  
                        \<button class="px-3 py-1 bg-app-surface text-gray-400 border border-app-border rounded-lg text-xs font-medium hover:text-white hover:border-gray-500 transition"\>B2\</button\>  
                    \</div\>  
                \</div\>

                \<div class="mb-8"\>  
                    \<h3 class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-4"\>Price\</h3\>  
                    \<div class="space-y-2"\>  
                        \<label class="flex items-center gap-3 cursor-pointer group"\>  
                            \<input type="radio" name="price" class="w-4 h-4 bg-app-surface border-app-border text-brand-primary focus:ring-offset-app-bg"\>  
                            \<span class="text-sm text-gray-300 group-hover:text-white transition"\>All\</span\>  
                        \</label\>  
                        \<label class="flex items-center gap-3 cursor-pointer group"\>  
                            \<input type="radio" name="price" class="w-4 h-4 bg-app-surface border-app-border text-brand-primary focus:ring-offset-app-bg"\>  
                            \<span class="text-sm text-gray-300 group-hover:text-white transition"\>Free Only\</span\>  
                        \</label\>  
                    \</div\>  
                \</div\>

            \</div\>

            \<\!-- RIGHT: GRID \--\>  
            \<main class="flex-1 overflow-y-auto p-8"\>  
                  
                \<div class="flex justify-between items-center mb-6"\>  
                    \<h2 class="text-lg font-bold text-white"\>Featured Courses\</h2\>  
                    \<div class="text-xs text-gray-500"\>Showing 142 results\</div\>  
                \</div\>

                \<div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 gap-6"\>

                    \<\!-- Product Card 1 \--\>  
                    \<div class="bg-app-surface rounded-xl overflow-hidden market-card group cursor-pointer relative"\>  
                        \<\!-- Cover \--\>  
                        \<div class="h-48 relative"\>  
                            \<img src="https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=600\&q=80" class="w-full h-full object-cover transition duration-700 group-hover:scale-105"\>  
                            \<div class="absolute inset-0 bg-gradient-to-t from-app-surface via-transparent to-transparent"\>\</div\>  
                              
                            \<\!-- Price Tag \--\>  
                            \<div class="absolute top-3 right-3 glass-tag px-2 py-1 rounded-lg text-white font-bold text-xs shadow-lg"\>  
                                $19.99  
                            \</div\>  
                            \<\!-- Verified Author Badge \--\>  
                            \<div class="absolute top-3 left-3 bg-brand-primary text-white text-\[10px\] font-bold px-2 py-0.5 rounded shadow-lg flex items-center gap-1"\>  
                                \<i class="fas fa-check-circle"\>\</i\> VERIFIED  
                            \</div\>  
                        \</div\>

                        \<\!-- Content \--\>  
                        \<div class="p-5 pt-2 relative"\>  
                            \<\!-- Avatar Overlap \--\>  
                            \<div class="absolute \-top-6 right-4"\>  
                                \<img src="https://i.pravatar.cc/150?u=1" class="w-10 h-10 rounded-full border-2 border-app-surface shadow-md"\>  
                            \</div\>

                            \<div class="flex gap-2 mb-2"\>  
                                \<span class="text-\[10px\] uppercase font-bold text-brand-secondary bg-brand-secondary/10 px-2 py-0.5 rounded"\>Business\</span\>  
                                \<span class="text-\[10px\] uppercase font-bold text-gray-400 bg-white/5 px-2 py-0.5 rounded"\>C1\</span\>  
                            \</div\>

                            \<h3 class="text-white font-bold text-lg mb-1 leading-tight group-hover:text-brand-primary transition"\>Advanced Business English\</h3\>  
                            \<p class="text-xs text-gray-500 mb-4 line-clamp-2"\>Master negotiations and corporate vocabulary with native audio.\</p\>  
                              
                            \<div class="flex items-center justify-between border-t border-app-border pt-3"\>  
                                \<div class="flex items-center gap-1 text-brand-yellow text-xs font-bold"\>  
                                    \<i class="fas fa-star"\>\</i\> 4.9 \<span class="text-gray-600 font-normal"\>(120)\</span\>  
                                \</div\>  
                                \<div class="flex items-center gap-1 text-gray-500 text-xs"\>  
                                    \<i class="fas fa-user-friends"\>\</i\> 2.5k  
                                \</div\>  
                            \</div\>  
                        \</div\>  
                    \</div\>

                    \<\!-- Product Card 2 \--\>  
                    \<div class="bg-app-surface rounded-xl overflow-hidden market-card group cursor-pointer relative"\>  
                        \<div class="h-48 relative"\>  
                            \<img src="https://images.unsplash.com/photo-1528164344705-47542687000d?w=600\&q=80" class="w-full h-full object-cover transition duration-700 group-hover:scale-105"\>  
                            \<div class="absolute inset-0 bg-gradient-to-t from-app-surface via-transparent to-transparent"\>\</div\>  
                              
                            \<div class="absolute top-3 right-3 bg-brand-green/20 border border-brand-green/30 text-brand-green px-2 py-1 rounded-lg font-bold text-xs shadow-lg backdrop-blur"\>  
                                FREE  
                            \</div\>  
                        \</div\>

                        \<div class="p-5 pt-2 relative"\>  
                            \<div class="absolute \-top-6 right-4"\>  
                                \<img src="https://i.pravatar.cc/150?u=2" class="w-10 h-10 rounded-full border-2 border-app-surface shadow-md"\>  
                            \</div\>

                            \<div class="flex gap-2 mb-2"\>  
                                \<span class="text-\[10px\] uppercase font-bold text-brand-pink bg-brand-pink/10 px-2 py-0.5 rounded"\>Kanji\</span\>  
                                \<span class="text-\[10px\] uppercase font-bold text-gray-400 bg-white/5 px-2 py-0.5 rounded"\>N5\</span\>  
                            \</div\>

                            \<h3 class="text-white font-bold text-lg mb-1 leading-tight group-hover:text-brand-pink transition"\>Japanese N5 Basic\</h3\>  
                            \<p class="text-xs text-gray-500 mb-4 line-clamp-2"\>Start your journey with the essential 100 Kanji. Includes stroke order.\</p\>  
                              
                            \<div class="flex items-center justify-between border-t border-app-border pt-3"\>  
                                \<div class="flex items-center gap-1 text-brand-yellow text-xs font-bold"\>  
                                    \<i class="fas fa-star"\>\</i\> 4.5 \<span class="text-gray-600 font-normal"\>(40)\</span\>  
                                \</div\>  
                                \<div class="flex items-center gap-1 text-gray-500 text-xs"\>  
                                    \<i class="fas fa-download"\>\</i\> 10k+  
                                \</div\>  
                            \</div\>  
                        \</div\>  
                    \</div\>

                    \<\!-- Product Card 3 \--\>  
                    \<div class="bg-app-surface rounded-xl overflow-hidden market-card group cursor-pointer relative"\>  
                        \<div class="h-48 relative bg-gradient-to-br from-indigo-900 to-purple-900 flex items-center justify-center"\>  
                            \<i class="fas fa-code text-6xl text-white/10"\>\</i\>  
                            \<div class="absolute inset-0 bg-gradient-to-t from-app-surface via-transparent to-transparent"\>\</div\>  
                              
                            \<div class="absolute top-3 right-3 glass-tag px-2 py-1 rounded-lg text-white font-bold text-xs shadow-lg"\>  
                                $5.00  
                            \</div\>  
                        \</div\>

                        \<div class="p-5 pt-2 relative"\>  
                            \<div class="absolute \-top-6 right-4"\>  
                                \<img src="https://i.pravatar.cc/150?u=3" class="w-10 h-10 rounded-full border-2 border-app-surface shadow-md"\>  
                            \</div\>

                            \<div class="flex gap-2 mb-2"\>  
                                \<span class="text-\[10px\] uppercase font-bold text-blue-400 bg-blue-500/10 px-2 py-0.5 rounded"\>IT / Tech\</span\>  
                            \</div\>

                            \<h3 class="text-white font-bold text-lg mb-1 leading-tight group-hover:text-blue-400 transition"\>Python for Data Science\</h3\>  
                            \<p class="text-xs text-gray-500 mb-4 line-clamp-2"\>English vocabulary specifically for DS/ML engineers.\</p\>  
                              
                            \<div class="flex items-center justify-between border-t border-app-border pt-3"\>  
                                \<div class="flex items-center gap-1 text-brand-yellow text-xs font-bold"\>  
                                    \<i class="fas fa-star"\>\</i\> 5.0 \<span class="text-gray-600 font-normal"\>(12)\</span\>  
                                \</div\>  
                                \<div class="flex items-center gap-1 text-gray-500 text-xs"\>  
                                    \<i class="fas fa-user-friends"\>\</i\> 300  
                                \</div\>  
                            \</div\>  
                        \</div\>  
                    \</div\>

                \</div\>  
            \</main\>

        \</div\>  
    \</div\>  
\</body\>  
\</html\>

Следующий шаг — **Product Page** (страница товара), куда пользователь попадает при клике на карточку. Делаем?

Отлично. Переходим к **Product Page (Страница товара)**.

Это страница с высокой конверсией. Здесь пользователь принимает решение: купить или уйти.  
В стиле **Leonardo**, мы сделаем акцент на визуальную привлекательность (большая обложка, эффектное размытие) и четкую иерархию информации.

### **Особенности дизайна:**

* **Sticky Sidebar:** Правая колонка с ценой и кнопкой "Купить" фиксируется при прокрутке, чтобы всегда быть под рукой.  
* **Hero Image:** Огромная, красивая обложка с градиентным переходом в фон.  
* **Smart Preview:** Кнопка демо-режима выделена, но не перебивает кнопку покупки.  
* **Вкладки:** Описание / Содержание / Отзывы — чтобы не перегружать страницу длинной "простыней".

Сохрани этот код как product\_page.html.

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Product Page\</title\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: { bg: '\#0B0F15', surface: '\#131927', hover: '\#1C2438', border: 'rgba(255, 255, 255, 0.08)' },  
                        brand: { primary: '\#8B5CF6', secondary: '\#3B82F6', pink: '\#EC4899', green: '\#10B981', yellow: '\#F59E0B' }  
                    },  
                    fontFamily: { sans: \['Inter', 'sans-serif'\] },  
                    boxShadow: { 'glow': '0 0 30px rgba(139, 92, 246, 0.2)' }  
                }  
            }  
        }  
    \</script\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');  
        ::-webkit-scrollbar { width: 6px; }  
        ::-webkit-scrollbar-track { background: \#0B0F15; }  
        ::-webkit-scrollbar-thumb { background: \#1C2438; border-radius: 3px; }  
          
        .glass { background: rgba(19, 25, 39, 0.7); backdrop-filter: blur(12px); border: 1px solid rgba(255, 255, 255, 0.08); }  
        .gradient-btn { background: linear-gradient(135deg, \#8B5CF6 0%, \#3B82F6 100%); }  
        .gradient-btn:hover { filter: brightness(1.1); box-shadow: 0 0 20px rgba(139, 92, 246, 0.4); }  
          
        /\* Tab Active State \*/  
        .tab-active { color: white; border-bottom: 2px solid \#8B5CF6; }  
        .tab-inactive { color: \#6B7280; border-bottom: 2px solid transparent; }  
        .tab-inactive:hover { color: \#D1D5DB; }  
    \</style\>  
\</head\>  
\<body class="bg-app-bg text-gray-300 font-sans min-h-screen flex flex-col"\>

    \<\!-- HEADER (Simplified) \--\>  
    \<header class="h-16 glass border-b border-app-border flex items-center px-8 z-30 sticky top-0"\>  
        \<div class="flex items-center gap-4"\>  
            \<a href="\#" class="w-8 h-8 rounded-lg bg-gradient-to-br from-brand-primary to-brand-secondary flex items-center justify-center text-white font-bold text-lg mr-3 shadow-glow"\>P\</a\>  
            \<div class="h-6 w-px bg-white/10"\>\</div\>  
            \<a href="\#" class="text-sm font-medium hover:text-white transition flex items-center gap-2"\>  
                \<i class="fas fa-arrow-left"\>\</i\> Back to Marketplace  
            \</a\>  
        \</div\>  
        \<div class="flex-1"\>\</div\>  
        \<div class="flex items-center gap-4"\>  
            \<img src="https://i.pravatar.cc/150?u=1" class="w-8 h-8 rounded-full border border-gray-600"\>  
        \</div\>  
    \</header\>

    \<\!-- HERO BACKGROUND \--\>  
    \<div class="w-full h-96 absolute top-0 left-0 z-0 overflow-hidden"\>  
        \<img src="https://images.unsplash.com/photo-1543269865-cbf427effbad?w=1600\&q=80" class="w-full h-full object-cover opacity-20 blur-xl"\>  
        \<div class="absolute inset-0 bg-gradient-to-b from-app-bg/80 via-app-bg/90 to-app-bg"\>\</div\>  
    \</div\>

    \<\!-- MAIN CONTENT \--\>  
    \<main class="flex-1 w-full max-w-7xl mx-auto px-6 py-12 relative z-10 grid grid-cols-1 lg:grid-cols-3 gap-12"\>

        \<\!-- LEFT COLUMN: Product Info \--\>  
        \<div class="lg:col-span-2 space-y-10"\>  
              
            \<\!-- Product Header \--\>  
            \<div class="flex gap-6 items-start"\>  
                \<div class="w-32 h-32 rounded-2xl overflow-hidden border border-white/10 shadow-2xl flex-shrink-0"\>  
                    \<img src="https://images.unsplash.com/photo-1543269865-cbf427effbad?w=400\&q=80" class="w-full h-full object-cover"\>  
                \</div\>  
                \<div\>  
                    \<div class="flex gap-2 mb-3"\>  
                        \<span class="px-2 py-1 rounded bg-brand-secondary/20 text-brand-secondary text-xs font-bold border border-brand-secondary/30"\>BUSINESS\</span\>  
                        \<span class="px-2 py-1 rounded bg-white/5 text-gray-400 text-xs font-bold border border-white/10"\>ENGLISH C1\</span\>  
                    \</div\>  
                    \<h1 class="text-4xl font-bold text-white mb-2 leading-tight"\>Advanced Business English Pro\</h1\>  
                    \<div class="flex items-center gap-4 text-sm"\>  
                        \<div class="flex items-center text-brand-yellow"\>  
                            \<i class="fas fa-star"\>\</i\>  
                            \<i class="fas fa-star"\>\</i\>  
                            \<i class="fas fa-star"\>\</i\>  
                            \<i class="fas fa-star"\>\</i\>  
                            \<i class="fas fa-star-half-alt"\>\</i\>  
                            \<span class="ml-1 text-white font-bold"\>4.8\</span\>  
                            \<span class="ml-1 text-gray-500"\>(124 reviews)\</span\>  
                        \</div\>  
                        \<div class="text-gray-500"\>•\</div\>  
                        \<div class="text-gray-400"\>2,530 students enrolled\</div\>  
                    \</div\>  
                \</div\>  
            \</div\>

            \<\!-- Tabs \--\>  
            \<div class="border-b border-app-border"\>  
                \<nav class="flex gap-8"\>  
                    \<button class="pb-4 text-sm font-medium tab-active transition"\>Description\</button\>  
                    \<button class="pb-4 text-sm font-medium tab-inactive transition"\>Card List (2000)\</button\>  
                    \<button class="pb-4 text-sm font-medium tab-inactive transition"\>Reviews\</button\>  
                \</nav\>  
            \</div\>

            \<\!-- Tab Content \--\>  
            \<div class="prose prose-invert max-w-none text-gray-300"\>  
                \<p\>Master the vocabulary needed for high-stakes business meetings, contract negotiations, and corporate strategy. This deck is curated from real-world business cases and Wall Street Journal articles.\</p\>  
                  
                \<h3 class="text-white"\>What you'll learn\</h3\>  
                \<ul class="space-y-2 list-none pl-0"\>  
                    \<li class="flex gap-3 items-center"\>  
                        \<i class="fas fa-check-circle text-brand-green"\>\</i\>  
                        \<span\>500+ advanced idioms for negotiations\</span\>  
                    \</li\>  
                    \<li class="flex gap-3 items-center"\>  
                        \<i class="fas fa-check-circle text-brand-green"\>\</i\>  
                        \<span\>Email writing templates and phrases\</span\>  
                    \</li\>  
                    \<li class="flex gap-3 items-center"\>  
                        \<i class="fas fa-check-circle text-brand-green"\>\</i\>  
                        \<span\>Native audio for every single card (US & UK accents)\</span\>  
                    \</li\>  
                \</ul\>

                \<h3 class="text-white mt-8"\>Sample Cards\</h3\>  
                \<\!-- Mini Card Preview \--\>  
                \<div class="grid grid-cols-2 gap-4 not-prose"\>  
                    \<div class="glass p-4 rounded-xl border border-white/5"\>  
                        \<div class="text-xs text-gray-500 uppercase font-bold mb-2"\>Front\</div\>  
                        \<div class="text-white text-lg"\>"Let's \<span class="text-brand-primary"\>table\</span\> this discussion."\</div\>  
                    \</div\>  
                    \<div class="glass p-4 rounded-xl border border-white/5"\>  
                        \<div class="text-xs text-gray-500 uppercase font-bold mb-2"\>Back\</div\>  
                        \<div class="text-gray-300"\>Отложить обсуждение (на потом).\</div\>  
                    \</div\>  
                \</div\>  
            \</div\>

            \<\!-- Reviews Section Preview \--\>  
            \<div\>  
                \<h3 class="text-xl font-bold text-white mb-6"\>Student Reviews\</h3\>  
                \<div class="space-y-4"\>  
                    \<\!-- Review 1 \--\>  
                    \<div class="bg-app-surface p-5 rounded-xl border border-app-border"\>  
                        \<div class="flex justify-between items-start mb-2"\>  
                            \<div class="flex items-center gap-3"\>  
                                \<div class="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center text-xs font-bold text-white"\>JD\</div\>  
                                \<div\>  
                                    \<div class="text-sm font-bold text-white"\>John Doe\</div\>  
                                    \<div class="text-\[10px\] text-brand-green flex items-center gap-1"\>\<i class="fas fa-check-circle"\>\</i\> Verified Purchase\</div\>  
                                \</div\>  
                            \</div\>  
                            \<div class="text-brand-yellow text-xs"\>★★★★★\</div\>  
                        \</div\>  
                        \<p class="text-sm text-gray-400"\>The audio quality is amazing. Really helped me with my presentation last week.\</p\>  
                    \</div\>  
                      
                    \<\!-- Review 2 \--\>  
                    \<div class="bg-app-surface p-5 rounded-xl border border-app-border"\>  
                        \<div class="flex justify-between items-start mb-2"\>  
                            \<div class="flex items-center gap-3"\>  
                                \<img src="https://i.pravatar.cc/150?u=5" class="w-8 h-8 rounded-full"\>  
                                \<div\>  
                                    \<div class="text-sm font-bold text-white"\>Sarah Smith\</div\>  
                                    \<div class="text-\[10px\] text-brand-green flex items-center gap-1"\>\<i class="fas fa-check-circle"\>\</i\> Verified Purchase\</div\>  
                                \</div\>  
                            \</div\>  
                            \<div class="text-brand-yellow text-xs"\>★★★★☆\</div\>  
                        \</div\>  
                        \<p class="text-sm text-gray-400"\>Great content, but I wish there were more examples for the finance section.\</p\>  
                    \</div\>  
                \</div\>  
            \</div\>

        \</div\>

        \<\!-- RIGHT COLUMN: Sticky Action Card \--\>  
        \<div class="lg:col-span-1"\>  
            \<div class="sticky top-24"\>  
                  
                \<\!-- Purchase Card \--\>  
                \<div class="glass p-6 rounded-2xl border border-brand-primary/20 shadow-glow"\>  
                    \<div class="flex justify-between items-end mb-6"\>  
                        \<div\>  
                            \<div class="text-xs text-gray-500 uppercase font-bold mb-1"\>One-time payment\</div\>  
                            \<div class="text-4xl font-bold text-white"\>$19.99\</div\>  
                        \</div\>  
                    \</div\>

                    \<button class="w-full gradient-btn text-white font-bold py-3.5 rounded-xl mb-3 shadow-lg flex items-center justify-center gap-2 transition transform active:scale-95"\>  
                        Get Access Now  
                    \</button\>  
                      
                    \<button class="w-full bg-white/5 hover:bg-white/10 text-white font-medium py-3 rounded-xl border border-white/10 transition flex items-center justify-center gap-2 group"\>  
                        \<i class="fas fa-eye text-brand-primary group-hover:scale-110 transition"\>\</i\>  
                        Smart Preview (10 Cards)  
                    \</button\>

                    \<div class="mt-6 pt-6 border-t border-white/10 space-y-3"\>  
                        \<div class="flex items-center gap-3 text-sm text-gray-400"\>  
                            \<i class="fas fa-infinity text-brand-primary w-5 text-center"\>\</i\> Lifetime access  
                        \</div\>  
                        \<div class="flex items-center gap-3 text-sm text-gray-400"\>  
                            \<i class="fas fa-sync-alt text-brand-primary w-5 text-center"\>\</i\> Free updates  
                        \</div\>  
                        \<div class="flex items-center gap-3 text-sm text-gray-400"\>  
                            \<i class="fas fa-mobile-alt text-brand-primary w-5 text-center"\>\</i\> Mobile & Web  
                        \</div\>  
                    \</div\>  
                \</div\>

                \<\!-- Author Box \--\>  
                \<div class="mt-6 bg-app-surface p-4 rounded-xl border border-app-border flex items-center gap-4"\>  
                    \<img src="https://i.pravatar.cc/150?u=1" class="w-12 h-12 rounded-full border-2 border-brand-primary"\>  
                    \<div class="flex-1"\>  
                        \<div class="text-xs text-gray-500 uppercase font-bold"\>Created by\</div\>  
                        \<div class="text-white font-bold hover:text-brand-primary cursor-pointer transition"\>Elena English\</div\>  
                        \<div class="text-xs text-gray-400"\>Top Rated Seller\</div\>  
                    \</div\>  
                    \<button class="text-gray-400 hover:text-white"\>\<i class="fas fa-envelope"\>\</i\>\</button\>  
                \</div\>

            \</div\>  
        \</div\>

    \</main\>

\</body\>  
\</html\>

### **Что мы имеем в итоге:**

1. **Профессиональный вид:** Темная тема, стекло, акценты — выглядит как дорогой продукт.  
2. **Конверсия:** Кнопка покупки яркая, цена понятная, отзывы и "Verified Purchase" повышают доверие.  
3. **Smart Preview:** Кнопка для демо-режима доступна, но вторична по отношению к покупке.  
4. **Author Identity:** Мы показываем лицо автора, что важно для Creator Economy.

Теперь у нас есть **полный набор макетов (5 штук)** для старта разработки фронтенда:

1. **Layout** (Sidebar).  
2. **Projects** (Launcher).  
3. **Dashboard** (Home).  
4. **Library** (Management).  
5. **Card Editor** (Creation).  
6. **Product Page** (Sales).  
7. *(Study Session \- есть код в предыдущих шагах, можно обновить цвета).*

**На этом этап дизайна можно считать завершенным.** Твои разработчики (или ты) могут брать эти HTML/Tailwind файлы и переносить их в React/Vue компоненты.

Отлично. Это финальный элемент нашего пазла UI — точка входа в приложение.

**Экран: Projects Hub (Лаунчер)**

Здесь нет Сайдбара, так как мы еще не выбрали контекст (язык). Этот экран должен выглядеть как «Врата» или меню выбора персонажа в игре.

### **Дизайн-концепция:**

* **Минимализм:** Фокус только на выборе.  
* **Карточки Проектов:** Большие, с яркими "аватарками" (флагами или символами).  
* **Геймификация:** Сразу видно уровень (CEFR) и прогресс-бар, чтобы пользователь захотел "прокачать" конкретный язык.

Сохрани этот код как projects.html.

code Html  
play\_circledownloadcontent\_copy  
expand\_less  
\<\!DOCTYPE html\>  
\<html lang="en"\>  
\<head\>  
    \<meta charset="UTF-8"\>  
    \<meta name="viewport" content="width=device-width, initial-scale=1.0"\>  
    \<title\>PVS \- Select Project\</title\>  
    \<script src="https://cdn.tailwindcss.com"\>\</script\>  
    \<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"\>  
    \<script\>  
        tailwind.config \= {  
            theme: {  
                extend: {  
                    colors: {  
                        app: { bg: '\#0B0F15', surface: '\#131927', hover: '\#1C2438', border: 'rgba(255, 255, 255, 0.08)' },  
                        brand: { primary: '\#8B5CF6', secondary: '\#3B82F6', pink: '\#EC4899', green: '\#10B981', red: '\#EF4444' }  
                    },  
                    fontFamily: { sans: \['Inter', 'sans-serif'\] }  
                }  
            }  
        }  
    \</script\>  
    \<style\>  
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700\&display=swap');  
          
        body {   
            background-color: \#0B0F15;   
            background-image: radial-gradient(circle at 50% 0%, \#1c2339 0%, \#0b0f19 60%);  
            background-attachment: fixed;  
        }

        .project-card {  
            background: rgba(19, 25, 39, 0.6);  
            backdrop-filter: blur(12px);  
            border: 1px solid rgba(255, 255, 255, 0.08);  
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);  
        }  
          
        .project-card:hover {  
            transform: translateY(-8px);  
            border-color: rgba(139, 92, 246, 0.5);  
            box-shadow: 0 20px 40px \-10px rgba(139, 92, 246, 0.15);  
        }

        /\* Анимация появления \*/  
        @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }  
        .fade-in { animation: fadeIn 0.5s ease-out forwards; }  
    \</style\>  
\</head\>  
\<body class="text-gray-300 font-sans min-h-screen flex flex-col"\>

    \<\!-- 1\. TOP NAV (Global) \--\>  
    \<nav class="w-full h-20 flex items-center justify-center border-b border-white/5 bg-app-bg/50 backdrop-blur-sm sticky top-0 z-20"\>  
        \<div class="w-full max-w-6xl px-6 flex items-center justify-between"\>  
            \<\!-- Logo \--\>  
            \<div class="flex items-center gap-3"\>  
                \<div class="w-9 h-9 rounded-xl bg-gradient-to-br from-brand-primary to-brand-secondary flex items-center justify-center text-white font-bold shadow-\[0\_0\_15px\_rgba(139,92,246,0.4)\]"\>P\</div\>  
                \<span class="font-bold text-xl tracking-tight text-white"\>PVS.ai\</span\>  
            \</div\>

            \<\!-- User Profile \--\>  
            \<div class="flex items-center gap-6"\>  
                \<button class="text-sm font-medium hover:text-white transition"\>Docs\</button\>  
                \<button class="text-sm font-medium hover:text-white transition"\>Support\</button\>  
                \<div class="h-6 w-px bg-white/10"\>\</div\>  
                \<div class="flex items-center gap-3 cursor-pointer hover:opacity-80 transition"\>  
                    \<div class="text-right hidden sm:block"\>  
                        \<div class="text-sm font-bold text-white"\>Kamil Karatov\</div\>  
                        \<div class="text-xs text-gray-500"\>Pro Plan\</div\>  
                    \</div\>  
                    \<img src="https://i.pravatar.cc/150?u=1" class="w-10 h-10 rounded-full border border-gray-600"\>  
                \</div\>  
            \</div\>  
        \</div\>  
    \</nav\>

    \<\!-- 2\. MAIN CONTENT \--\>  
    \<main class="flex-1 w-full max-w-6xl mx-auto px-6 py-12 flex flex-col fade-in"\>  
          
        \<\!-- Welcome Header \--\>  
        \<div class="flex flex-col md:flex-row justify-between items-end mb-12 gap-6"\>  
            \<div\>  
                \<h1 class="text-4xl font-bold text-white mb-3"\>Welcome back\</h1\>  
                \<p class="text-gray-400"\>Select a project to continue your learning session.\</p\>  
            \</div\>  
              
            \<\!-- Global Stats Widget \--\>  
            \<div class="flex gap-4"\>  
                \<div class="px-5 py-3 rounded-xl bg-app-surface border border-white/5 flex flex-col items-center"\>  
                    \<span class="text-\[10px\] uppercase font-bold text-gray-500 tracking-wider"\>Total Words\</span\>  
                    \<span class="text-xl font-bold text-white"\>2,850\</span\>  
                \</div\>  
                \<div class="px-5 py-3 rounded-xl bg-app-surface border border-white/5 flex flex-col items-center"\>  
                    \<span class="text-\[10px\] uppercase font-bold text-gray-500 tracking-wider"\>Day Streak\</span\>  
                    \<span class="text-xl font-bold text-brand-secondary flex items-center gap-1"\>\<i class="fas fa-fire"\>\</i\> 12\</span\>  
                \</div\>  
            \</div\>  
        \</div\>

        \<\!-- 3\. PROJECTS GRID \--\>  
        \<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8"\>

            \<\!-- Card 1: English (Active) \--\>  
            \<div class="project-card rounded-2xl overflow-hidden group cursor-pointer relative h-\[320px\] flex flex-col"\>  
                \<\!-- Top Gradient \--\>  
                \<div class="absolute top-0 left-0 w-full h-32 bg-gradient-to-b from-brand-secondary/20 to-transparent opacity-50 group-hover:opacity-80 transition"\>\</div\>  
                  
                \<div class="p-8 relative z-10 flex flex-col h-full"\>  
                    \<\!-- Icon \--\>  
                    \<div class="flex justify-between items-start mb-6"\>  
                        \<div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-600 to-indigo-900 flex items-center justify-center text-4xl shadow-lg border border-white/10 group-hover:scale-110 transition duration-300"\>  
                            🇬🇧  
                        \</div\>  
                        \<div class="px-2 py-1 rounded bg-green-500/10 border border-green-500/20 text-green-400 text-\[10px\] font-bold uppercase tracking-wider"\>  
                            Active  
                        \</div\>  
                    \</div\>

                    \<\!-- Info \--\>  
                    \<h2 class="text-2xl font-bold text-white mb-1 group-hover:text-brand-secondary transition"\>English\</h2\>  
                    \<p class="text-sm text-gray-400 mb-6"\>Advanced C1 • Business\</p\>

                    \<\!-- Progress \--\>  
                    \<div class="mt-auto"\>  
                        \<div class="flex justify-between text-xs text-gray-300 mb-2"\>  
                            \<span\>Level Progress (C1)\</span\>  
                            \<span class="text-white font-bold"\>85%\</span\>  
                        \</div\>  
                        \<div class="w-full h-2 bg-app-bg rounded-full overflow-hidden mb-4 border border-white/5"\>  
                            \<div class="h-full bg-gradient-to-r from-brand-secondary to-brand-primary w-\[85%\] shadow-\[0\_0\_10px\_rgba(59,130,246,0.5)\]"\>\</div\>  
                        \</div\>

                        \<\!-- Footer \--\>  
                        \<div class="flex items-center justify-between pt-4 border-t border-white/5"\>  
                            \<div class="text-xs text-gray-500 flex items-center gap-1"\>  
                                \<i class="fas fa-layer-group"\>\</i\> 12 Decks  
                            \</div\>  
                            \<span class="text-sm font-bold text-white group-hover:translate-x-1 transition flex items-center gap-2"\>  
                                Open \<i class="fas fa-arrow-right text-brand-secondary"\>\</i\>  
                            \</span\>  
                        \</div\>  
                    \</div\>  
                \</div\>  
            \</div\>

            \<\!-- Card 2: Japanese (Learning) \--\>  
            \<div class="project-card rounded-2xl overflow-hidden group cursor-pointer relative h-\[320px\] flex flex-col"\>  
                \<div class="absolute top-0 left-0 w-full h-32 bg-gradient-to-b from-brand-red/20 to-transparent opacity-50 group-hover:opacity-80 transition"\>\</div\>  
                  
                \<div class="p-8 relative z-10 flex flex-col h-full"\>  
                    \<div class="flex justify-between items-start mb-6"\>  
                        \<div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-red-700 to-orange-900 flex items-center justify-center text-4xl shadow-lg border border-white/10 group-hover:scale-110 transition duration-300"\>  
                            🇯🇵  
                        \</div\>  
                    \</div\>

                    \<h2 class="text-2xl font-bold text-white mb-1 group-hover:text-brand-red transition"\>Japanese\</h2\>  
                    \<p class="text-sm text-gray-400 mb-6"\>Beginner N5 • Kanji\</p\>

                    \<div class="mt-auto"\>  
                        \<div class="flex justify-between text-xs text-gray-300 mb-2"\>  
                            \<span\>Level Progress (N5)\</span\>  
                            \<span class="text-white font-bold"\>35%\</span\>  
                        \</div\>  
                        \<div class="w-full h-2 bg-app-bg rounded-full overflow-hidden mb-4 border border-white/5"\>  
                            \<div class="h-full bg-gradient-to-r from-red-500 to-orange-500 w-\[35%\] shadow-\[0\_0\_10px\_rgba(239,68,68,0.5)\]"\>\</div\>  
                        \</div\>

                        \<div class="flex items-center justify-between pt-4 border-t border-white/5"\>  
                            \<div class="text-xs text-gray-500 flex items-center gap-1"\>  
                                \<i class="fas fa-layer-group"\>\</i\> 4 Decks  
                            \</div\>  
                            \<span class="text-sm font-bold text-white group-hover:translate-x-1 transition flex items-center gap-2"\>  
                                Open \<i class="fas fa-arrow-right text-brand-red"\>\</i\>  
                            \</span\>  
                        \</div\>  
                    \</div\>  
                \</div\>  
            \</div\>

            \<\!-- Card 3: Create New \--\>  
            \<div class="rounded-2xl border-2 border-dashed border-white/10 hover:border-brand-primary/50 hover:bg-white/5 transition duration-300 cursor-pointer flex flex-col items-center justify-center h-\[320px\] group"\>  
                \<div class="w-20 h-20 rounded-full bg-app-surface border border-white/5 flex items-center justify-center mb-6 group-hover:scale-110 transition duration-300 group-hover:border-brand-primary/50 shadow-lg"\>  
                    \<i class="fas fa-plus text-3xl text-gray-500 group-hover:text-brand-primary transition"\>\</i\>  
                \</div\>  
                \<h3 class="text-xl font-bold text-white mb-2 group-hover:text-brand-primary transition"\>New Project\</h3\>  
                \<p class="text-sm text-gray-500 text-center max-w-\[200px\] leading-relaxed"\>Start learning a new language. We'll set up the best SRS settings.\</p\>  
            \</div\>

        \</div\>

    \</main\>

    \<\!-- Footer \--\>  
    \<footer class="w-full border-t border-white/5 py-6 mt-auto"\>  
        \<div class="max-w-6xl mx-auto px-6 text-center text-xs text-gray-600"\>  
            \&copy; 2025 PVS.ai. All rights reserved.  
        \</div\>  
    \</footer\>

\</body\>  
\</html\>

### **Итог этапа дизайна**

Мы создали полный комплект макетов для фронтенда:

1. **Projects Hub** — Вход.  
2. **Layout** — Скелет (Сайдбар, Хедер).  
3. **Dashboard** — Главная.  
4. **Library** — Управление.  
5. **Card Editor** — Создание.  
6. **Study Session** — Обучение.  
7. **Marketplace & Product** — Коммерция.

Все страницы используют единую цветовую палитру (app-bg, glass), шрифты и принципы (акцентные градиенты, карточки с эффектом стекла).

