# Paddle Duel

一款受 1972 年早期投幣式電子桌球遊戲啟發的 Windows 遊戲。Partner Center 產品名稱為 **Paddle Duel**；現有 App 顯示名稱與視覺仍待正式封裝項目更新。本專案與 Atari 無關，也不使用原始遊戲的商標、程式、美術、字型或錄音。

## Partner Center 產品識別

以下值由產品擁有者於 2026-10-08 從 Partner Center 提供，保留原始大小寫與標點：

| 欄位 | 值 |
| --- | --- |
| 產品名稱 | `Paddle Duel` |
| Package/Identity/Name | `25526PoyChang.PaddleDuel` |
| Package/Identity/Publisher | `CN=DC46547E-28C9-45C1-91CC-5380ACD55B44` |
| Package/Properties/PublisherDisplayName | `Poy Chang` |
| Store ID | `9NJ180X2R7TF` |

這些是產品識別資訊，不是登入憑證或簽署私鑰。此處僅記錄已取得的正式 identity；尚未套用到遊戲 App、上傳封裝或驗證 Store runtime。獨立 packaging spike 繼續使用自己的測試 identity，不應以正式值取代。

此 Store ID 屬於遊戲本體，不能當作 1 次／10 次 consumable 的 Store ID。Partner Center 已建立 `plays1`（Store ID `9NGSHZT4WSR1`）與 `plays10`（Store ID `9MZ6KPM0339W`，由產品擁有者於 2026-10-08 提供）；畫面分別顯示 `ManagedConsumable`、Quantity 1／10，Content type 選擇 `Electronic software download`。`plays10` 的 Properties 儲存受必填隱私權聲明阻塞，兩個商品的最終保存狀態仍待確認；不能將畫面輸入值視為已發行或已驗證購買。

建立產品不代表已公開上架或完成商標查核。遊戲程式尚未套用上述正式附加商品識別。

### 隱私權政策草稿

已依目前資料處理實作整理 [Paddle Duel 隱私權政策草稿](docs/privacy-policy.md)，聯絡信箱由產品擁有者指定為 `poychang.tw@gmail.com`。目前尚待擁有者確認、公開網址與正式版本核對，不能當作已發布的政策或 Store 驗收結果。準備政策本身也不決定 Partner Center 隱私權 Yes／No 的答案，仍須依實際資料用途填寫。

## 目前進度

- 可調整大小的 WinUI 3 自訂標題列視窗，預設不進入全螢幕。
- 本機雙人模式：`W` / `S` 與方向鍵控制球拍。
- 單人模式：具反應延遲且受相同移動速度限制的電腦對手。
- 單人模式可選 Easy／Medium／Hard 難度；難度只調整 AI controller，不改變遊戲物理。
- 標題列齒輪可開啟設定，調整 AI 難度與 Windowed／Full Screen 顯示模式。
- 120 Hz 固定步進核心、八段球拍反射、回擊加速與 11 分勝負。
- 全螢幕由玩家透過標題列按鈕主動切換。
- 標題列資訊按鈕會顯示 Player 1／Player 2 按鍵，遊戲中開啟時會自動暫停。
- 遊戲中按 `Esc` 可暫停或繼續；放棄比賽返回主選單不會消耗遊玩次數。
- 視窗失焦或最小化時會自動暫停並清除按鍵，回到前景後需按「CONTINUE」才能繼續。
- 視窗大小、位置與玩家選擇的全螢幕模式會保存，還原時會依目前螢幕工作區校正。
- 開發版限制單一程序，避免多開視窗同時操作免費額度資料。
- 每日免費 3 次的純核心模型已完成，包含 UTC 重置、完成才扣與時鐘回撥保護。
- 額度狀態使用 Windows LocalState 路徑與原子 JSON 替換，損毀資料會隔離保留。
- 額度完成紀錄只保留最近 400 個 UTC 日，避免本機狀態無限成長。
- 單人與雙人開局皆會建立免費額度 session，只有完成 11 分比賽才實際扣除。
- 主選單顯示今日剩餘免費次數與下一個 00:00 UTC 重置日期。
- 額度耗盡時會停用開局並顯示 Store 占位入口，不顯示假價格或執行假交易。

Microsoft Store 消耗型商品、MSIX 封裝與正式商店素材尚未實作；目前 Store 畫面只說明尚未連線，不會執行任何購買流程。

