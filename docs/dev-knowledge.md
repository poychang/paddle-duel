# 開發知識紀錄

這份文件只保留經實作或驗證確認、未來仍可能節省除錯時間的非顯然知識。它不是工作日誌，也不要求每個開發項目都新增內容。

維護時：

- 優先記錄觸發條件、實際症狀、根因、可靠做法與重新驗證時機。
- 不收錄可直接從程式碼看出的實作細節、一次性命令輸出或未驗證猜測。
- API、工具鏈或專案限制改變後，直接修正或刪除過時內容，不保留歷史包袱。
- 規則來源放在 `docs/game-reference.md`，待辦事項放在 `todo.md`。

## WinUI 3 與 Windows App SDK

### Visual Studio 元件 ID 與 SDK 版本要以目前 catalog 驗證

**已驗證環境：** Visual Studio Enterprise 2026 18.10.3、.NET SDK 10.0.401、2026-10-08。

Windows App SDK C# 支援的元件 ID 是 `Microsoft.VisualStudio.Component.WindowsAppSdkSupport.CSharp`，不是 `Microsoft.VisualStudio.Component.WindowsAppSDK.CSharp`。使用不存在的 ID 執行 `vswhere -requires` 也會得到空結果，因此不能只靠舊 ID 判定缺少工具；先確認目前 Installer catalog 中的 ID，再查詢安裝狀態。

此版本 catalog 不提供 `Microsoft.VisualStudio.Component.Windows10SDK.19041`。已透過 Installer 安裝 Windows SDK 26100、C# 支援與 MSIX Packaging，確認 DesktopBridge targets、WinUI 範本及 x64 MakeAppx／SignTool 存在，且現有 `net10.0-windows10.0.19041.0` App 的 Debug x64 建置通過。工具 SDK 版本與 App 最低 OS 版本是不同設定；不應為了取得舊 SDK 而直接提高 App 最低版本。

可重用的元件清單位於 [`.vsconfig`](../.vsconfig)。升級 Visual Studio 後應重新確認元件 ID；實際 MSIX 簽署、安裝與啟動仍需獨立驗證，不能由 unpackaged 建置成功推論。

### MSIX 最低版本要檢查最終封裝，不只看原始 manifest

**已驗證環境：** .NET 10、Windows App SDK 2.5.1、MSIX BuildTools 1.7.251221100、2026-10-08。

Single-project MSIX 會依專案的 `TargetPlatformMinVersion`／target framework 產生最終 `TargetDeviceFamily`。Spike 最初在原始 manifest 填入 `MinVersion=10.0.19045.0`，但專案仍使用 19041 設定，實際 MSIX 內卻變為 19041。不可把編輯原始 manifest 或建置成功視為最低版本設定已生效。

Spike 改用 `net10.0-windows10.0.26100.0` 與 `TargetPlatformMinVersion=10.0.19045.0` 後，已確認 MSIX 內只有 Windows.Desktop，最低版本 19045、MaxVersionTested 26100。未來遊戲 App 轉換時應同樣對**產物**做斷言；提高 target SDK 不等於把最低 OS 提高到 Windows 11，也不代表已完成 Windows 10 實機驗收。

### 本機 MSIX 驗證不需要匯出私鑰

測試簽章可使用 `CurrentUser\My` 中的不可匯出 code-signing 私鑰，透過 SignTool `/sha1 <thumbprint> /s My` 簽署。Subject 必須與 manifest Publisher 完全一致；安裝端只需要公開 `.cer` 加入 `LocalMachine\TrustedPeople`，不需要保存 PFX 或密碼。

已以 [spike 驗證腳本](../spikes/PaddleDuel.PackagingSpike/Verify-Package.ps1) 完成簽署、信任、安裝、package activation、UI Automation 與清除。腳本透過啟動回傳的 PID 找視窗，確認 UI 的 package identity 與 runtime，再操作關閉按鈕；不以程序出現或 `MainWindowHandle` 代替 WinUI 啟動成功。測試憑證、私鑰與安裝應在驗證後清除，不能留作正式發行 identity。

