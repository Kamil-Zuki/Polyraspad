Как делают топовые конкуренты
🔹 LingQ
показывает структуру книги (chapters)
сохраняет формат
есть прогресс по книге
слова интегрированы, но не ломают текст
🔹 Language Reactor
контент — первичен
обучение — вторичный слой поверх

👉 Это ключевая идея, которую тебе нужно принять:

💡 Reader ≠ flashcard tool
💡 Reader = immersive environment + learning overlay

🔥 Как улучшить твой Reader (конкретно)

1. 🧭 Добавь структуру книги (это must-have)
   Что сделать:
   Sidebar / Drawer:
   Chapters (оглавление)
   текущая глава подсвечена

Breadcrumb:

Atomic Habits > Chapter 3 > Page 42
UX:
клик → переход к главе
scroll sync (как в Notion) 2. 🖼 Верни оригинальный layout (критично)

Сейчас ты:

превращаешь PDF → plain text → теряешь UX

Нужно:
HTML renderer вместо plain текста
поддержка:
изображений
заголовков
списков
абзацев

👉 Минимум:

сохранить <p>, <h1>, <img>

👉 Идеально:

PDF → HTML (pdf.js + кастомная разметка) 3. 📍 Прогресс и позиция (очень влияет на retention)

Добавь:

Progress bar:

███████░░░░░ 65%
Метрики:
% книги
время чтения
words read
“Continue reading”:
→ открывает последнюю позицию 4. 🎯 Улучши UX выделения слов (очень важно)

Сейчас:

текст превращается в “синий шум”

Проблема:

слишком много подсветки → мозг перегружается

Решение:
🔹 режимы отображения:
🔘 Focus mode:
→ подсвечивается только 1 слово (по клику)
🔘 Learning mode:
→ подсветка новых слов
🔘 Clean mode:
→ вообще без подсветки
🔹 hover UX:

tooltip:

word → translation + save
без клика → не ломает поток 5. 🧘 Сделай режим “глубокого чтения”

Это твой killer feature может быть.

Focus Mode:
скрыть sidebar
центрировать текст
увеличить line-height
убрать UI шум

👉 как:

Linear
Notion reader vibe 6. 🧠 Контекст слов (сейчас у тебя этого нет)

Сейчас:

слово = отдельная единица

Но нужно:

слово = часть предложения

Добавь:
sentence highlight при клике
перевод всей фразы
“Add sentence to deck”

👉 это делает Readlang очень хорошо

7. 📚 Smart chapters (AI-фича)

Если PDF без структуры:

👉 делай:

авто-разбиение на главы через AI
summary главы 8. 🔄 Reader ↔ Flashcards связь

Сейчас у тебя есть Studio — это супер.

Но нужно:

👉 из reader:

“Add word → deck”
“Add sentence → deck”
auto-tag:
книга
глава 9. 🎬 Сделай reader “живым”

Вот где ты можешь обогнать всех:

💡 идеи:
аудио + подсветка текста
автоскролл как субтитры
“play text”

👉 это уровень Lingopie

🧱 Как бы я переписал твой Reader (архитектура)
3 слоя:
[ Content Layer ]
→ HTML / PDF / text

[ Interaction Layer ]
→ selection, hover, click

[ Learning Layer ]
→ vocab, SRS, stats

👉 Сейчас у тебя:

Learning layer ломает content layer

👉 Нужно:

Learning layer поверх content
⚡ Быстрые wins (что сделать в первую очередь)

Если не хочешь распыляться:

🔥 TOP 5:
Добавить chapters + sidebar
Вернуть форматирование (HTML)
Сделать progress bar
Упростить подсветку слов
Добавить sentence-level UX
