# Живой персонаж в лобби — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить дыхание, моргание, три жеста ожидания, приветствие и реакции на мышь и цвет, сохранив вращение героя и удобство лобби.

**Architecture:** Двигаем связанные детали нынешней модели через группы с точками поворота. `CharacterLife` выбирает единственный крупный жест, `CharacterFace` изменяет голову и глаза, `CharacterGestures` — тело и конечности. `CharacterIdle` сохраняет управление корнем, а камера использует постоянную область всех допустимых поз.

**Tech Stack:** Unity 2022.3.62f3, C#, Built-in Render Pipeline, uGUI, Unity Test Framework, существующий Unity MCP; новых пакетов нет.

**Spec:** [Согласованное описание](../specs/2026-10-10-character-life-design.md).

## Global Constraints

- Моргание: пауза 3–6 секунд, длительность около 0,16 секунды; оба глаза вместе.
- Жесты ожидания: пауза 5–10 секунд после предыдущего жеста, длительность 1–2 секунды; одинаковые подряд исключены.
- Реакция на цвет: 1,6 секунды — 0,5 вниз, 0,8 кивок, 0,3 возврат. Один последний выбор, без очереди.
- Взгляд разрешён при угле лица к камере не больше 60 градусов. Лицо нынешней модели направлено вдоль локального `Vector3.back`.
- Вращение выше реакций на цвет, реакции выше приветствия и ожидания. При вращении продолжаются дыхание и моргание.
- Приветствие один раз при открытии Lobby; восстановление настроек, вкладки и громкость не вызывают реакции.
- Использовать время, не зависящее от `Time.timeScale`; не сохранять анимации и угол в PlayerPrefs.
- Сохранить ручные правки сцены, имена деталей, материалы одежды и пользовательскую строку `CompactRotationHint`.
- Не запускать `Build Lobby` для обновления текущей сцены. Не открывать второй Unity с этим проектом.
- Русские XML-комментарии обязательны; обычные и блочные комментарии только внутри методов/свойств. Коммиты на русском.
- Оставить пользователю изменение максимальной паузы в коде; не перекрывать число сериализованным значением prefab. Проверить его попытку.
- После реализации обновить README, снимки и отчёт. Не объявлять работу проверенной на устройстве или в отдельной сборке.

## Review Focus

1. Повторная настройка шаблона не создаёт вложенные дубликаты групп и не смещает детали — тест задачи 1.
2. Отсутствующая необязательная часть отключает только её движение, с одним предупреждением — тесты задач 1–2.
3. Отпускание вне героя, потеря фокуса и отключение не оставляют состояние вращения — тест задачи 3.
4. Быстрые смены цвета, включая выбор клавиатурой во время вращения, не создают очередь — тест задачи 3.
5. Изменение размера окна в середине жеста и при повороте спиной не обрезает героя и не качает камеру — тест задачи 4.

---

## Файлы и обязанности

Все новые классы находятся в пространстве имён `TinyTrouble`.

| Файл | Назначение |
| --- | --- |
| `Assets/TinyTrouble/Runtime/CharacterGesture.cs` | Перечисление `None`, `Greeting`, `WeightShift`, `LookAround`, `Wave`, `ColorReaction`. |
| `Assets/TinyTrouble/Runtime/CharacterGestures.cs` | Исходные позы, дыхание, движения групп тела и конечностей, сохранённая область поз. |
| `Assets/TinyTrouble/Runtime/CharacterFace.cs` | Исходные позы головы и глаз, моргание, взгляд и движения головы жеста. |
| `Assets/TinyTrouble/Runtime/CharacterLife.cs` | Выбор жестов, таймеры, переходы, сигналы вращения и цвета; вызов двух компонентов позы. |
| `Assets/TinyTrouble/Editor/CharacterRigBuilder.cs` | Повторяемая настройка групп и ссылок в редакторе. |
| `Assets/TinyTrouble/Editor/LobbySceneBuilder.cs` | Использование настройки героя при создании новых лобби. |
| `Assets/TinyTrouble/Runtime/CharacterDrag.cs`, `LobbyController.cs` | Передача только фактических изменений состояния и цвета. |
| `Assets/TinyTrouble/Runtime/LobbyLayout.cs` | Подгонка камеры по постоянной области поз. |
| `Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab`, `Assets/Scenes/Lobby.unity` | Группы, компоненты и ссылки без полного пересоздания. |
| `Assets/TinyTrouble/Tests/EditMode/CharacterRigTests.cs` | Сохранённая модель, повторная настройка, исходный внешний вид. |
| `Assets/TinyTrouble/Tests/PlayMode/CharacterGesturesTests.cs`, `CharacterFaceTests.cs`, `CharacterLifeTests.cs` | Движения, лицо и сочетание событий. |
| `Assets/TinyTrouble/Tests/PlayMode/LobbyLayoutTests.cs`, `CharacterRotationTests.cs`, `LobbyFlowTests.cs` | Расширение существующих проверок без удаления прежних. |

