# Магазин и инвентарь — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить в лобби покупку, сохранение, просмотр и выбор косметики восьми категорий за Шалкоины.

**Architecture:** ScriptableObject-каталог описывает товары, обычные C#-классы управляют профилем и покупками через интерфейс хранилища. Две панели используют общие карточки, а временный просмотр отделён от сохранённого выбора. Единственный писатель поз тела остаётся CharacterGestures, материалов одежды — CharacterAppearance.

**Tech Stack:** Unity 2022.3.62f3, C#, Built-in Render Pipeline, uGUI, PlayerPrefs/JsonUtility, Unity Test Framework, Unity MCP; новых пакетов нет.

**Spec:** [Согласованная спецификация с увеличенными ценами](../specs/2026-10-11-shop-inventory-design.md).

## Global Constraints

- 27 платных товаров, 4 бесплатных цвета; первый профиль получает 1000 Шалкоинов один раз, без примерных покупок HTML.
- Цены: одежда 400, оружие 350, танцы 500, спреи 300, парашюты 400, питомцы 600, следы 400, кирки 400. Бесплатные цвета — 0.
- Валюта «Шалкоины», золотая монета «Ш»; покупки не надевают вещь автоматически.
- Десять независимых мест выбора: одежда, пистолет, автомат, дробовик, танец, спрей, парашют, питомец, след, кирка.
- Формат профиля v1, ключ `TinyTrouble.Cosmetics.Profile`; восстановление старого цвета, сохранность громкости.
- Повреждённый/неизвестный формат не выдаёт новый баланс; покупки и сохранение выбора заблокированы, исходный документ остаётся.
- Восемь категорий, четыре вкладки: «Играть», «Персонаж», «Магазин», «Настройки»; Tab/Shift+Tab, стрелки, Enter и мышь.
- Цикл следа 2,4 секунды; дыхание, моргание, плечо, вращение и приближение сохраняются. Смена одежды вызывает одну реакцию.
- Сохранять описания пользователя: «Бум!» — «Оставь яркий след в матче.», «Щенок» — «Супер друг для красоты.».
- Собственные простые модели и рисунки; матч, пополнение валюты, боевые характеристики и применение косметики в матче не добавляются.
- Работать через существующий Unity MCP; не запускать второй редактор и не вызывать полную Build Lobby для текущей сцены.
- Русские XML-комментарии перед C#-объявлениями; обычные/блочные только в методах и свойствах. Коммиты на русском.
- Сохранять все `.meta`; не перезаписывать ручные правки героя, `CompactRotationHint` и скорость `ZoomStep = .16f`.
- Учебный шаг задачи 7 предложить заранее, открыть в VS Code, оставить пользователю и проверить его попытку.
- README и отчёт обновить с настоящими результатами и снимками Unity; старые 21/21 и 56/56 не считать проверкой магазина.

## Review Focus

1. Удалённый из нового каталога купленный ID остаётся в сохранении, но не становится доступным неизвестным товаром — задача 2.
2. Повреждение профиля или ошибка диска не превращается в повторную выдачу 1000 и успешную покупку — задачи 2–3.
3. Смена карточек/закрытие вкладки при танце, вращении или просмотре питомца восстанавливает применённый вид без очередей — задачи 5–6.
4. Клавиатурное выделение удалённой/недоступной карточки и прокрутка над каталогом не теряются и не приближают героя — задачи 7–8.
5. Тесты и повторная подготовка сцены не меняют личные покупки, старые настройки и ручные объекты — задачи 2, 7, 9.

---

## Файлы и обязанности

Новые игровые классы в `Assets/TinyTrouble/Runtime/Cosmetics/`, namespace `TinyTrouble`.
Они входят в существующую Runtime asmdef; новую сборку не создавать.

| Файл | Ответственность |
| --- | --- |
| `CosmeticItem.cs`, `CosmeticCatalog.cs` | Категории, места выбора, описание товара, поиск, проверки каталога. |
| `CosmeticProfile.cs` | Копируемые данные версии, баланса, владения, выбора. |
| `ICosmeticProfileStore.cs`, `CosmeticProfileStore.cs` | Загрузка/сохранение, проверка и восстановление документа. |
| `ICosmeticPreferences.cs`, `PlayerPrefsCosmeticPreferences.cs` | Маленький заменяемый доступ к ключам PlayerPrefs. |
| `CosmeticService.cs` | Проверки операций, запись до публикации нового состояния. |
| `CharacterAppearance.cs` | Личные материалы одежды, временная примерка, восстановление. |
| `CharacterDance.cs` | Запрос и остановка процедурного танца через CharacterLife. |
| `CosmeticPreview.cs`, `CosmeticTrailPreview.cs` | Просмотр моделей/питомца/следа, камера и жизненный цикл ресурсов. |
| `CosmeticPanel.cs`, `CosmeticCard.cs` | Общие карточки магазина/инвентаря, действия, фокус и прокрутка. |
| `CosmeticPanelLayout.cs` | Категории и сетка внутри выделенного панелям прямоугольника. |
| `Assets/TinyTrouble/Editor/CosmeticAssetBuilder.cs` | Воспроизводимый каталог, модели, узоры и картинки. |
| `Assets/TinyTrouble/Editor/CosmeticUIBuilder.cs` | Точечное добавление UI и ссылок в сохранённое лобби. |
| `Assets/TinyTrouble/Cosmetics/Catalog.asset` | 31 товар; материалы, текстуры, модели, иконки в соседних подпапках. |
| `Assets/TinyTrouble/Runtime/LobbyController.cs` | Четыре вкладки, сервис, баланс и первый доступный элемент. |
| `Assets/TinyTrouble/Runtime/LobbyLayout.cs` | Разделение просмотра и каталога, шапка, камера героя/питомца. |
| `Assets/TinyTrouble/Runtime/CharacterLife.cs`, `CharacterGestures.cs`, `CharacterGesture.cs` | Танцы в существующем выборе жеста и области допустимых поз. |
| `Assets/TinyTrouble/Editor/CharacterRigBuilder.cs`, `LobbySceneBuilder.cs` | Новые ссылки, границы поз и использование добавленных builders в рецепте новой сцены. |
| `Assets/Scenes/Lobby.unity`, `Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab` | Сохранённые ссылки UI, героя и просмотра. |

