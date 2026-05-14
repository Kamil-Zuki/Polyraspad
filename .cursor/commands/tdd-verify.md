# TDD Verify — Проверка покрытия и фиксация рисков

Используй эту команду перед завершением задачи.

## Чеклист

### 1. Покрытие изменённого поведения

#### Backend
- [ ] Каждое изменение публичного метода имеет тест
- [ ] DTO/gRPC контракты проверены (integration test)
- [ ] Миграции протестированы (если есть)
- [ ] Запустить `dotnet test` — все тесты проходят:
```bash
dotnet test --filter "FullyQualifiedName~<ServiceName>"
# или
dotnet test --no-build --verbosity normal
```

#### Frontend
- [ ] Компоненты/страницы с новой логикой имеют тесты
- [ ] API клиенты проверены
- [ ] Запустить `npm test` — все тесты проходят:
```bash
npm test -- --testPathPattern=<component-name>
npm test -- --watchAll=false
```

### 2. LingQ-specific регрессии (если Reader/Vocabulary)

- [ ] Тест: `sleep` и `slept` — разные статусы
- [ ] Тест: `go` и `went` — не дубли карточек
- [ ] Тест: точная фраза "take off" — дубль отдельных слов
- [ ] Тест: перелистывание с включённой настройкой переводит NEW → KNOWN
- [ ] Тест: создание карточки сохраняет точную форму

### 3. Соответствие правилам
- [ ] Проверить `.cursor/rules/02-tdd-testing-policy.mdc`
- [ ] Проверить `.cursor/rules/06-lingq-domain-guardrails.mdc` (если применимо)

### 4. Residual Risk Assessment

Если не удалось протестировать что-то (время, сложность, зависимости):

- [ ] Задокументировать риск в комментарии или плане
- [ ] Указать mitigation (ручная проверка, мониторинг)
- [ ] Пример записи:
```
RISK: Integration with external translation service not tested
MITIGATION: Manual verification performed, error handling covered in unit tests
```

### 5. Сборка и базовая проверка

- [ ] Backend собирается без ошибок:
```bash
dotnet build
```
- [ ] Frontend собирается без ошибок:
```bash
npm run build
```
- [ ] Нет новых warning'ов от анализаторов

### 6. Чистота изменений
- [ ] Нет отладочного кода (console.log, Debugger.Break)
- [ ] Нет закомментированного мёртвого кода
- [ ] Нет изменений вне scope задачи
- [ ] Сохранены несвязанные изменения пользователя

### 7. Финальный коммит
- [ ] Все файлы добавлены в индекс
- [ ] Сообщение коммита описывает что и почему
- [ ] Ссылка на issue/план если есть

## Шаблон сообщения коммита

```
feat(scope): краткое описание

- детали изменения
- тесты: что покрыто
- риски: что не протестировано и почему

Refs: #<issue-number>
```