Новые Unity-ассеты сохранять вместе с `.meta`. EditMode-сборке добавить ссылки на `TinyTrouble.Runtime` и `TinyTrouble.Editor`, чтобы проверять инструмент подготовки модели напрямую.

## Общий порядок проверок

Тесты запускать через существующий редактор. Перед первой проверкой сохранить изменённые сцены и выйти из Play Mode; дождаться `EditorApplication.isPlaying == false` и `isCompiling == false`.

В шагах ниже `run_tests` и `get_test_job` означают инструменты Unity MCP:

```text
refresh_unity(mode="force", scope="all", compile="request", wait_for_ready=true)
run_tests(mode="EditMode" или "PlayMode", test_names=[полное имя класса], include_failed_tests=true)
get_test_job(job_id=полученный_id, wait_timeout=30, include_failed_tests=true)
```

Дожидаться завершённого задания, а не считать ответ с ID успешной проверкой. Для красного шага ожидается конкретное нарушение утверждения теста, не ошибка компиляции. Для зелёного — все выбранные тесты прошли, ошибок компиляции нет. Имена тестовых классов: `TinyTrouble.Tests.CharacterRigTests`, `CharacterGesturesTests`, `CharacterFaceTests`, `CharacterLifeTests`; при создании использовать этот namespace.

В примерах ниже показаны имена тестов и ключевые утверждения. Локальные результаты измерений получает сам тест из поз, вызовов событий и сохранённой сцены; тестовые помощники находятся в том же тестовом файле. Пока нового компонента нет, проверять его наличие через `GetComponent("CharacterGestures")` / `GetComponent("CharacterFace")` / `GetComponent("CharacterLife")`, чтобы красный шаг компилировался.

### Task 1: Связанные части модели, дыхание и позы тела

**Files:** Создать `CharacterGesture.cs`, `CharacterGestures.cs`, `CharacterRigBuilder.cs`, `CharacterRigTests.cs`, `CharacterGesturesTests.cs` из таблицы. Изменить `TinyTrouble.EditModeTests.asmdef`, `LobbySceneBuilder.cs` в `MakeCharacter`, шаблон героя и связанную сцену.

**Interfaces:**
- Производит `public enum CharacterGesture` с шестью значениями из таблицы.
- `CharacterRigBuilder.Configure(GameObject hero): void` — настраивает существующую модель; повторный вызов безопасен.
- `CharacterGestures.Configure(Transform upperBody, Transform leftArm, Transform rightArm, Transform leftFoot, Transform rightFoot): void` — сохраняет ссылки.
- `CharacterGestures.ApplyPose(CharacterGesture gesture, float progress, float weight, float breathingPhase): void` — progress и weight в диапазоне 0–1; breathingPhase в радианах.
- `CharacterGestures.ResetPose(): void`, `CharacterGestures.PoseBounds: Bounds { get; }` — восстановление и постоянные границы в координатах корня.
- `CharacterGestures.SetPoseBounds(Bounds bounds): void` — сохраняется инструментом редактора, а не обновляется при движениях.

- [x] **1. Написать красные тесты групп и движения.**