Новые тесты в существующих `Assets/TinyTrouble/Tests/EditMode/` и `PlayMode/`,
namespace `TinyTrouble.Tests`. Каждый новый файл и папка получают `.meta` Unity.
Имена и сигнатуры ниже — соглашения между этапами, не готовые тела методов.

## Проверки и работа с редактором

В начале исполнения прочитать AGENTS и spec, навык Unity MCP. Проверить ветку
`codex/shop`, рабочее дерево и уже открытый проект. Для этой задачи использовать
нынешний checkout: отдельный редактор/worktree не нужен. При неожиданной
ручной правке сохранить её и адаптировать точечный инструмент.

Перед тестами сохранить изменённые ассеты/сцены, выйти из Play Mode и дождаться
окончания компиляции. У инструментов MCP уточнить действующую схему:

```text
refresh_unity(mode="force", scope="all", compile="request", wait_for_ready=true)
run_tests(mode="EditMode" или "PlayMode", test_names=["TinyTrouble.Tests.<класс>"], include_failed_tests=true)
get_test_job(job_id=<ID>, wait_timeout=30, include_failed_tests=true)
```

Новый ID задания не означает успех: ждать конечный результат. Красный шаг
должен показывать конкретное нарушение утверждения, а не ошибку компиляции.
Чтобы тесты новых API компилировались, сначала допустим минимальный контракт
без поведения; полный алгоритм писать после подтверждения красного результата.
Зелёный шаг — все выбранные тесты пройдены и новых ошибок Console нет.

Для текстовых документов: `git diff --check`. После каждого этапа коммитить
только перечисленные изменённые файлы и их `.meta`; не включать временные
размеры Game View, логи и личные настройки. Push/merge — по отдельной просьбе.

## Учебный шаг: заранее оставить часть задачи 7

До написания нового UI предложить пользователю маленькую правку заголовка
инвентаря. В начале задачи 7 создать `CosmeticUIBuilder.cs` с документированной
константой `InventoryTitle = "КОЛЛЕКЦИЯ"`, которую использует панель.
Открыть фактическую строку через `code --goto`; попросить изменить только текст
в кавычках, чтобы заголовок назывался «ИНВЕНТАРЬ». Объяснить, что строка —
надпись, а кавычки отделяют её от команд C#. Не давать готовую строку кода.

Предложить шаг до реализации остального содержимого панели. Не менять эту
константу за пользователя. Пока он пробует, создавать независимые карточки
и проверки. После ответа перечитать файл, проверить синтаксис и реально
показанный заголовок Unity. При ошибке дать одну короткую подсказку и новую
попытку. Если пользователь явно откладывает обучение, записать это в отчёте;
не объявлять учебное задание выполненным без проверки.

### Task 1: Каталог и определения товаров

**Files:** Создать `CosmeticItem.cs`, `CosmeticCatalog.cs`,
`Editor/CosmeticAssetBuilder.cs`, `Tests/EditMode/CosmeticCatalogTests.cs` и
`Cosmetics/Catalog.asset` по таблице файлов.

**Interfaces:**
- `CosmeticCategory`: Clothes, Weapon, Dance, Spray, Parachute, Pet, Trail, Pickaxe.
- `CosmeticSlot`: Clothes=0, Pistol=1, Rifle=2, Shotgun=3, Dance=4, Spray=5, Parachute=6, Pet=7, Trail=8, Pickaxe=9. Значения сохраняются, переставлять нельзя.
- `CosmeticItem` — сериализуемый класс: `string Id, Name, Description`, `CosmeticCategory Category`, `CosmeticSlot Slot`, `int Price`, `int BasicColorIndex` (−1 для платных), `Sprite Icon`, `Material Material`, `GameObject PreviewPrefab`.
- `CosmeticCatalog : ScriptableObject`: `IReadOnlyList<CosmeticItem> Items`, `bool TryGet(string id, out CosmeticItem item)`, `void Configure(CosmeticItem[] items)`, `string[] Validate(bool requireVisuals)`.
- `CosmeticAssetBuilder.BuildCatalog(): CosmeticCatalog` — записать/обновить определения в `Assets/TinyTrouble/Cosmetics/Catalog.asset`, сохранить существующие GUID.