### Unpackaged App 不可假設具有 package identity

**已驗證環境：** Windows App SDK 2.5.1、unpackaged WinUI 3、2026-09-24。

`Microsoft.Windows.Storage.ApplicationData.GetDefault()` 雖可編譯，但在目前沒有 package identity 的開發執行檔中，曾讓程序在建立視窗前退出。編譯成功不足以證明 LocalState API 可在該部署模式執行。

目前做法位於 `LocalStateFreePlayQuotaStore`：先以 `GetCurrentPackageFullName` 判斷 package identity。Packaged App 使用 `ApplicationData.LocalPath`；unpackaged 開發版使用 `%LocalAppData%\Arcade1972`。完成 MSIX 轉換後，要重新驗證 packaged 分支並評估是否仍需 fallback。

### WinUI 視窗驗證不要只依賴 Process.MainWindowHandle

WinUI 3 程序可能已建立可見視窗，但 `Process.MainWindowHandle` 仍回傳 `0`。自動化啟動驗證應以 `EnumWindows` 搭配 PID 找 top-level HWND，或使用 UI Automation 尋找視窗與控制項。

### 自訂標題列的子控制項需要 passthrough region

`ExtendsContentIntoTitleBar` 與 `SetTitleBar` 會讓標題列區域由 non-client input 處理。畫面上可見的資訊或全螢幕按鈕不一定能收到滑鼠事件，必須透過 `InputNonClientPointerSource.SetRegionRects` 將控制項範圍設為 `Passthrough`。矩形座標須乘上 `XamlRoot.RasterizationScale`，並在標題列尺寸改變時重算。

 ### 內部鍵盤 focus sink 不應進入玩家的 Tab 導覽

 遊戲需要一個不可見控制項接收程式化鍵盤 focus，但它不應成為玩家 Tab 導覽中的遊戲按鈕。`InputSink` 保留程式化 `Focus`，設定 `IsTabStop="False"`，並提供內部用途的 automation name。
### Unpackaged 開發版先在 OnLaunched 取得單一程序鎖

目前開發版沒有 package identity，也沒有依賴 MSIX 的 instance redirection。若讓第二個程序先建立 Window 和 quota store，兩個程序可能同時讀寫同一份 LocalState。`App.OnLaunched` 在建立 Window 前取得 per-user named mutex；取得失敗的程序立即退出，第一個程序在 Window 關閉時釋放 mutex。轉為 Packaged App 後仍應重新驗證，並評估是否改用 Windows App SDK `AppInstance` 的啟動轉導功能。

## 本機狀態持久化

### 產品改名不代表持久化識別也應改名

**已驗證：** 2026-10-08，專案／assembly／namespace 改為 `PaddleDuel.*`。

JSON 額度資料使用欄位值而非 assembly-qualified type name；改名後仍可讀取原有 payload。但儲存目錄與互斥鎖是跨版本協定，必須保留 `%LocalAppData%\Arcade1972` 與 `Local\Arcade1972.SingleInstance`，否則資料會看似消失，或讓舊版與新版同時操作同一份狀態。此次以舊 JSON fixture、既有額度載入與持有原互斥鎖時的新執行檔啟動驗證相容性。

專案改名時應只搬移來源檔，讓新路徑重新產生 `bin`／`obj`；舊 XAML 產物與 NuGet assets 可能帶有原 namespace 或路徑，不應作為新專案驗證的依據。

### 原子替換與損毀隔離

額度狀態先寫到目標目錄內的唯一暫存檔，flush 後再以 overwrite move 取代正式檔；暫存檔必須與正式檔位於同一個 volume。失敗時清除暫存檔，避免把半寫入內容當成有效狀態。

載入時除了捕捉 JSON 語法錯誤，也要驗證必要欄位與識別碼。損毀檔移到 `.corrupt` 後回傳空狀態，保留診斷證據。移動檔案前必須先釋放讀取 stream；否則 Windows 會因檔案仍被占用而讓 quarantine 失敗。