```csharp
// SavedRigHasRequiredGroupsAndReferences
Assert.That(hero.GetComponent("CharacterGestures"), Is.Not.Null);
Assert.That(missingReferences, Is.Empty);
// ConfigureTwicePreservesRestAppearance
Assert.That(duplicateGroupNames, Is.Empty);
Assert.That(largestWorldPositionDifference, Is.LessThan(0.0001f));
Assert.That(largestWorldRotationDifference, Is.LessThan(0.01f));
// BreathingAndWaveKeepLinkedPartsTogether
Assert.That(handRelativeToSleeveAfter, Is.EqualTo(handRelativeToSleeveBefore));
Assert.That(footPositionAfterBreathing, Is.EqualTo(footPositionBefore));
Assert.That(rightArmAngleAtWavePeak, Is.GreaterThan(30f));
// MissingArmDoesNotStopBreathing
Assert.That(upperBodyMoved, Is.True);
Assert.That(warningsForMissingArm, Is.EqualTo(1));
```

- [x] **2. Запустить CharacterRigTests и CharacterGesturesTests.** Ожидается отсутствие компонента/групп либо движения; сохранить конкретный красный результат.
- [x] **3. Реализовать интерфейсы и подготовку модели.** Добавить `UpperBodyPivot` на высоте 0,95, `HeadPivot` у шеи на высоте 1,72, `LeftArmPivot`/`RightArmPivot` у плеч (±0,44; 1,55; 0), `LeftFootPivot`/`RightFootPivot` у носков (±0,25; 0,07; −0,35). Переподчинять с сохранением мирового положения/угла/масштаба.

  В UpperBody поместить голову с лицом, шею, одежду, шнурки и группы рук. HeadPivot содержит голову, волосы, уши и все детали лица; руку составляют рукав и кисть. Группы ступней содержат обувь и соответствующую штанину. Имена деталей не менять. Поиск по именам допустим при подготовке; в игре использовать сохранённые ссылки.

  Дыхание: подъём до 0,012 и увеличение ширины/глубины UpperBody до 1%; период 3 секунды. Переминание: наклон UpperBody до 3 градусов и поочерёдный подъём пяток поворотом стоп до 6 градусов вокруг носка. Взмах правой рукой: подъём до 55 градусов с колебанием до 12 градусов. Greeting использует тот же взмах. Все смещения считаются от исходных поз, без накопления; плавная оболочка веса равна нулю в начале/конце жеста. Отсутствующие группы пропускаются, одна диагностика при настройке, не каждый кадр.

- [x] **4. Обновить сохранённый prefab через PrefabUtility и только затронутые ссылки Lobby.** Включить Configure в MakeCharacter перед сохранением нового шаблона. Не вызывать Build. Проверить, что UI и материалы сцены не изменились. Область PoseBounds окончательно заполнит задача 4.
- [x] **5. Повторить выбранные тесты до зелёного результата.** Исходная поза визуально совпадает, кисть следует за рукавом, подошва за обувью, сброс возвращает исходные значения.
- [x] **6. Коммит:** `git commit -m "Подготовить части героя для дыхания и жестов"` — перечисленные файлы и их `.meta`, без временных настроек тестов.

### Task 2: Моргание, взгляд и жесты головы

**Files:** Создать `CharacterFace.cs`, `CharacterFaceTests.cs`. Изменить `CharacterRigBuilder.cs`, `CharacterRigTests.cs`, prefab и ссылки Lobby.

**Interfaces:**
- Потребляет `CharacterGesture` из задачи 1.
- `CharacterFace.Configure(Transform headPivot, Transform[] eyeWhites, Transform[] pupils, Transform[] eyeShines): void` — пары глаз в порядке −1, +1.
- `CharacterFace.Animate(float deltaTime, CharacterGesture gesture, float progress, float weight, bool allowGaze, Vector2 pointerViewport, bool hasFocus, Camera camera): void` — один шаг по времени; координаты указателя 0–1 внутри изображения.
- `CharacterFace.ResetPose(): void` — возвращает голову/глаза и очищает таймеры. Face единственный записывает эти Transform.

- [x] **1. Написать красные тесты; зафиксировать исходные позы глаз и головы.**