- [x] **1. Написать тесты каталога с минимальными контрактами.**
  `CatalogHasStableIdsAndNewPrices`: 27 платных, 4 бесплатных, 31 уникальный ID,
  ровно 8 категорий и 10 мест; проверка каждой цены из Global Constraints.
  `RejectsDuplicateIdNegativePriceAndWrongSlot`: массив ошибок непустой для
  каждого нарушения; пустой для правильного каталога без визуальных ссылок.

  Ключевые утверждения для построенного каталога:
  ```csharp
  Assert.That(catalog.Items.Count, Is.EqualTo(31));
  Assert.That(catalog.Items.Count(item => item.Price > 0), Is.EqualTo(27));
  Assert.That(catalog.Items.Select(item => item.Id).Distinct().Count(), Is.EqualTo(31));
  Assert.That(catalog.TryGet("stripes", out var stripes), Is.True);
  Assert.That(stripes.Price, Is.EqualTo(400));
  Assert.That(catalog.Validate(false), Is.Empty);
  ```
- [x] **2. Запустить `CosmeticCatalogTests` в EditMode.** Ожидается неверное
  содержимое/проверка каталога, сохранить красный результат.
- [x] **3. Реализовать поиск, проверки и BuildCatalog.** Перенести все ID,
  названия и описания из согласованного HTML, без выполнения его скриптов.
  Порядок ID: `stripes`, `space`, `neon`; `pistol-pop`, `pistol-ice`,
  `rifle-space`, `rifle-lime`, `shotgun-fire`, `shotgun-wave`; `robot`, `spring`,
  `victory`; `spray-smile`, `spray-mischief`, `spray-boom`; `parachute-clouds`,
  `parachute-space`, `parachute-neon`; `pet-kitten`, `pet-puppy`, `pet-robot`;
  `trail-sparks`, `trail-stars`, `trail-rainbow`; `pickaxe-ice`, `pickaxe-neon`,
  `pickaxe-space`. Бесплатные `base-yellow`, `base-blue`, `base-red`,
  `base-purple` с BasicColorIndex 0..3 и ценой 0. Платные цены задавать сразу
  новыми числами, не прибавлять 100 при каждой сборке каталога.
- [x] **4. Повторить `CosmeticCatalogTests`.** Все проверки проходят;
  повторный BuildCatalog не удваивает цены и не создаёт второй каталог.
- [x] **5. Коммит:** `Описать товары и цены магазина`.

### Task 2: Профиль, миграция и безопасное хранилище

**Files:** Создать `CosmeticProfile.cs`, `ICosmeticProfileStore.cs`,
`ICosmeticPreferences.cs`, `PlayerPrefsCosmeticPreferences.cs`,
`CosmeticProfileStore.cs`, `Tests/EditMode/CosmeticProfileStoreTests.cs`.

**Interfaces:**
- `CosmeticProfile`: `int Version, Balance`, `string[] OwnedIds`, `string[] EquippedIds` (10 позиций), `CosmeticProfile Clone()`.
- `CosmeticLoadResult`: `CosmeticProfile Profile`, `bool CanWrite`, `string Error`.
- `ICosmeticProfileStore`: `CosmeticLoadResult Load(CosmeticCatalog catalog, int legacyColor)`, `void Save(CosmeticProfile profile, int? legacyColor)`; ошибка записи — исключение.
- `ICosmeticPreferences`: `bool HasKey(string key)`, `string GetString(string key)`, `int GetInt(string key)`, `void SetString(string key, string value)`, `void SetInt(string key, int value)`, `void DeleteKey(string key)`, `void Flush()`.
- `PlayerPrefsCosmeticPreferences` реализует интерфейс; `CosmeticProfileStore(ICosmeticPreferences preferences)` принимает заменяемый доступ к ключам.
- Ключ восстановления: `TinyTrouble.Cosmetics.Profile.Recovery`. Исходные JSON копировать туда, не перезаписывая уже имеющуюся копию; при ошибке резервирования всё равно блокировать изменения исходного профиля.

- [x] **1. Написать тесты на подставном хранилище в том же тестовом файле.**
  `FirstLoadMigratesColorAndGrantsOnce`: цвет 2 → `base-red`, баланс 1000,
  платные OwnedIds пусты; повторная загрузка не пополняет потраченные монеты,
  не изменяет ключ громкости.
  `NormalizeRetainsUnknownOwnedIdsAndRepairsSlots`: баланс −1 → 0, дубликаты
  удалены, неизвестный купленный ID сохранён, некупленные/чужие/пустые выборы
  исправлены: одежда базовая, остальные пустые.
  `CorruptAndUnknownVersionAreReadOnly`: неверный JSON и Version=99 не
  получают стартовые деньги, CanWrite=false, исходная строка неизменна,
  копия восстановления есть; повторное чтение не заменяет копию.
  `FailedFlushRestoresProfileAndLegacyColor`: при исключении прежняя JSON
  строка/отсутствие ключа и прежний цвет восстановлены в памяти.

  Ключевые утверждения первого чтения `result` при старом цвете2:
  ```csharp
  Assert.That(result.CanWrite, Is.True);
  Assert.That(result.Profile.Balance, Is.EqualTo(1000));
  Assert.That(result.Profile.OwnedIds, Is.Empty);
  Assert.That(result.Profile.EquippedIds[(int)CosmeticSlot.Clothes], Is.EqualTo("base-red"));
  ```
- [x] **2. Запустить `CosmeticProfileStoreTests` EditMode.** Ожидается
  невыполненная миграция/валидация/откат, без ошибок компиляции.
- [x] **3. Реализовать v1 JSON, проверку и запись.** Бесплатные цвета доступны
  через каталог, не притворяются покупками. Первый профиль считать созданным
  после успешной записи; при её ошибке CanWrite=false, не разрешать операции.
  Снимок старых ключей восстанавливать при ошибке, не обещая устойчивости
  к потере питания. Нормализованное существующее состояние записывать лишь
  вместе со следующей допустимой операцией. Не сохранять угол/анимацию.
