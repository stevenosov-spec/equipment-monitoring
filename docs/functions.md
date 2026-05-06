# Документация функций системы мониторинга оборудования

## 1. Уровень интеграции (EquipmentMonitoring.Integration)

### SimulatedTagReader
Реализует интерфейс `ITagReader`. Имитирует поступление данных с промышленных датчиков, генерируя плавно меняющиеся значения с возвратом к среднему (mean reversion).

- **Конструктор** – инициализирует словарь тегов (имя тега, min, max, среднее) и начальные значения.
- **Start()** – запускает внутренний таймер с интервалом 1 секунду, который вызывает событие `TagValueChanged`.
- **Stop()** – останавливает таймер и освобождает ресурсы.
- **GenerateGaussian(double mean, double stdDev, double min, double max)** – статический вспомогательный метод генерации случайного числа с нормальным распределением.
- **Событие TagValueChanged** – передаёт новое значение тега (`TagValueChangedEventArgs`).

## 2. Бизнес-логика (EquipmentMonitoring.Core)

### EquipmentMonitorService (IEquipmentMonitor)
Центральный сервис мониторинга. Подписывается на события от `ITagReader`, обновляет параметры в БД, обнаруживает выход за границы допуска, создаёт отказы и управляет состоянием оборудования.

- **Start() / Stop()** – запускает/останавливает получение данных.
- **OnTagValueChanged(object? sender, TagValueChangedEventArgs e)** – обработчик нового значения тега: обновляет соответствующий параметр, проверяет границы, при выходе создаёт объект `Fault` и вызывает событие `OnFaultDetected`. Затем пересчитывает общее состояние оборудования (`Alarm`/`Normal`) и вызывает `OnStateChanged`.
- **DeterminePriority(Parameter param, double value)** – статический метод, вычисляющий приоритет отказа на основе относительного отклонения от границ.
- **События**:
  - `OnFaultDetected` – уведомляет UI о новом отказе.
  - `OnStateChanged` – уведомляет об изменении состояния оборудования.

### ReportService (IReportGenerator)
Генерирует Excel-отчёт по отказам за указанный период с помощью библиотеки ClosedXML.

- **GenerateFaultReport(DateTime from, DateTime to)** – возвращает массив байт готового .xlsx файла. Включает столбцы: Дата/время, Оборудование, Описание, Приоритет, Статус.

### OeeService (IOeeService)
Рассчитывает показатели OEE (Overall Equipment Effectiveness) для конкретного оборудования за заданный интервал времени.

- **CalculateOeeAsync(int equipmentId, DateTime from, DateTime to)** – асинхронно вычисляет доступность (по времени простоев из-за отказов), производительность (отношение текущего значения параметра к номинальному) и качество (заглушка = 1.0). Возвращает объект `OeeResult`.

### HistoryService (IHistoryService)
Предоставляет исторические данные параметра для построения трендов. В учебном прототипе генерирует синтетические точки на основе текущего значения с небольшим случайным разбросом.

- **GetHistoryAsync(int parameterId, DateTime from, DateTime to)** – возвращает список `ParameterHistoryPoint` с шагом 10 минут.

### Модели данных (Core.Models)
- **Equipment** – единица оборудования (Id, Name, Type, Installation, CurrentState, списки Parameters и Faults).
- **Parameter** – измеряемый параметр (Id, Name, Value, Timestamp, Unit, TagAddress, MinAllowed, MaxAllowed, NominalValue, связь с Equipment).
- **Fault** – запись об отказе (Id, EquipmentId, StartTime, EndTime, Description, Priority, Status).
- **User** – учётная запись пользователя (не используется в текущем прототипе, но присутствует).

### Перечисления (Core.Enums)
- **EquipmentState** – Normal, Warning, Alarm, NoData.
- **FaultPriority** – Low, Medium, High, Critical.
- **FaultStatus** – Active, Acknowledged, Escalated, Closed.

### Контекст базы данных (AppDbContext)
Entity Framework Core DbContext. Содержит DbSet для Equipment, Parameter, Fault, User. Настроены связи: каскадное удаление параметров при удалении оборудования, и NoAction для Faults (история сохраняется). Используется фабрика `IDbContextFactory<AppDbContext>` для потокобезопасного создания контекста.

## 3. Уровень представления (EquipmentMonitoring.App)

### MainViewModel
Главная ViewModel, управляющая состоянием и поведением основного окна.

- **Коллекции**: Equipments, ActiveFaults, SelectedParameters.
- **Свойства**: SelectedEquipment, данные OEE (CurrentOeeText и др.).
- **Команды**: Start/StopMonitoring, AcknowledgeFault, GenerateReport, GenerateOeeReport, ShowTrend.
- **Методы**:
  - `LoadEquipments()` – загружает список оборудования из БД.
  - `StartMonitoring() / StopMonitoring()` – запускает/останавливает мониторинг.
  - `OnFaultDetected(Fault fault)` – добавляет новый отказ в начало списка.
  - `OnStateChanged(int equipmentId, EquipmentState newState)` – обновляет цвет плитки оборудования.
  - `AcknowledgeFault(FaultViewModel? faultVm)` – сохраняет статус отказа в БД, проверяет, остались ли активные отказы, и при необходимости переводит оборудование в Normal. Через 3 секунды удаляет отказ из списка.
  - `GenerateReport()` – экспорт отчёта по отказам в Excel.
  - `GenerateOeeReport()` – открывает диалог выбора периода, затем асинхронно генерирует сводный отчёт OEE для всех единиц оборудования.
  - `ShowTrend(ParameterViewModel? paramVm)` – открывает окно тренда.
  - `RefreshParametersAndOee()` – вызывается таймером каждую секунду, обновляет значения параметров и текущий OEE выбранного оборудования.

### Дочерние ViewModel
- **EquipmentViewModel** – обёртка для Equipment с наблюдаемым свойством State.
- **FaultViewModel** – обёртка для Fault с наблюдаемым статусом.
- **ParameterViewModel** – обёртка для Parameter с наблюдаемыми Value, Timestamp и дополнительными MinAllowed, MaxAllowed.
- **TrendViewModel** – управляет окном тренда: содержит список масштабов времени, даты начала/конца, команду Refresh, строит график OxyPlot с границами допуска (аннотации LineAnnotation).

### Окна
- **MainWindow** – главное окно с тремя панелями.
- **TrendWindow** – окно для отображения графика тренда.
- **OeeDateRangeDialog** – диалог выбора периода для отчёта OEE.

### Конвертер
- **StateToColorConverter** – преобразует EquipmentState в кисть соответствующего цвета для графического индикатора.

### DI-контейнер (App.xaml.cs)
Регистрирует все сервисы и ViewModel, выполняет миграции БД, заполняет начальные данные (SeedData).