```csharp
// BothEyesBlinkAndReturnToRest
Assert.That(blinkStartDelay, Is.InRange(3f, 6f));
Assert.That(blinkDuration, Is.EqualTo(0.16f).Within(0.02f));
Assert.That(leftEyeClosure, Is.EqualTo(rightEyeClosure).Within(0.001f));
Assert.That(finalEyeScale, Is.EqualTo(restEyeScale));
// GazeIsBoundedAndReturnsWhenUnavailable
Assert.That(maximumPupilOffsetX, Is.LessThanOrEqualTo(0.0351f));
Assert.That(maximumPupilOffsetY, Is.LessThanOrEqualTo(0.0251f));
Assert.That(tracksAt60Degrees, Is.True);
Assert.That(tracksAt61Degrees, Is.False);
Assert.That(offsetAfterFocusLossOrViewportExit, Is.LessThan(0.001f));
// ColorReactionMovesHeadThroughThreeStages
Assert.That(looksDownAtHalfSecond, Is.True);
Assert.That(nodsDuringNextEightTenths, Is.True);
Assert.That(headAngleAtOnePointSixSeconds, Is.LessThan(0.1f));
// MissingEyeLogsOnceAndHeadStillMoves
Assert.That(warningsForMissingEye, Is.EqualTo(1));
Assert.That(headMoved, Is.True);
```

- [x] **2. Запустить CharacterFaceTests.** Ожидается отсутствие компонента или невыполненные утверждения движения, без ошибок компиляции.
- [x] **3. Реализовать Configure, Animate и ResetPose.** Моргание сжимает по вертикали белки, зрачки и блики вместе, затем полностью восстанавливает исходный масштаб. Вертикальные смещения зрачков и бликов относительно центра белка тоже сжимать, чтобы при закрытии они не висели вне глаза. Пауза 3–6, закрытие/открытие суммарно 0,16 секунды. Зрачки/блики двигаются согласованно: максимум ±0,035 по X и ±0,025 по Y; голова помогает максимум 12 градусов по горизонтали и 8 по вертикали.

  Разрешение взгляда проверять по направлению корня `TransformDirection(Vector3.back)` к камере, а не по уже анимированной голове. Указатель вне [0;1], потеря фокуса, отсутствие камеры, allowGaze=false или угол >60 плавно возвращают взгляд. Экспоненциальное приближение с коэффициентом 12 в секунду не зависит от частоты кадров.

  LookAround поворачивает голову до ±20 градусов. ColorReaction: взгляд вниз до 15 градусов за первые 0,5 секунды, два небольших кивка амплитудой 6 градусов в следующие 0,8, возврат за последние 0,3. Смешивать голову жеста и взгляд через weight; при дыхании Face не меняет UpperBodyPivot. Продолжать моргание независимо от allowGaze. Части отсутствующей пары пропустить согласованно, предупреждение один раз.

- [x] **4. Сохранить ссылки Face в prefab через Configure; расширить проверку сохранённой сцены.** Не хранить ссылку на камеру сцены внутри prefab: камера передаётся в Animate.
- [x] **5. Повторить CharacterFaceTests и CharacterRigTests.** Ожидается PASS; движение головы сохраняет взаимное положение волос, ушей и лица.
- [x] **6. Коммит:** `git commit -m "Добавить моргание и движения головы героя"` с файлами этой задачи и `.meta`.

### Task 3: Выбор жестов, реакции меню и учебный шаг

**Files:** Создать `CharacterLife.cs`, `CharacterLifeTests.cs`. Изменить `CharacterDrag.cs`, `LobbyController.cs`, `CharacterRigBuilder.cs`, `LobbySceneBuilder.cs` в MakeUI, prefab, Lobby и существующие тесты вращения/цвета.