- [x] **4. Повторить выбранные тесты.** Проверить отдельно, что тестовый
  double не вызывает настоящий PlayerPrefs и не выдаёт повторный запас.
- [x] **5. Коммит:** `Добавить сохранение коллекции и Шалкоинов`.

### Task 3: Покупка и независимый выбор

**Files:** Создать `CosmeticService.cs`, `Tests/EditMode/CosmeticServiceTests.cs`.

**Interfaces:**
- Потребляет каталог, профиль и store задач 1–2.
- `CosmeticOperationResult`: Success, AlreadyOwned, AlreadyEquipped, UnknownItem, NotOwned, InsufficientFunds, ReadOnly, Busy, SaveFailed.
- `CosmeticService(CosmeticCatalog catalog, ICosmeticProfileStore store, int legacyColor)` загружает профиль.
- `CosmeticProfile Snapshot { get; }` возвращает копию; `bool CanWrite { get; }`, `string Error { get; }`.
- `bool Owns(string id)`, `CosmeticOperationResult Buy(string id)`, `CosmeticOperationResult Equip(string id)`, `event Action Changed`.

- [x] **1. Написать доменные тесты.**
  `BuyDeductsNewPriceExactlyOnce`: купить `stripes` при 1000 → Success,
  Balance=600, OwnedIds содержит один stripes; второй вызов AlreadyOwned,
  Balance=600, ни лишней записи, ни второго Changed; одежда ещё base-yellow.
  `InsufficientAndUnknownItemDoNotWrite`: купить питомца при 350 →
  InsufficientFunds, неизвестный ID → UnknownItem, состояние неизменно.
  `EquipUsesIndependentWeaponSlots`: купить pistol-pop и rifle-lime,
  затем заменить pistol-pop на купленный pistol-ice; оба скина пистолета
  остаются OwnedIds, автомат остаётся rifle-lime. Для последовательности
  использовать fixture с достаточным балансом, не добавлять пополнение игры.
  `FreeColorAndOtherSlotsRemainIndependent`: выбор base-blue, pet-kitten,
  trail-stars не затрагивает другие места; некупленная вещь → NotOwned.
  `WriteFailureReadOnlyAndReentrancyNeverPublish`: ошибка Save → SaveFailed,
  Snapshot прежний, Changed=0; read-only → ReadOnly; повторный вызов из
  store во время Save → Busy; внешний изменённый Snapshot не меняет сервис.

  Ключевые утверждения покупки на первом профиле:
  ```csharp
  Assert.That(service.Buy("stripes"), Is.EqualTo(CosmeticOperationResult.Success));
  Assert.That(service.Snapshot.Balance, Is.EqualTo(600));
  Assert.That(service.Buy("stripes"), Is.EqualTo(CosmeticOperationResult.AlreadyOwned));
  Assert.That(service.Snapshot.Balance, Is.EqualTo(600));
  Assert.That(service.Snapshot.EquippedIds[0], Is.EqualTo("base-yellow"));
  ```
- [x] **2. Запустить `CosmeticServiceTests` EditMode.** Подтвердить красные
  утверждения о деньгах/выборе/событии.
- [x] **3. Реализовать операции.** Проверить ID/цену/доступность, создать
  копию, записать её и только затем заменить активное состояние и отправить
  Changed. При бесплатной одежде передавать BasicColorIndex в Save.
  AlreadyEquipped не пишет профиль; повторный танец разрешается UI отдельно.
- [x] **4. Повторить тесты сервиса и store.** Ни одного частичного
  приобретения или повторного списания.
- [x] **5. Коммит:** `Добавить покупки и выбор косметики`.

### Task 4: Собственные ассеты и внешний вид одежды

**Files:** Дополнить `CosmeticAssetBuilder.cs`; создать `CharacterAppearance.cs`,
`Tests/EditMode/CosmeticAssetTests.cs`, `Tests/PlayMode/CharacterAppearanceTests.cs`;
ассеты подпапок `Cosmetics/Materials`, `Textures`, `Models`, `Icons`.

**Interfaces:**
- `CosmeticAssetBuilder.BuildVisuals(CosmeticCatalog catalog): void` — устойчивые пути ассетов по Id, обновление на месте без новых GUID.
- `CharacterAppearance.Configure(Renderer[] hoodieRenderers): void`, `void Apply(CosmeticItem item)`, `void Preview(CosmeticItem item)`, `void Restore()`, `string AppliedId { get; }`.
- Configure создаёт личные материалы один раз; Apply закрепляет вид, Preview временный, Restore возвращает последний Apply.

- [ ] **1. Написать проверки ассетов и настоящих материалов.**
  `SavedCatalogHasVisualsForEveryItem`: все31 Icon непусты, одежда Material
  непустой, оружие/парашюты/питомцы/кирки/спреи/следы имеют PreviewPrefab;
  Validate(true) пустой. `BuildVisualsTwicePreservesGuidsAndUserRig`:
  GUID прежние, количество товаров31, герой и его ручные правки неизменны.
  `ClothingPreviewDoesNotCommitOrAlterAnotherHero`: после Apply(base-red),
  Preview(space), Restore вид снова красный, AppliedId прежний, другая копия
  героя, кожа и брюки неизменны; Destroy восстанавливает исходные материалы.

  После Apply(base-red), Preview(space), Restore:
  ```csharp
  Assert.That(appearance.AppliedId, Is.EqualTo("base-red"));
  Assert.That(catalog.Validate(true), Is.Empty);
  ```
  Вид проверять по фактическим sharedMaterial.color/mainTexture рендереров,
  не ограничиваться AppliedId. Для второй копии сравнить исходные материалы.