## 每日免費額度

### UTC 回撥與跨午夜比賽需要分開建模

只保存「今日使用次數」無法同時處理時間回撥與跨午夜比賽。目前模型保存最後觀察到的最大 UTC 日期，裝置時間倒退時沿用該日期，不重新發放額度。

開局時建立包含唯一 match ID 與開始日期的 session；比賽完成時才記錄消耗。如此中途離開不扣次、跨午夜仍歸屬開始日期，重複送出相同 session 也能以 match ID 保持冪等。

顯示下一次重置時間時，應由 service 回傳的有效 quota date 加一天，而不是直接使用目前系統日期。時鐘回撥時，兩者可能不同；直接使用系統日期會讓 UI 顯示一個實際不會重置額度的錯誤時間。

### 額度歷史應以最後可信日期裁剪

完成紀錄不是永久帳本；若無界保存，LocalState 會隨每日遊玩次數成長。裁剪 cutoff 應以 `LastObservedUtcDate` 減去保留窗口計算，而不是直接信任當下裝置時間。使用 inclusive cutoff 並先保留 session 的 quota date，可在壓縮歷史的同時維持時鐘回撥防護與跨 UTC 午夜比賽歸屬。

## Store commerce

### Gateway 結果必須保留 operation status

商品查詢若只回傳空清單，呼叫端無法區分「沒有商品」與網路／伺服器錯誤。`IStoreGateway` 的商品、餘額、購買與 fulfillment 結果都保留明確 status；fake gateway 以同樣模型模擬錯誤，讓 UI 與 entitlement coordinator 不必依賴真實 Store 才能測試。

Windows `StoreContext` 的 API adapter 可以先在 unpackaged 專案編譯，但商品 discovery、balance、purchase 與 fulfillment 的 runtime 行為仍需要 package identity、Store account 與 private flight 才能驗證；不能把編譯成功當成真實購買已完成。

### 購買 busy guard 必須在 request 前取得

購買按鈕的防重入不能只依賴 UI disabled 狀態；`StorePurchaseCoordinator` 在呼叫 gateway 前以原子 busy guard 取得唯一執行權，第二個 request 立即回傳 `AlreadyInProgress`。gateway 失敗後 finally 釋放 guard，讓玩家可以重試。

### 付費 fallback 必須保存選中的 Store pool

免費額度耗盡後，entitlement coordinator 先查詢 1-play pool，再查詢 10-play pool；開局時把選中的 Store ID 寫入 match session。賽後 fulfillment 使用該 Store ID 與由 match ID 產生的固定 tracking ID，避免完成時重新選 pool 造成扣錯商品。

付費局開始與賽後完成可能發生在不同程序生命週期；Store gateway 必須 lazy 建立，且完成既有 paid session 時也要能重新建立 gateway，否則重啟後會遺失 fulfillment 路徑。

### Fulfillment 必須先寫 journal 再呼叫 Store

付費局完成時先保存 `StoreId`、quantity 與固定 tracking ID，再呼叫 Store fulfillment；網路或程序中斷時才不會遺失待履行交易。成功或 Store 回報已履行後才移除 journal，重試使用同一 tracking ID。

Lifecycle retry 應在 App 啟動與回前景時執行，但不能讓 Store 初始化錯誤阻塞遊戲。使用 lazy gateway factory，retry 失敗時保留 journal 並吞掉暫時性 Store 例外，讓下一次 lifecycle event 繼續重試。

## 視窗狀態

### 還原位置前要依目前 DisplayArea 校正

`AppWindow.Position` 與 `AppWindow.Size` 是實體像素資料；使用者可能已更換螢幕或 DPI，直接還原舊位置可能把視窗放到不可見區域。保存時記錄大小、位置與 fullscreen 偏好；還原時先取得目前 `DisplayArea.WorkArea`，再限制尺寸與位置到工作區範圍內。