**Interfaces:**
- Потребляет ApplyPose/Animate/ResetPose из задач 1–2; прежний `CharacterIdle.Turn(float degrees): void` не меняется.
- `CharacterLife.Configure(CharacterGestures gestures, CharacterFace face): void` — сохранённые компоненты.
- `CharacterLife.SetDragging(bool dragging): void`, `CharacterLife.NotifyColorChanged(): void` — сообщения от Drag и Controller.
- `CharacterLife.Advance(float deltaTime, Vector2 pointerViewport, bool hasFocus, Camera camera): void` — единственный шаг таймеров и вызова компонентов; обычный Update передаёт Time.unscaledDeltaTime и текущий ввод. Тесты на отключённом компоненте используют этот же шаг.
- `CharacterLife.CurrentGesture: CharacterGesture { get; }`, `CharacterLife.IsDragging: bool { get; }` — текущее состояние только для чтения.
- Несериализованные `const float MinimumIdleDelay = 5f`, `MaximumIdleDelay = 10f` (пользователь изменил исходное 9f) в CharacterLife; второе число — участок пользователя.
- `CharacterDrag` добавляет внутренний `SetDragging(bool value): void`, который сообщает Life только при изменении флага.
- `LobbyController` добавляет сериализованную ссылку `CharacterLife characterLife` и текущий нормализованный индекс цвета; публичные методы меню сохраняются.

- [x] **1. До реализации таймера объяснить пользователю упражнение.** Подготовить CharacterLife с XML-описанием констант, открыть `MaximumIdleDelay` через `code --goto`. Предложить изменить максимум на число больше 5, сохранить и написать «готово». Подсказка: большее число увеличит возможное ожидание. Не изменять эту строку вместо пользователя. Пока он пробует, работать над событиями Drag/Controller и тестами. Изменение исходного 9 допустимо только как его учебная правка; итоговое значение отразить в документации.
- [x] **2. Написать красные тесты сочетания событий.** При отсутствии Life начать с проверки GetComponent по строке. Для Advance использовать контролируемые шаги времени, сохраняя/восстанавливая UnityEngine.Random.state и PlayerPrefs тестового окружения.

```csharp
// GreetingOnceAndFreshIdlePause
Assert.That(greetingCountAfterTabChanges, Is.EqualTo(1));
Assert.That(idleStartDelayAfterGesture, Is.InRange(5f, maximumFromCode));
Assert.That(idleDuration, Is.InRange(1f, 2f));
Assert.That(adjacentIdleGesturesAreDifferent, Is.True);
Assert.That(observedIdleKinds, Is.EquivalentTo(new[] {
    CharacterGesture.WeightShift, CharacterGesture.LookAround, CharacterGesture.Wave }));
// ColorOnlyReactsToActualChange
Assert.That(reactionsDuringSavedColorRestore, Is.Zero);
Assert.That(reactionsForSameColorOrVolumeOrTabs, Is.Zero);
Assert.That(savedColorImmediatelyAfterClick, Is.EqualTo(chosenColor));
Assert.That(colorChosenBeforeLifeStartKeepsPriorityOverGreeting, Is.True);
// RapidColorsDuringDragProduceOnlyLatestReaction
Assert.That(reactionsWhileDragging, Is.Zero);
Assert.That(reactionsAfterReleaseAndCompletion, Is.EqualTo(1));
Assert.That(reactionDuration, Is.EqualTo(1.6f).Within(0.02f));
// ReleaseOutsideFocusLossAndDisableClearDrag
Assert.That(life.IsDragging, Is.False);
Assert.That(angleAfterRelease, Is.EqualTo(chosenAngle).Within(4f));
Assert.That(interruptedGreetingReplayCount, Is.Zero);
// PausedGameStillAnimatesAndReloadResetsState
Assert.That(breathingAndBlinkWithTimeScaleZero, Is.True);
Assert.That(greetingCountAfterSceneReload, Is.EqualTo(1));
Assert.That(animatedPoseAfterDisable, Is.EqualTo(restPose));
```