- [ ] **2. Запустить `CosmeticAssetTests` EditMode и
  `CharacterAppearanceTests` PlayMode.** Ожидается отсутствие нужных
  рисунков/ссылок или неправильное восстановление.
- [ ] **3. Создать визуальные ассеты.** Полосы, звёзды, неон — собственные
  текстуры на одежде, оружии, куполах и кирках. Три разных рисунка спрея,
  простые различимые пистолет/автомат/дробовик, кирка, купол со стропами,
  котёнок/щенок/робот. Иконки рендерить в редакторе в PNG со Sprite import,
  шрифт DejaVuSans при необходимости подписи. Стабильные имена по ID.
  Камеру и RT рендера иконок удалять после использования. Танцы получают
  свои символы, следы — префабы с параметрами движения для задачи 6.
- [ ] **4. Реализовать CharacterAppearance с одним владельцем материалов.**
  Исходные sharedMaterial сохранять, не менять ассеты каталога. Передача
  владения из LobbyController выполняется задачей7, без двух копировщиков.
- [ ] **5. Повторить тесты и осмотреть все категории в редакторе.** Картинки
  отличаются не только названием; отсутствуют новые shader/Console errors.
- [ ] **6. Коммит:** `Создать косметические модели и узоры одежды`.

### Task 5: Три танца в существующей системе жестов

**Files:** Создать `CharacterDance.cs`, `Tests/PlayMode/CharacterDanceTests.cs`;
изменить CharacterGesture, CharacterLife, CharacterGestures, CharacterRigBuilder,
CharacterRigTests, prefab героя по таблице файлов.

**Interfaces:**
- В CharacterGesture добавить DanceRobot, DanceSpring, DanceVictory, сохраняя значения старых жестов.
- `CharacterLife.TryStartDance(CharacterGesture dance): bool`, `void StopDance()`; позу по-прежнему записывает CharacterGestures.ApplyPose.
- `CharacterDance.Configure(CharacterLife life): void`, `bool Preview(string id)`, `void Stop()`; map robot/spring/victory на эти жесты.
- Одна петля танца — 3 секунды на unscaled time; плавное завершение за существующие .2 секунды, от исходных поз без накопления.

- [ ] **1. Написать тесты.** `ThreeDancesHaveDistinctMovingPoses`:
  в середине цикла robot/spring/victory позы различны и меняются во времени;
  рукав и кисть сохраняют связь при всех позах. `DanceRetainsBreathingBlink`:
  тело поднимается между фазами вдоха, оба глаза моргают при танце.
  `DragColorFocusLossAndStopInterruptDance`: drag побеждает танец, смена
  одежды вызывает одну реакцию, Stop/потеря фокуса заканчивает танец за .2,
  случайные жесты после паузы возвращаются, петля не возобновляется сама.
  `DanceBoundsEnclosePoseSamples`: сохранённые PoseBounds содержат все
  sampled poses, не пересчитываются каждый кадр.

  После начала robot и остановки с Advance(.21f):
  ```csharp
  Assert.That(dance.Preview("robot"), Is.True);
  Assert.That(life.CurrentGesture, Is.EqualTo(CharacterGesture.DanceRobot));
  dance.Stop();
  ```
  Затем продвинуть существующий CharacterLife.Advance на .21с:
  `Assert.That(life.CurrentGesture, Is.EqualTo(CharacterGesture.None));`.
  Связь кисти дополнительно сравнить по позиции относительно рукава до/после.
- [ ] **2. Запустить `CharacterDanceTests` PlayMode и `CharacterRigTests`
  EditMode.** Сохранить конкретную недостающую реакцию/позу.
- [ ] **3. Реализовать танцы.** Robot — чередование угловатых поворотов
  рук/корпуса; Spring — приседание/выпрямление и небольшой подъём;
  Victory — поднятые руки и наклон корпуса. Амплитуды подобрать внутри
  обновлённых постоянных PoseBounds, не отсоединяя плечо. CharacterLife
  управляет циклом/приоритетом, CharacterFace продолжает моргание;
  CharacterDance не меняет Transform суставов сам.
- [ ] **4. Точечно обновить prefab и границы через CharacterRigBuilder.**
  Не пересоздавать сцену/модель и не менять ShoulderHeight=1.50f.
- [ ] **5. Повторить новые тесты и прежние CharacterLife/Gestures/FaceTests.**
  Проверить возвращение дыхания и жестов после каждого способа остановки.
- [ ] **6. Коммит:** `Добавить просмотр трёх танцев героя`.

### Task 6: Временный просмотр предметов, питомцев и следов

**Files:** Создать `CosmeticPreview.cs`, `CosmeticTrailPreview.cs`,
`Tests/PlayMode/CosmeticPreviewTests.cs`; дополнить префабы следов задачи4.