Store commerce 目前已具備平台無關的商品、餘額、購買與 fulfillment gateway fake；真實 `StoreContext` adapter 與 Partner Center 商品尚未接入。
Entitlement coordinator 已實作免費優先與 1-play／10-play fallback；真實 Store fulfillment 尚未接入。
免費額度耗盡時，付費局開始前會先查詢 Store balance，並將選定商品 pool 保存於 entitlement session。
付費 fulfillment 會先寫入 pending journal；失敗時保留 tracking ID，供之後重試。
App 啟動與回到前景時會嘗試重試 pending fulfillment；Store 暫時不可用不會阻塞遊戲啟動。
Windows `StoreContext` adapter 已可編譯並映射 Store 結果；真實 runtime 驗證仍需 package identity 與 private flight。
Store UI 已接入商品 discovery 與購買協調器；沒有 package identity 或 Store 商品時，購買按鈕會保持停用，不會模擬購買。
Fake commerce tests 已覆蓋取消、餘額錯誤、pending retry、重啟 session 與重複 fulfillment。

## 建置與執行

需求：Windows 10 22H2 或 Windows 11，以及 .NET 10 SDK。

```powershell
dotnet restore pong-in-1972.sln
dotnet build pong-in-1972.sln -c Debug -p:Platform=x64
dotnet test tests/Arcade1972.Tests/Arcade1972.Tests.csproj -c Debug
dotnet run --project src/Arcade1972.App/Arcade1972.App.csproj -c Debug -p:Platform=x64
```

目前的遊戲 App 仍是供開發驗證使用的 unpackaged WinUI 3 應用。獨立 Packaged spike 已通過本機簽署、安裝與啟動驗證；正式 MSIX/Store 建置仍需完成遊戲 App 轉換與 Partner Center identity 關聯，不能將 spike 驗證視為 Store 驗收。

### Visual Studio 封裝工具鏈

使用 Visual Studio 2026，在 Visual Studio Installer 的「匯入組態」選擇 repository 根目錄的 [`.vsconfig`](.vsconfig)，安裝 Windows App SDK C# 支援、MSIX Packaging 與 Windows SDK 26100。需要系統管理員權限；此組態只列出封裝前置元件，不取代 .NET 10 SDK 或專案的 NuGet 參考。

安裝後可在 PowerShell 驗證同一個 Visual Studio instance 具有所有必要元件：

```powershell
$config = Get-Content .\.vsconfig -Raw | ConvertFrom-Json
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -products '*' -requires $config.components -property installationPath
dotnet build .\src\Arcade1972.App\Arcade1972.App.csproj -c Debug -p:Platform=x64
```

`vswhere` 應回傳安裝路徑；沒有輸出代表尚未找到具備全部元件的 instance。另確認該路徑下的 `MSBuild\Microsoft\DesktopBridge\Microsoft.DesktopBridge.targets`，以及 `%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.26100.0\x64` 下的 `makeappx.exe` 與 `signtool.exe` 存在。

2026-10-08 已在 Visual Studio Enterprise 2026 18.10.3 驗證上述元件、MakeAppx／SignTool 可執行及既有 App 的 Debug／Release x64 建置（零警告、零錯誤）。Windows SDK 26100 是建置工具版本，不會把現有 App 的 `net10.0-windows10.0.19041.0` 目標改為 Windows 11；未來 MSIX 的 Windows Desktop 最低版本仍需另行設定並驗證。

### 獨立 Packaged WinUI 3 spike

[`spikes/Arcade1972.PackagingSpike`](spikes/Arcade1972.PackagingSpike) 是不加入遊戲 solution 的最小封裝實驗，使用測試 identity，不連線 Store、不讀寫遊戲資料，也不共用遊戲的單一程序鎖。圖示為本專案產生的黑白幾何圖形，並非正式商店素材。

- .NET 10、Windows App SDK 2.5.1、x64；.NET 與 Windows App SDK 皆為 self-contained。
- Spike 的 target framework 為 `net10.0-windows10.0.26100.0`，`TargetPlatformMinVersion` 與最終 MSIX 的 Windows Desktop 最低版本為 `10.0.19045.0`。遊戲 App 的設定未變更。
- 預設產生未簽署 MSIX，不產生 bundle 或 Store symbols package，不啟用 trimming／ReadyToRun。