- [x] **3. Запустить CharacterLifeTests и затронутые существующие тесты.** Ожидается красное утверждение отсутствия управления жестами либо реакции; сохранить результат.
- [x] **4. Реализовать Advance и приоритеты.** Greeting длится 1,5 секунды, случайные жесты 1–2. После окончания брать новую паузу между MinimumIdleDelay и MaximumIdleDelay. Выбирать следующий idle из двух вариантов, отличных от последнего; приветствие не считать случайным жестом. Не создавать очереди корутин: одно состояние, одно ожидающее событие цвета.

  Переход между несовместимыми жестами — плавный возврат текущего веса за 0,2 секунды, затем новый жест. Drag блокирует новые крупные жесты сразу; дыхание/моргание не блокируются. Повторный цвет заменяет ожидающий запрос; при уже идущей реакции плавно начать одну актуальную. После окончания Drag сбросить ожидание, не возобновлять прерванную анимацию; ожидающий цвет запустить первым. Дублирующие PointerUp/EndDrag не сбрасывают таймер второй раз.

  OnDisable возвращает позы и сбрасывает состояния; при повторном включении в той же сцене приветствие не повторяется. Новый экземпляр сцены приветствует снова. Start запускает приветствие после восстановления Controller.Awake, если более приоритетное событие ещё не произошло; Update вызывает Advance. При отсутствии одного компонента оставшийся продолжает работать с одной диагностикой.

- [x] **5. Подключить события и сохранённые ссылки.** В Drag все пути завершения, включая focus/pause/disable, вызывают его SetDragging. В Controller.SetHoodieColor сначала нормализовать/сохранить/применить цвет, затем уведомить только при отличии от прежнего индекса. Awake только восстанавливает цвет. MakeUI задаёт characterLife через SerializedObject; текущую Lobby обновить адресно. Никаких новых PlayerPrefs.
- [x] **6. Проверить попытку пользователя.** Посмотреть diff строки, объяснить смысл выбранного числа, проверить max>5 и что он не сериализован в prefab; показать частоту жестов в Unity. Если ответа ещё нет, сохранить упражнение ожидающим и продолжать задачу 4, не отмечая этот шаг выполненным.
- [x] **7. Повторить CharacterLifeTests, CharacterRotationTests и LobbyFlowTests.** Ожидается PASS, включая прежний формат громкости с пробелом. Проверить плавность прерывания вручную.
- [x] **8. Коммит:** `git commit -m "Связать жесты героя с вращением и выбором цвета"` — только после проверки учебной правки либо без её участка с явно сохранённым ожиданием.

### Task 4: Постоянная область поз, проверка лобби и документация

**Files:** Изменить `CharacterRigBuilder.cs`, `LobbyLayout.cs` в FitCamera, `LobbyLayoutTests.cs`, `CharacterRigTests.cs`, prefab/Lobby, `README.md`, согласованную спеку в части статуса и учебного значения; создать `docs/superpowers/reports/2026-10-10-character-life-validation.md`. Обновить `docs/images/lobby.png` и `lobby-portrait.png` после визуальной проверки.

**Interfaces:** Потребляет `CharacterGestures.PoseBounds`, SetPoseBounds, ApplyPose, CharacterFace.Animate. Добавляет `CharacterRigBuilder.CalculatePoseBounds(GameObject hero): Bounds`, который получает постоянную область в координатах корня. Публичный `LobbyLayout.Configure(Camera camera, Transform character): void` остаётся прежним.

- [x] **1. Расширить красные проверки раскладки.** Использовать сохранённую сцену, все три вкладки, повороты 0/90/180/270 и 13 существующих размеров: 720×1280, 1920×1080, 1024×1024, 390×844, 844×390, 1024×768, 1050×1000, 1051×1000, 1100×1000, 1440×1000, 1450×1000, 2560×1080, 1280×720. Через штатные ApplyPose/Animate задавать контрольные позы на отключённом Life; затем отдельно проверить обычный живой герой.

```csharp
// AllGesturesFitAndCameraIsStableAcrossResize
Assert.That(heroBoundsInsideScreen, Is.True);
Assert.That(heroBoundsOverlapHeaderNameOrPanel, Is.False);
Assert.That(cameraPositionAfterGesture, Is.EqualTo(cameraPositionAfterResize));
Assert.That(cameraRotationAfterGesture, Is.EqualTo(cameraRotationAfterResize));
// PoseBoundsContainsMotionExtremes
Assert.That(allRendererCornersInsideSavedPoseBounds, Is.True);
Assert.That(poseBoundsAfterGesture, Is.EqualTo(poseBoundsBeforeGesture));
```

  Контрольные позы: исходная, максимум вдоха, обе поднятые пятки по очереди, пик взмаха и его отклонения, крайние оглядывания/взгляды, три этапа ColorReaction. Сохранять прежние проверки текста, ползунка, выделения и неперекрытия блоков UI. В finally вернуть Game View, timeScale, исходную сцену и удалить только добавленные тестом размеры.

