using TrayAlarmWatcher.Api;
using TrayAlarmWatcher.Configuration;
using TrayAlarmWatcher.Models;
using TrayAlarmWatcher.Services;
using Region = TrayAlarmWatcher.Api.Region;

namespace TrayAlarmWatcher;

public sealed class TrayApplicationContext : ApplicationContext
{
    private const int TooltipMaxLength = 63;
    private const int PollIntervalMs = 60_000;

    private readonly NotifyIcon _notifyIcon;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly AppConfig? _config;
    private readonly System.Windows.Forms.Timer _pollTimer;

    private readonly Icon _alarmIcon = TrayIconFactory.CreateAlarmIcon();
    private readonly Icon _calmIcon = TrayIconFactory.CreateCalmIcon();
    private readonly Icon _unknownIcon = TrayIconFactory.CreateUnknownIcon();

    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _refreshMenuItem;
    private readonly ToolStripMenuItem _selectRegionMenuItem;
    private readonly ToolStripMenuItem _autoStartMenuItem;
    private readonly Dictionary<string, ToolStripMenuItem> _regionMenuItemsById = new();

    private AlarmStatusSnapshot _snapshot = AlarmStatusSnapshot.InitialUnknown();
    private bool _isRefreshing;
    private AlarmStatus? _lastConfirmedStatus;
    private List<Region>? _cachedStates;
    private bool _isFetchingRegionsTree;

    public TrayApplicationContext()
    {
        _config = AppConfig.Load();

        _statusMenuItem = new ToolStripMenuItem("Статус: невідомо") { Enabled = false };
        _refreshMenuItem = new ToolStripMenuItem("Оновити зараз", null, OnRefreshClicked);
        _selectRegionMenuItem = new ToolStripMenuItem("Обрати район/місто");
        _selectRegionMenuItem.DropDownOpening += OnSelectRegionDropDownOpening;
        _autoStartMenuItem = new ToolStripMenuItem("Запускати з Windows")
        {
            CheckOnClick = true,
            Checked = AutoStartManager.IsEnabled()
        };
        _autoStartMenuItem.CheckedChanged += OnAutoStartCheckedChanged;

        var exitMenuItem = new ToolStripMenuItem("Вийти", null, OnExit);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_statusMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_refreshMenuItem);
        contextMenu.Items.Add(_selectRegionMenuItem);
        contextMenu.Items.Add(_autoStartMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _unknownIcon,
            Text = "TrayAlarmWatcher",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _pollTimer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
        _pollTimer.Tick += OnPollTimerTick;

        UpdateUi();

        if (_config is null || string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _refreshMenuItem.Enabled = false;
            _snapshot = new AlarmStatusSnapshot(AlarmStatus.Unknown, DateTime.Now, "немає apiKey");
            UpdateUi();

            _notifyIcon.ShowBalloonTip(
                5000,
                "TrayAlarmWatcher",
                $"Конфігурацію не знайдено або відсутній apiKey.\nЗаповніть {AppConfig.FilePath}",
                ToolTipIcon.Warning);
            return;
        }

        _ = InitializeAsync(_config);
        _ = PrefetchRegionsTreeAsync(_config.ApiKey);
        _pollTimer.Start();
    }

    private async void OnPollTimerTick(object? sender, EventArgs e) => await RefreshStatusAsync();