從 repository 根目錄使用 **Windows PowerShell 5.1** 建置：

```powershell
$vs = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products '*' -version '[18.0,19.0)' `
    -requires Microsoft.VisualStudio.Component.WindowsAppSdkSupport.CSharp `
              Microsoft.VisualStudio.ComponentGroup.MSIX.Packaging `
    -property installationPath
if (-not $vs) { throw '找不到具備封裝工具的 Visual Studio 2026。' }
& "$vs\MSBuild\Current\Bin\MSBuild.exe" `
    .\spikes\Arcade1972.PackagingSpike\Arcade1972.PackagingSpike.csproj `
    -restore -t:Rebuild -p:Configuration=Release -p:Platform=x64 `
    -p:GenerateAppxPackageOnBuild=true -p:UapAppxPackageBuildMode=SideloadOnly
if ($LASTEXITCODE -ne 0) { throw 'MSIX 建置失敗。' }
```

實際簽署、安裝與 UI Automation 驗證：

```powershell
$package = (Resolve-Path .\spikes\Arcade1972.PackagingSpike\AppPackages\Arcade1972.PackagingSpike_1.0.0.0_x64_Test\Arcade1972.PackagingSpike_1.0.0.0_x64.msix).Path
& .\spikes\Arcade1972.PackagingSpike\Verify-Package.ps1 -PackagePath $package
```

**執行前須同意本機測試的系統變更：** 驗證腳本建立有效一天且不可匯出的 code-signing 私鑰，以 SHA-256 簽署指定 MSIX；僅匯出公開憑證，透過 UAC 暫時加入 `LocalMachine\TrustedPeople`。請在有互動桌面的 Windows PowerShell 執行並同意匯入與清除時的 UAC 提示；不要在無人值守 CI 執行。

腳本會拒絕覆蓋既有 spike 安裝或同名憑證檔，檢查 **MSIX 內**的 identity、x64、OS 版本與 self-contained payload，驗證簽章後安裝 App。以 package family name 啟動，透過 UI Automation 確認可見視窗、完整 package identity、`.NET 10.x.x | X64`，最後操作 `CLOSE` 按鈕確認退出。

`finally` 會解除安裝 spike，移除本次的 TrustedPeople 憑證、CurrentUser 私鑰與 `.cer`。不要中途強制終止 PowerShell；若清除遭取消或失敗，依輸出的 certificate thumbprint／package full name 清除該次資源，不要廣泛刪除憑證。未匯出 PFX，憑證與 `AppPackages` 均不提交。清除信任後留下的測試 MSIX 不再受本機信任；再次驗證請重新建置並執行腳本。

**2026-10-08 驗證結果：**

- 主機 OS build `26300`、Visual Studio 2026 18.10.3；Release x64 MSIX 建置成功。
- 最終 manifest 為 Windows.Desktop `MinVersion=10.0.19045.0`、`MaxVersionTested=10.0.26100.0`。
- SignTool 驗證成功（零警告、零錯誤），安裝 status 為 `Ok`。
- UI Automation 驗證 package identity、`.NET 10.0.12 | X64`、可見 WinUI 視窗與關閉按鈕成功；測試安裝與憑證已清除。
- 建置仍有 `mspdbcmf.exe` 找不到的符號套件警告；目前 MSIX targets 即使設定不產生 symbols package 仍會探測該工具。此 spike 不驗證 Store symbols，不以關閉其他警告掩蓋結果。
- 尚未驗證 Windows 10 22H2、多 DPI、遊戲 App 的 packaged LocalState、Store runtime 或 WACK。

## 專案結構

- `src/Arcade1972.Core`：不依賴 Windows UI 的 deterministic 遊戲規則。
- `src/Arcade1972.Infrastructure`：可測試的檔案持久化與平台邊界實作。
- `src/Arcade1972.App`：WinUI 3 視窗、輸入與 XAML 畫面。
- `tests/Arcade1972.Tests`：物理、勝負、固定步進與 AI 測試。
- `spikes/Arcade1972.PackagingSpike`：獨立的本機 MSIX 簽署、安裝與 WinUI 啟動驗證，不屬於遊戲 solution。
- `docs/game-reference.md`：歷史規則依據與目前 deterministic baseline。
- `docs/dev-knowledge.md`：開發中經驗證且可重用的技術知識，會隨專案演進汰舊更新。