**Interfaces:**
- `CosmeticPreview.Configure(CosmeticCatalog catalog, CharacterAppearance appearance, CharacterDance dance, Transform hero, Camera previewCamera, RawImage target, Transform previewRoot, Transform petAnchor): void`.
- `void Show(CosmeticItem item)`, `void Restore(CosmeticProfile profile)`, `void Close(CosmeticProfile profile)`; Restore возвращает сохранённую одежду/питомца и очищает временные предметы/танец.
- `CosmeticTrailPreview.Configure(CosmeticItem item): void`, `void Advance(float unscaledDeltaTime)`, `void ResetPreview()`; период2.4, детерминированная фаза без записи профиля.

- [ ] **1. Написать тесты.** `PreviewNeverChangesProfileOrHoodiePreference`:
  сравнить профиль/старый ключ до и после Show одежды, предмета и танца.
  `SwitchAndCloseRestoreClothingPetAndClearOldObjects`: быстро чередовать
  котёнка/щенка/оружие/танец/след, оставить только актуальный объект;
  после Close одежда и питомец сохранённые, угол/zoom героя прежние.
  `TrailMovesAndRepeatsAtTwoPointFourSeconds`: частицы/ленты переместились
  за .6с, после полного цикла фаза совпала, герой лобби не сдвинут.
  `CloseAndDestroyReleaseOwnedResources`: после повторных открытий/закрытий
  количество временных материалов/камер не растёт; при Destroy RT освобождён.

  Сохранённый снимок сервиса и старый ключ записать до Show; после Close:
  ```csharp
  Assert.That(service.Snapshot.Balance, Is.EqualTo(before.Balance));
  Assert.That(service.Snapshot.OwnedIds, Is.EqualTo(before.OwnedIds));
  Assert.That(service.Snapshot.EquippedIds, Is.EqualTo(before.EquippedIds));
  ```
  Объекты и ресурсы проверять дополнительно, не заменять ими проверку движения.
- [ ] **2. Запустить `CosmeticPreviewTests` PlayMode.** Увидеть красный
  результат восстановления/движения, не только наличия компонента.
- [ ] **3. Реализовать просмотр.** Выделенный слой `CosmeticPreview`:
  previewCamera рисует только его в RT, основная камера исключает этот слой.
  Примитивы без коллайдеров, camera/RT не создаются каждый кадр. Кадрировать
  предмет по bounds, рисовать спрей на нейтральной плоскости. Для следа
  использовать отдельного уменьшенного героя без idle/drag/life, не главный
  LobbyCharacter. Три разных эффекта: искры, звёзды, радужные ленты.
  Питомец появляется у отдельной точки на платформе главного героя;
  примерка питомца не меняет сохранённый ID. Изменение размера RawImage
  меняет RT лишь при фактическом изменении размеров.
- [ ] **4. Повторить тесты, осмотреть модели и все три следа в движении.**
  Закрытие вкладки, категории и потеря фокуса прекращают танец;
  последующее открытие создаёт корректный просмотр без старых объектов.
- [ ] **5. Коммит:** `Добавить просмотр косметики и питомца в лобби`.

### Task 7: Магазин, инвентарь и реальные кнопки лобби

**Files:** Создать CosmeticPanel, CosmeticCard, CosmeticUIBuilder,
`Tests/EditMode/CosmeticSceneTests.cs`, `Tests/PlayMode/CosmeticFlowTests.cs`,
`Tests/PlayMode/CosmeticPrefsScope.cs`; изменить LobbyController,
LobbySceneBuilder, LobbySceneTests, LobbyFlowTests и существующие PlayMode
fixtures, загружающие Lobby; сохранить сцену и ссылки prefab.

**Interfaces:**
- `CosmeticPanelMode`: Shop, Inventory.
- `CosmeticPanel.Configure(CosmeticCatalog catalog, CosmeticService service, CosmeticPreview preview, CosmeticPanelMode mode): void`.
- `void SelectCategory(CosmeticCategory category)`, `void SelectItem(string id)`, `void Refresh()`, `Selectable FirstSelectable { get; }`.
- `CosmeticCard.Configure(CosmeticItem item, CosmeticService service, Action<string> selected): void`, `string ItemId { get; }`; onSelect прокручивает родительский список.
- `CosmeticUIBuilder.Upgrade(UnityEngine.SceneManagement.Scene scene, CosmeticCatalog catalog): void` — точечное обновление нынешней Lobby.
- `CosmeticUIBuilder.Attach(GameObject canvas, GameObject hero, CosmeticCatalog catalog): void` — общая настройка для Upgrade и рецепта MakeUI.
- `CosmeticPrefsScope : IDisposable` в тестовой сборке: сохраняет наличие/значения ключей профиля, recovery, старого цвета и громкости; временно очищает их, Restore в Dispose после выгрузки тестового лобби.
- `LobbyController.Cosmetics: CosmeticService { get; }` — доступ к сервису для проверки фактического состояния после перезагрузки сцены.

- [ ] **1. Предложить учебный шаг и открыть константу в VS Code.** По
  отдельному разделу выше; не заменять надпись за пользователя.
- [ ] **2. Добавить защиту личных сохранений во все тесты, загружающие Lobby.**
  Выполнять до подключения сервиса к сцене. При cleanup сначала выгрузить
  тестовое лобби/отписать слушателей, затем восстановить ключи; гарантировать
  finally/TearDown при провале теста. `TestsPreserveExistingProfile` проверяет
  заранее записанный тестовый JSON, recovery, цвет и громкость после scope.