    private async Task InitializeAsync(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.RegionId))
        {
            await ResolveBuchaRegionAsync(config);
        }

        if (!string.IsNullOrWhiteSpace(config.RegionId))
        {
            await RefreshStatusAsync();
        }
    }

    private async Task ResolveBuchaRegionAsync(AppConfig config)
    {
        try
        {
            var lookupService = new RegionLookupService(new RegionsApiClient(_httpClient));
            var result = await lookupService.FindBuchaDistrictAsync(config.ApiKey);

            if (result.Success)
            {
                config.RegionId = result.RegionId!;
                config.Save();

                _notifyIcon.ShowBalloonTip(
                    5000,
                    "TrayAlarmWatcher",
                    $"Знайдено регіон \"{result.RegionName}\" ({result.RegionId}). Збережено в конфіг.",
                    ToolTipIcon.Info);
            }
            else
            {
                _snapshot = new AlarmStatusSnapshot(AlarmStatus.Unknown, DateTime.Now, "район не знайдено");
                UpdateUi();

                _notifyIcon.ShowBalloonTip(
                    8000,
                    "TrayAlarmWatcher",
                    $"Не вдалося знайти район \"Бучанськ\" у списку regions.\nПеревірте {AppConfig.RegionsLogFilePath} та вкажіть regionId вручну.",
                    ToolTipIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogError($"Пошук regionId для Бучанського району провалився: {ex.Message}");

            _snapshot = new AlarmStatusSnapshot(AlarmStatus.Unknown, DateTime.Now, ex.Message);
            UpdateUi();

            _notifyIcon.ShowBalloonTip(
                8000,
                "TrayAlarmWatcher",
                $"Помилка при отриманні списку regions: {ex.Message}",
                ToolTipIcon.Error);
        }
    }

    private async Task PrefetchRegionsTreeAsync(string apiKey)
    {
        if (_isFetchingRegionsTree)
        {
            return;
        }

        _isFetchingRegionsTree = true;
        try
        {
            var lookupService = new RegionLookupService(new RegionsApiClient(_httpClient));
            _cachedStates = await lookupService.GetStatesAsync(apiKey);
            BuildRegionMenu();
        }
        catch (Exception ex)
        {
            FileLogger.LogError($"Не вдалося завантажити список регіонів для меню вибору: {ex.Message}");
        }
        finally
        {
            _isFetchingRegionsTree = false;
        }
    }

    private void OnSelectRegionDropDownOpening(object? sender, EventArgs e)
    {
        if (_selectRegionMenuItem.DropDownItems.Count == 0)
        {
            _selectRegionMenuItem.DropDownItems.Add(new ToolStripMenuItem("Завантаження списку регіонів...") { Enabled = false });
        }
        else
        {
            // Регіон міг бути автовизначений (Бучанський район) уже після побудови меню - синхронізуємо позначку.
            UpdateRegionMenuChecks();
        }

        if (_cachedStates is null && _config is not null)
        {
            _ = PrefetchRegionsTreeAsync(_config.ApiKey);
        }
    }

    private void BuildRegionMenu()
    {
        if (_cachedStates is null)
        {
            return;
        }

        _selectRegionMenuItem.DropDownItems.Clear();
        _regionMenuItemsById.Clear();

        foreach (var state in _cachedStates.OrderBy(s => s.RegionName))
        {
            var districts = state.RegionChildIds.OrderBy(d => d.RegionName).ToList();
            if (districts.Count == 0)
            {
                continue;
            }

            var stateItem = new ToolStripMenuItem(state.RegionName);

            foreach (var district in districts)
            {
                var districtItem = new ToolStripMenuItem(district.RegionName);

                var selectDistrictItem = new ToolStripMenuItem($"Обрати «{district.RegionName}»", null, OnRegionSelected)
                {
                    Tag = district
                };
                districtItem.DropDownItems.Add(selectDistrictItem);
                _regionMenuItemsById[district.RegionId] = selectDistrictItem;

                var communities = district.RegionChildIds.OrderBy(c => c.RegionName).ToList();
                if (communities.Count > 0)
                {
                    districtItem.DropDownItems.Add(new ToolStripSeparator());

                    foreach (var community in communities)
                    {
                        var communityItem = new ToolStripMenuItem(community.RegionName, null, OnRegionSelected)
                        {
                            Tag = community
                        };
                        districtItem.DropDownItems.Add(communityItem);
                        _regionMenuItemsById[community.RegionId] = communityItem;
                    }
                }

                stateItem.DropDownItems.Add(districtItem);
            }

            _selectRegionMenuItem.DropDownItems.Add(stateItem);
        }

        UpdateRegionMenuChecks();
    }

    private void OnRegionSelected(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: Region region } || _config is null)
        {
            return;
        }

        if (string.Equals(region.RegionId, _config.RegionId, StringComparison.Ordinal))
        {
            return;
        }

        _config.RegionId = region.RegionId;
        _config.Save();
        _lastConfirmedStatus = null;

        UpdateRegionMenuChecks();

        _notifyIcon.ShowBalloonTip(
            5000,
            "TrayAlarmWatcher",
            $"Регіон змінено на \"{region.RegionName}\". Оновлюю статус...",
            ToolTipIcon.Info);

        _ = RefreshStatusAsync();
    }

    private void UpdateRegionMenuChecks()
    {
        foreach (var item in _regionMenuItemsById.Values)
        {
            item.Checked = false;
        }

        if (_config?.RegionId is { Length: > 0 } currentId
            && _regionMenuItemsById.TryGetValue(currentId, out var currentItem))
        {
            currentItem.Checked = true;
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e) => await RefreshStatusAsync();

    private async Task RefreshStatusAsync()
    {
        if (_isRefreshing || _config is null || !_config.IsValid)
        {
            return;
        }

        _isRefreshing = true;
        _refreshMenuItem.Enabled = false;

        try
        {
            var checker = new AlarmStatusChecker(new AlertsApiClient(_httpClient));
            _snapshot = await checker.CheckAsync(_config.ApiKey, _config.RegionId);
            NotifyOnStatusChange(_snapshot.Status);
        }
        finally
        {
            _isRefreshing = false;
            _refreshMenuItem.Enabled = true;
            UpdateUi();
        }
    }

    private void NotifyOnStatusChange(AlarmStatus newStatus)
    {
        // Мережеві збої (Unknown) не вважаються підтвердженою зміною стану - не скидаємо
        // й не сповіщаємо, щоб не було хибних "відбоїв"/"тривог" через тимчасову недоступність API.
        if (newStatus == AlarmStatus.Unknown)
        {
            return;
        }

        if (newStatus == AlarmStatus.Alarm && _lastConfirmedStatus != AlarmStatus.Alarm)
        {
            _notifyIcon.ShowBalloonTip(10000, "TrayAlarmWatcher", "Оголошено повітряну тривогу", ToolTipIcon.Warning);
        }
        else if (newStatus == AlarmStatus.Calm && _lastConfirmedStatus == AlarmStatus.Alarm)
        {
            _notifyIcon.ShowBalloonTip(10000, "TrayAlarmWatcher", "Відбій повітряної тривоги", ToolTipIcon.Info);
        }

        _lastConfirmedStatus = newStatus;
    }

    private void UpdateUi()
    {
        var time = _snapshot.CheckedAtLocal.ToString("HH:mm:ss");

        var (icon, statusText) = _snapshot.Status switch
        {
            AlarmStatus.Alarm => (_alarmIcon, "Тривога"),
            AlarmStatus.Calm => (_calmIcon, "Спокійно"),
            _ => (_unknownIcon, _snapshot.ErrorMessage is null ? "Невідомо" : $"Невідомо ({_snapshot.ErrorMessage})")
        };

        _notifyIcon.Icon = icon;
        _notifyIcon.Text = Truncate($"{statusText}. Оновлено: {time}", TooltipMaxLength);
        _statusMenuItem.Text = $"Статус: {statusText} (перевірено: {time})";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private void OnAutoStartCheckedChanged(object? sender, EventArgs e) =>
        AutoStartManager.SetEnabled(_autoStartMenuItem.Checked);

    private void OnExit(object? sender, EventArgs e)
    {
        _pollTimer.Stop();
        _pollTimer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _httpClient.Dispose();
        _alarmIcon.Dispose();
        _calmIcon.Dispose();
        _unknownIcon.Dispose();
        Application.Exit();
    }
}