- [x] **2. Запустить расширенные CharacterRigTests и LobbyLayoutTests.** Ожидается выход позы за старую область либо её незаполненное значение; не считать любое случайное падение ожидаемым.
- [x] **3. Реализовать CalculatePoseBounds и FitCamera.** На временной копии модели собрать объединение углов renderer bounds в координатах корня: все жесты по фазе с шагом 0,025 в сочетании с крайними дыханием и разрешённым взглядом, плюс запас 0,12 по каждой стороне. Для углов renderer.bounds использовать преобразование InverseTransformPoint. Не изменять сохранённую исходную позу. Сохранить результат в Gestures через SetPoseBounds; запас проверить крайними и промежуточными позами, а не только совпадающими с шагом расчёта.

  FitCamera берёт постоянные локальные bounds и преобразует их масштаб в размеры мира; вращение корня покрывает через горизонтальный радиус, вертикальное покачивание — существующим запасом. Пересчёт остаётся только при изменении размера/включении раскладки. Без Gestures использовать прежнее получение границ как запасной путь. При одинаковом размере позиция/угол камеры не зависят от текущего жеста или yaw.

- [x] **4. Повторить выбранные тесты, затем полностью EditMode и PlayMode.** Ожидается всё PASS; записать фактические количества и ID заданий. Проверить консоль после завершения. Восстановить временно изменённый Test Runner параметр Enter Play Mode Options в памяти и `ProjectSettings/EditorSettings.asset`; не коммитить его изменение.
- [x] **5. Визуально проверить и снять результат.** В существующем Unity увидеть приветствие, несколько разных idle, моргание/дыхание, реакцию цвета, вращение посреди жеста и взгляд спереди/сбоку/спиной. Проверить отсутствие оторванных деталей и резких скачков в горизонтальном, вертикальном и квадратном окне. Для полного UI использовать асинхронный `ScreenCapture.CaptureScreenshot`, не синхронный MCP-захват с EditorApplication.Step.
- [x] **6. Обновить README и отчёт проверок.** Новое поведение, назначение трёх компонентов, ручные проверки и фактические тесты; изображения только из проверенного результата. Снять пометку «пока не реализованы» после реализации. Отразить фактическую пользовательскую максимальную паузу, статус упражнения и ограничения проверки в редакторе.
- [x] **7. Проверить весь diff и выполнить итоговый обзор.** Использовать requesting-code-review для одного независимого обзора реализации; исправить подтверждённые проблемы и повторить только затронутые проверки. Проверить XML-комментарии и README, не объявлять учебный шаг завершённым без попытки пользователя.
- [x] **8. Коммит:** `git commit -m "Учесть жесты героя в раскладке и описании лобби"` — сохранённые ассеты, проверки, README, снимки и отчёт. Отправку на GitHub выполнять по просьбе пользователя; автоматически сейчас не отправлять.

## Самопроверка плана и передача

Проверено соответствие описанию: группы/позы — задача 1, глаза/голова — 2, приоритеты/меню/учебный шаг — 3, камера/13 размеров/документация — 4. Пять условий Review Focus имеют конкретные проверки. Сигнатуры между задачами согласованы; владение Transform не пересекается, корневой yaw остаётся у CharacterIdle.

План согласован и выполнен помощником в этом чате. Пользователь изменил максимальную паузу на 10 секунд; его попытка проверена. Итог: 21/21 EditMode и 48/48 PlayMode. Независимый обзор выявил скачок взгляда в начале жеста и пропуск нажатия на машущую кисть; оба случая сначала воспроизведены тестами, затем исправлены. Результаты и ограничения сохранены в [отчёте](../reports/2026-10-10-character-life-validation.md). Ветка `codex/character-life`; отправка на GitHub и объединение с `main` выполняются по отдельной просьбе пользователя.