- [ ] **3. Написать тесты реального интерфейса и редакторского обновления.**
  `PurchaseThenEquipThroughButtonsAndReload`: Shop → stripes купить за400,
  Balance600, одежда прежняя; Inventory → Надеть, затем reload → stripes,
  Balance600, покупка сохранилась. `InsufficientDuplicateAndFailedSaveShowStatus`:
  кнопка недоступна при недостатке/куплено, при ошибке запись не проявляется
  как успешная покупка, понятное сообщение. Ошибку вводить подставным сервисом
  через Configure без изменения настоящего диска.
  `InventoryFiltersAndWeaponChoicesAreIndependent`: пустая категория имеет
  пояснение, неизвестные/некупленные товары скрыты; pistol/rifle независимы.
  `CorruptProfileAllowsOtherTabsButBlocksWrites`: Shop read-only, Settings
  и Play доступны. `UpgradeTwicePreservesManualObjectsAndLinks`: один
  контроллер, EventSystem, четыре tabs/panels, один previewRoot и camera;
  пользовательский объект/позиция героя не изменены, ссылки непустые.

  После настоящих кнопок покупки/применения и повторной загрузки Lobby:
  ```csharp
  Assert.That(service.Snapshot.Balance, Is.EqualTo(600));
  Assert.That(service.Snapshot.OwnedIds, Does.Contain("stripes"));
  Assert.That(service.Snapshot.EquippedIds[(int)CosmeticSlot.Clothes], Is.EqualTo("stripes"));
  Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
  ```
  Здесь service — повторно полученный Cosmetics нового LobbyController.
- [ ] **4. Запустить `CosmeticSceneTests` EditMode, `CosmeticFlowTests`
  PlayMode.** Красный результат о кнопках/ссылках/поведении.
- [ ] **5. Реализовать CosmeticPanel/Card и Upgrade/Attach.**
  Сохранить имена PlayPanel, CharacterPanel, SettingsPanel, TabPlay,
  TabCharacter, TabSettings; добавить ShopPanel, TabShop, BalanceBadge.
  Массивы контроллера в порядке Play0, Character1, Shop2, Settings3;
  прежние вызовы SelectTab(2) настроек в builders/tests изменить на3.
  Два режима используют один код фильтра/карточек. Карточки одежды
  бесплатных цветов сохраняют имена Color0..3 и SelectionFrame для
  совместимости текущего API/тестов. В магазине бесплатные цвета не показывать.
  Category buttons, ScrollRect, fixed description/action, статус операции,
  RawImage просмотра — реальные uGUI-объекты с сериализованными ссылками.
  Persistent handlers для сохранённых кнопок; динамические карточки подключает
  Configure. Старый тест «каждая кнопка имеет persistent event» уточнить:
  static UI проверяется в EditMode, динамические обработчики — PlayMode.
- [ ] **6. Подключить сервис, вид и вкладки в LobbyController.**
  Создавать service/appearance один раз; подписка Changed обновляет баланс,
  панель и сохранённый вид. Удалить старое дублирующее владение материалами,
  SetHoodieColor делегирует Equip бесплатного ID, ApplyColor — appearance.
  Показывать активный выбор отдельно от временно выделенной карточки;
  купить не значит надеть. При смене вкладки Close(profile), фокус на
  FirstSelectable панели, на Settings volumeSlider, на Play playButton.
  Уже выбранный танец можно проиграть отдельным доступным действием
  «Танцевать»; другое действие «Выбрать и танцевать» сохраняет выбор.
  Остальные надписи точно по спецификации. Во время загрузки уровня
  закрыть просмотр и заблокировать повторный запуск/действия.
- [ ] **7. Сохранить результат Upgrade и ссылки в существующей Lobby.**
  В MakeUI добавить Attach для будущих новых сцен, не запускать Build сейчас.
  Уточнить старый тест одной камеры: одна основная + одна камера просмотра
  с targetTexture и выделенным cullingMask; не считать её второй игровой.
- [ ] **8. Проверить попытку пользователя и повторить тесты.** Заголовок
  действительно изменился; новые и прежние LobbyFlowTests зелёные.
  Не подстраивать тесты так, чтобы пропала проверка сохранения/навигации.
- [ ] **9. Коммит:** `Добавить магазин и инвентарь в сцену лобби`.

### Task 8: Адаптивная раскладка и управление

**Files:** Создать CosmeticPanelLayout, `Tests/PlayMode/CosmeticNavigationTests.cs`;
изменить LobbyLayout, LobbyController.AdvanceFocus, LobbyLayoutTests,
CharacterZoomTests, ссылки панели и области героя в Lobby.

**Interfaces:**
- `CosmeticPanelLayout.Configure(RectTransform categoryRoot, RectTransform viewport, RectTransform content, RectTransform description, RectTransform action): void`, `void Apply(Vector2 size)`.
- `LobbyLayout.SetCosmeticView(bool active, bool hasPet): void` — изменить
  области, пересчитать только при реальном изменении режима/размера/питомца.
- `CosmeticPanel.EnsureSelectionVisible(Selectable selection): void`;
  вызывается при клавиатурном onSelect и после обновления коллекции.

- [ ] **1. Расширить реальные Game View тесты без удаления прежних кейсов.**
  `ShopAndInventoryFitAllSupportedSizes`: все4вкладки и8категорий при каждом
  размере из нынешнего массива13; header/badge/categories внутри окна,
  кнопка/описание вне scrollviewport, каталог не пересекает hero/preview.
  Проверить содержимое и ширины надписей, не только прямоугольник родителя.
  `ResizeDuringDancePetPreviewAndZoomKeepsAreasSeparate`: изменять размер
  во время танца, с питомцем и максимальным zoom; позы не качают камеру,
  на обычной вкладке zoom по-прежнему даёт крупный план лица.
  `KeyboardScrollAndRefreshKeepVisibleFocus`: Tab/Shift+Tab, стрелки, Enter
  доходят до карточек за viewport, auto-scroll показывает их; после смены
  категории/покупки выделен существующий активный Selectable.
  `WheelOnCatalogDoesNotZoomHero`: события ScrollRect меняют scroll,
  расстояние камеры неизменно; scroll по герою меняет zoom как раньше.

  После каждого перехода фокуса:
  ```csharp
  Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
  Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
  Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable(), Is.True);
  ```
  Для экранных областей использовать существующие Bounds/Inside в
  LobbyLayoutTests; утверждать отсутствие Rect.Overlaps между каталогом,
  областью героя/предмета и фиксированной кнопкой.
- [ ] **2. Запустить `LobbyLayoutTests`, `CosmeticNavigationTests`,
  `CharacterZoomTests` PlayMode.** Ожидается непоместившаяся область или
  потерянный фокус на старой трёхвкладочной раскладке.
- [ ] **3. Реализовать шапку и разделение областей.**
  Четыре вкладки и badge; на компактном экране под названием отдельная
  строка вкладок. На shop/inventory скрыть старый рекламный текст.
  На широком окно просмотра слева, каталог справа; aspect<=1.05 — сверху
  и снизу, промежуточный аспект — две области по свободному месту.
  У категорий перенос строк, у grid три колонки при достаточной ширине,
  иначе две, у списка вертикальная прокрутка. Кнопка/цена/название видимы.
  Скрывать только вторичные пояснения, не уменьшать всё до нечитаемого.
  Постоянные bounds героя расширить областью выбранного питомца; камеру
  предметов настраивать независимо. У нового UI raycast блокирует колесо
  от героя; main drag/zoom остаётся на реальном герое.
- [ ] **4. Реализовать явные переходы клавиатуры и прокрутку выделения.**
  Навигация использует только активные interactable elements; ScrollRect
  не получает Enter вместо карточки; недоступные кнопки пропускаются.
  После удаления старой карточки выбрать ближайшую текущую или категорию,
  а не оставлять EventSystem с уничтоженным объектом.
- [ ] **5. Повторить выбранные проверки и осмотреть 1920×1080, 1024×768,
  1024×1024, 390×844, 844×390.** Сохранить настоящие снимки Unity,
  при дефекте поправить конкретную область и повторить затронутый тест.
- [ ] **6. Коммит:** `Адаптировать магазин и управление для разных экранов`.

### Task 9: Сквозная проверка, README и отчёт

**Files:** Изменить `README.md`; создать
`docs/superpowers/reports/2026-10-11-shop-inventory-validation.md`, реальные
снимки `docs/images/shop-landscape.png`, `shop-portrait.png`, `inventory.png`;
при найденном дефекте изменить принадлежащие ему файлы и связанные проверки.

**Interfaces:** Потребляет законченную сцену/каталог/API задач1–8; новых API нет.

- [ ] **1. Выполнить один полный EditMode и PlayMode после зелёных этапов.**
  Дождаться завершения обеих jobs, сохранить итоги/время/провалы. Убедиться,
  что временные профили и размеры Game View восстановлены. Старые21/56 —
  baseline, фактические новые количества записать из результата.
- [ ] **2. Проверить сценарий игрока в существующем редакторе.**
  На временном профиле купить одежду400 + спрей300 → баланс300;
  питомец600 недоступен. Надеть одежду/выбрать спрей, перезапустить Play:
  баланс300 и выбор сохранились. Другой временный профиль — проверить
  отдельные оружия, все3танца/следа, питомцев, парашюты/кирки. Вернуть
  оригинальные ключи после проверки; не тратить личные монеты пользователя.
- [ ] **3. Проверить регрессии и Console.**
  Громкость, старт Lobby, Play → SampleScene один раз, четыре бесплатных
  цвета, дыхание/моргание, связанное плечо, drag и zoom. Быстро20раз открыть
  категории и вкладки; временные материалы/камеры/подписки не накапливаются.
  Никаких новых ошибок, предупреждения объяснены в отчёте.
- [ ] **4. Обновить README и отчёт.**
  Указать реальные цены, начальные1000, восемь категорий, правила покупки
  и независимые оружия; профиль/ключ/recovery и ограничения локального
  сохранения; новые компоненты/builders, запуск, проверенные размеры и
  результаты. Старые макеты явно отделить от снимков Unity; не писать
  о матчах, устройстве, touch или сборке как о проверенных.
  Записать учебную попытку/результат и незавершённые ограничения.
- [ ] **5. Провести финальное ревью по спецификации и AGENTS.**
  При исполнении самим агентом — использовать предусмотренное навыком
  executing-plans независимое ревью законченной ветки; сейчас никого не
  запускать. Исправить подтверждённые проблемы и повторить только затронутые
  проверки; если код изменился после полного прогона, объяснить охват.
  `git diff --check`, XML-комментарии и корректность `.meta` обязательны.
- [ ] **6. Коммит:** `Описать и проверить магазин и инвентарь`.

## Перед началом исполнения

Спецификация согласована с поправкой цен +100. Этот план нужно показать
пользователю и дождаться его просмотра. Ранее выбранное исполнение самим
агентом в этом чате сохраняется; после подтверждения плана использовать
`superpowers:executing-plans`. Игровой код до подтверждения не писать.
