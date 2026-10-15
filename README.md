# Paddle Duel

一款受 1972 年早期投幣式電子桌球遊戲啟發的 Windows 遊戲。Partner Center 產品名稱與 App 顯示名稱皆為 **Paddle Duel**，具有原創黑白封裝圖示與繁中／英文介面。本專案與 Atari 無關，也不使用原始遊戲的商標、程式、美術、字型或錄音。

GitHub repository：[poychang/paddle-duel](https://github.com/poychang/paddle-duel)，Git remote URL 為 `https://github.com/poychang/paddle-duel.git`。Solution 為 [`PaddleDuel.sln`](PaddleDuel.sln)，專案目錄、`.csproj`、assembly 與 namespace 使用 `PaddleDuel.*`。本機 checkout 目錄名稱不影響建置，開啟中的工作目錄不在此次改名範圍。

## Partner Center 產品識別

以下值由產品擁有者於 2026-10-08 從 Partner Center 提供，保留原始大小寫與標點：

| 欄位 | 值 |
| --- | --- |
| 產品名稱 | `Paddle Duel` |
| Package/Identity/Name | `25526PoyChang.PaddleDuel` |
| Package/Identity/Publisher | `CN=DC46547E-28C9-45C1-91CC-5380ACD55B44` |
| Package/Properties/PublisherDisplayName | `Poy Chang` |
| Store ID | `9NJ180X2R7TF` |

這些是產品識別資訊，不是登入憑證或簽署私鑰。2026-10-15 已將 Package Name、Publisher 與 PublisherDisplayName 套用到遊戲 App，並核對最終 MSIX；本機安裝的 package family 為 `25526PoyChang.PaddleDuel_hwmjejkjqxx7e`。尚未上傳封裝或驗證真實 Store 購買。獨立 packaging spike 繼續使用自己的測試 identity，不應以正式值取代。

此 Store ID 屬於遊戲本體，不能當作 1 次／10 次 consumable 的 Store ID。Partner Center 已建立以下商品，畫面分別顯示 `ManagedConsumable`、Quantity 1／10，Content type 選擇 `Electronic software download`。產品擁有者於 2026-10-08 回報兩個商品的隱私權政策與以下售價已設定完成：

| Product ID | Store ID | Quantity | USD 售價（每次購買整包） |
| --- | --- | --- | --- |
| `plays1` | `9NGSHZT4WSR1` | 1 | 0.99 USD |
| `plays10` | `9MZ6KPM0339W` | 10 | 1.99 USD |

附加商品政策網址為 `https://project.poychang.net/paddle-duel/privacy.html`；`plays10` 截圖顯示隱私權聲明選擇 Yes。產品擁有者於 2026-10-08 回報兩個商品的 Store listings 設定完成。此表僅記錄 Partner Center 設定；App 必須顯示 Store 回傳的商品名稱與 formatted price，不得硬編碼這些 USD 售價或自行換算其他幣別。

2026-10-08 已從 `plays1` 截圖核對以下設定，擁有者確認 `plays10` 除售價外採用相同設定：

- Markets：全球市場，包含自動加入未來新增市場。
- Visibility：`Public audience`，可顯示於主 App 的 Microsoft Store 頁面。
- Release：`2027-01-01 00:00 UTC`（台灣時間 `2027-01-01 08:00`）；擁有者確認是預定正式上市日期，不是暫時阻擋購買的日期。
- Stop acquisition：`Never`。

目前仍保留草稿，不送審或公開發布；設定排程不保證當日上市，仍須完成審核與發布。這些是附加商品的公開供應設定，不代表主 App 排程或私人測試已設定，也不代表已驗證購買。上市前的測試取得方式與排程限制須在 Store runtime 驗收時另行確認。

建立產品不代表已公開上架或完成商標查核。遊戲程式尚未套用上述正式附加商品識別。

### 隱私權政策與官網

已依目前資料處理實作整理 [Paddle Duel 隱私權政策](docs/privacy-policy.md)，聯絡信箱為 `poychang.tw@gmail.com`。產品擁有者確認內容並指定生效日期為 **2026-10-08**；[公開政策頁](https://project.poychang.net/paddle-duel/privacy.html) 已發布，移除內部備註並補充 GitHub Pages 託管資料處理說明。2026-10-08 已確認公開頁可直接讀取且與 [HTML 原始檔](sites/privacy.html) 一致。附加商品政策欄位已由擁有者回報完成；正式版本核對、主 App 的 Store 欄位與 App 政策入口仍待完成。網站發布不代表 Store 驗收，也不決定 Partner Center 隱私權 Yes／No 的答案。

[`sites`](sites) 是不需建置的 HTML／CSS 靜態官網，包含遊戲介紹、鍵盤操作、聯絡資訊、非官方產品聲明與獨立政策頁。沒有 JavaScript、表單、分析追蹤、第三方字型或外部嵌入資源；GitHub 作為託管商仍可能處理訪客連線資料。

#### 發布到 GitHub Pages

GitHub Pages 的 **Deploy from a branch** 只接受分支根目錄或 `/docs`，不能直接選 `/sites`。本專案因此提供 [Pages workflow](.github/workflows/pages.yml)，只上傳 `sites`，不發布程式碼、測試或政策內部備註。

1. 由擁有者將本次 commit push 到 `main`（開發助手不自動 push）。
2. 在 GitHub repository → **Settings → Pages → Build and deployment → Source** 選 **GitHub Actions**，不必再產生另一份 workflow。
3. 在 **Actions → Deploy Paddle Duel website → Run workflow** 選 `main` 執行。之後 `main` 上的網站、政策或網站測試變更也會自動觸發。
4. workflow 先執行網站測試，再部署。若 repository 設定了 environment approval，依 GitHub 提示核准 `github-pages`。
5. 部署成功後，以 GitHub 顯示的實際網址確認首頁與政策頁可在未登入狀態閱讀，再填入 Partner Center。

目前使用擁有者設定的自訂網域：

- 官網：[https://project.poychang.net/paddle-duel/](https://project.poychang.net/paddle-duel/)
- 政策：[https://project.poychang.net/paddle-duel/privacy.html](https://project.poychang.net/paddle-duel/privacy.html)
- 聯絡區：[https://project.poychang.net/paddle-duel/#contact](https://project.poychang.net/paddle-duel/#contact)

2026-10-08 已透過無登入的 HTTPS 請求確認首頁、政策、CSS 與 favicon 均回應 HTTP 200，且內容與本機檔案一致。本機 `sites` 的內容發布於 `/paddle-duel/`，公開網址不包含 `/sites/`。網站使用相對連結，不需因網域變更修改資源路徑；保留既有 DNS／Pages 設定，不為此新增會改變專案路徑的 `CNAME`。

#### 本機預覽與驗證

```powershell
python -m http.server 8765 --bind 127.0.0.1 --directory .\sites
# 瀏覽 http://127.0.0.1:8765/，按 Ctrl+C 停止。
```

測試使用 Node.js 24 內建 test runner，不需 npm install：

```powershell
node --test .\tests\site.test.mjs
```

政策更新時，同步修改 [Markdown 內容](docs/privacy-policy.md) 與 [公開 HTML](sites/privacy.html)；測試會比較公開正文的段落、表格、日期與連結，避免兩份內容不同步，也檢查本機連結、錨點與不載入外部資源。生效日期變更時需同步更新測試中的預期日期。

已在本機驗證 320／390／768／1440 像素視窗、首頁與政策往返、鍵盤跳至主內容及 project 子路徑資源載入。公開頁只提供資訊，沒有購買或下載功能；Store 上架後再更新首頁狀態與正式入口。

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

遊戲 App 的 MSIX 已關聯正式 Store identity 與封裝圖示；正式附加商品識別、Store 列表宣傳素材與交易驗收仍待完成。本機簽署封裝不可視為已能購買正式商品。

Store commerce 已具備平台無關的商品、餘額、購買與 fulfillment gateway fake，以及可編譯的 `StoreContext` adapter。
Entitlement coordinator 已實作免費優先與 1-play／10-play fallback；真實 Store fulfillment 尚未驗收。
免費額度耗盡時，付費局開始前會先查詢 Store balance，並將選定商品 pool 保存於 entitlement session。
付費 fulfillment 會先寫入 pending journal；失敗時保留 tracking ID，供之後重試。
App 啟動與回到前景時會嘗試重試 pending fulfillment；Store 暫時不可用不會阻塞遊戲啟動。
Windows `StoreContext` adapter 已可編譯並映射 Store 結果；真實 runtime 驗證仍需 package identity 與 private flight。
Store UI 已接入商品 discovery 與購買協調器；沒有 package identity 或 Store 商品時，購買按鈕會保持停用，不會模擬購買。
Fake commerce tests 已覆蓋取消、餘額錯誤、pending retry、重啟 session 與重複 fulfillment。

## 建置與執行

需求：Windows 10 22H2 或 Windows 11、.NET 10 SDK，以及下述 Visual Studio 封裝工具鏈。

```powershell
dotnet restore PaddleDuel.sln
dotnet build PaddleDuel.sln -c Debug -p:Platform=x64
dotnet test .\tests\PaddleDuel.Tests\PaddleDuel.Tests.csproj -c Debug
dotnet build PaddleDuel.sln -c Release -p:Platform=x64
```

遊戲 App 預設為 single-project MSIX。一般 `dotnet build` 驗證編譯，不代表已產生、安裝或啟動封裝；不要再把無參數的 `dotnet run` 當成 packaged 啟動方式。Visual Studio 提供 `PaddleDuel.App (Package)` launch profile；本次執行驗證使用已簽署 MSIX 安裝及 package activation，而非 IDE F5。

### 遊戲 App 封裝

[`Package.appxmanifest`](src/PaddleDuel.App/Package.appxmanifest) 使用上節的正式 Partner Center identity；套用 identity 不等於已送審、通過認證或公開發布。

- target framework：`net10.0-windows10.0.26100.0`；Windows.Desktop 最低版本 `10.0.19045.0`。
- x64，.NET 與 Windows App SDK 都為 self-contained；不啟用 trimming／ReadyToRun。
- 預設不簽署、不產生 bundle 或 Store symbols package；以下命令產生本機 sideload 驗證用 MSIX，Store 提交用 bundle 使用下一節的獨立命令。

先執行上面的 restore，再從 repository 根目錄使用 **Windows PowerShell 5.1**：

```powershell
$vs = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products '*' -version '[18.0,19.0)' `
    -requires Microsoft.VisualStudio.Component.WindowsAppSdkSupport.CSharp `
              Microsoft.VisualStudio.ComponentGroup.MSIX.Packaging `
    -property installationPath
if (-not $vs) { throw '找不到具備封裝工具的 Visual Studio 2026。' }
& "$vs\MSBuild\Current\Bin\MSBuild.exe" `
    .\src\PaddleDuel.App\PaddleDuel.App.csproj `
    -t:Build -p:Configuration=Release -p:Platform=x64 `
    -p:GenerateAppxPackageOnBuild=true -p:UapAppxPackageBuildMode=SideloadOnly
if ($LASTEXITCODE -ne 0) { throw 'MSIX 建置失敗。' }
$package = (Resolve-Path .\src\PaddleDuel.App\AppPackages\PaddleDuel.App_1.0.0.0_x64_Test\PaddleDuel.App_1.0.0.0_x64.msix).Path
& .\scripts\Test-AppPackage.ps1 -PackagePath $package
```

[`Test-AppPackage.ps1`](scripts/Test-AppPackage.ps1) 不簽署、不安裝、不改變系統信任；驗證最終正式 identity、x64、Windows.Desktop 最低／目標版本、遊戲啟動入口、self-contained runtime 與多尺寸圖示。另以 SDK 26100 MakePri 在暫存目錄解析**封裝內**的 PRI，逐項比對兩種語言的 100 個字串與原始資源；測試結束清除暫存檔。不以來源 manifest 或 resw 的存在代替產物驗證。

MSIX 必須先以與 Publisher 相符且受本機信任的憑證簽署，才能透過 `Add-AppxPackage -Path $package` 安裝；不要直接安裝未簽署的產物。完成安裝後可透過 package activation 啟動：

```powershell
$installed = Get-AppxPackage -Name 25526PoyChang.PaddleDuel
if (-not $installed) { throw '請先完成開發封裝簽署與安裝。' }
Start-Process explorer.exe -ArgumentList "shell:AppsFolder\$($installed.PackageFamilyName)!App"
```

簽署、信任憑證與安裝必須先取得操作者同意；測試結束後僅移除該次安裝與憑證。使用正式 identity 做本機測試前，必須確認沒有既有同 identity 的 App 或資料，不能覆蓋／清除使用者安裝。本機一次性自簽憑證不是 Store 簽章，不應留作長期信任，也不可提交私鑰／憑證。

**資料邊界：** 額度、視窗設定與 pending fulfillment journal 都透過 `LocalStateDirectory` 取得路徑。Packaged 模式使用 `%LocalAppData%\Packages\<PackageFamilyName>\LocalState`；先前 `PaddleDuel.Development` 與目前正式 identity 各自獨立，不自動匯入舊開發資料。同 identity、同資料格式的本機原地升級已驗證保留狀態；跨 identity 遷移、未來資料格式變更與 Store 更新通路不包含在此結果內。

### Release x64 bundle 與 Store upload

完成相同的 restore／工具鏈準備後，從 repository 根目錄執行：

```powershell
& .\scripts\Build-AppBundle.ps1
```

[`Build-AppBundle.ps1`](scripts/Build-AppBundle.ps1) 使用 Visual Studio 2026 MSBuild 重新建置 Release x64，以 `AppxBundle=Always`／`UapAppxPackageBuildMode=StoreUpload` 產生兩種檔案，並自動驗證、輸出完整路徑與 SHA-256：

| 產物 | 用途 |
| --- | --- |
| `PaddleDuel.App_1.0.0.0_x64.msixbundle` | 含一個自包含 x64 App 的未簽署 bundle；不是可直接雙擊安裝的已信任封裝 |
| `PaddleDuel.App_1.0.0.0_x64_bundle.msixupload` | 含 Store 版本 bundle 的上傳容器，留待 Partner Center 測試提交；不把檔案產生視為已上傳／送審 |

每次輸出至新的 `BundleArtifacts\Release-x64\<run-id>\`，避免將舊產物誤認為此次建置結果；路徑與檔名以腳本輸出為準，版號來自 manifest，不自動遞增。此目錄已由 Git 忽略，勿強制加入版本控制。

- 不簽署、不建立或匯出憑證、不安裝 App、不呼叫 Partner Center，也不更動一般 build／unpackaged 的專案預設。
- 不按 Language／Scale 分包，保留兩種語言與所有圖示尺寸在同一 App；保留 SDK 的 DXFeatureLevel 分包能力，目前無此類獨立資源包。預設語言 qualifier 包含 en-US／zh-TW，英文仍排在前面。
- [`Test-AppPackage.ps1`](scripts/Test-AppPackage.ps1) 可檢查 `.msix`、`.msixbundle` 與 `.msixupload`。它解出**容器內的實際檔案**，核對 bundle／App identity、版號、架構、OS 要求、payload 大小、自包含 runtime、美術及全部中英字串。`-RequireUnsigned` 另拒絕外層或內層測試簽章。
- 同時檢查 sideload bundle 與 upload 內的 Store bundle，不假設兩條打包路徑的產物相同。暫存解包檔在檢查結束後清除。
- 不產生 symbols package；工具鏈仍會回報既有的 `mspdbcmf.exe` 缺少警告。此版本不提供 Store crash symbols，並非零警告建置。

可獨立重跑檢查器的 10 個拒絕案例（不需憑證／安裝，僅建立小型暫存 ZIP fixture）：

```powershell
& .\tests\AppPackage.Tests.ps1
& .\scripts\Test-AppPackage.ps1 -PackagePath '<實際 .msixbundle 或 .msixupload 路徑>' -RequireUnsigned
```

**2026-10-15：** Release x64 `1.0.0.0` bundle／upload 已建置並通過上述內容檢查；Windows PowerShell 5.1／PowerShell 7 的拒絕測試均通過。SDK 26100 MakeAppx 亦在未停用驗證下成功 unbundle／unpack。這不取代真實 Store 商品接線、private flight、WACK 或支援 OS／DPI 驗收；目前商品 ID 仍為開發占位，**不要將此產物直接當成已完成交易功能的正式上市版本**。

### 安裝、升級與解除安裝驗收

[`Test-AppLifecycle.ps1`](scripts/Test-AppLifecycle.ps1) 是**會變更本機系統狀態**的互動驗收工具。執行前先完成上面的 MSIX 建置；需要 Windows 互動桌面、.NET 10 或更新 SDK、SDK 26100 封裝工具，以及匯入／移除信任憑證的 UAC 同意。不要在無人值守 CI 執行。

```powershell
$package = (Resolve-Path .\src\PaddleDuel.App\AppPackages\PaddleDuel.App_1.0.0.0_x64_Test\PaddleDuel.App_1.0.0.0_x64.msix).Path
& .\scripts\Test-AppLifecycle.ps1 -PackagePath $package -AllowTemporaryInstall
```

安全限制與流程：

- 必須明確傳入 `-AllowTemporaryInstall`；存在同 identity 的 App／package 資料或遊戲程序時拒絕執行，不覆蓋玩家安裝。
- 只在獨立暫存目錄複製輸入封裝，使用 MakeAppx 建立較高版號的升級 fixture；不修改輸入 MSIX、來源 manifest 或產品版號。
- 以有效一天、不可匯出的私鑰簽署兩個 fixture，暫時信任公開憑證；完成或失敗後清除本次安裝、信任、私鑰與暫存封裝。不要中途強制終止；若 UAC 清除失敗，依輸出的 thumbprint 處理該次憑證。
- [`LifecycleProbe.cs`](scripts/LifecycleProbe.cs) 是 .NET file-based 測試程式，只接受有本次 run ID 標記的正式 package LocalState。它直接使用 production quota／journal 實作與**純記憶體 fake gateway**，不保存付費餘額、不連線 Store。
- 非空的合成 pending journal 存在時，工具拒絕啟動遊戲 App，避免測試 tracking ID 被真實 Store adapter 回報；fake 重試清空 journal 後才驗證升級版 UI。
- 此驗收採一般 `Remove-AppxPackage`，不使用保留資料選項；不手動刪除 LocalState 來製造解除安裝成功的結果。

**2026-10-15 驗證結果（Windows build 26300，正式 package family `25526PoyChang.PaddleDuel_hwmjejkjqxx7e`）：**

| 情境 | 已驗證結果 |
| --- | --- |
| 首次安裝 `1.0.0.0` | 可見 1024×720 視窗、3 次免費額度，quota／window-state 寫入 package LocalState |
| 原地升級至 fixture `1.0.1.0` | 所有 LocalState 檔案逐位元組保留，包含完成比賽 ID、最後日期、視窗設定、pending journal 與損毀隔離檔 |
| 升級後重新建立服務 | 保留 1 次剩餘額度及時鐘回撥保護；相同比賽 ID 不重複扣次 |
| fake fulfillment 重試 | 原 tracking ID 成功扣 1 unit 後清除 journal；模擬 Store 已成功但 journal 尚未清除，再重試也不重複扣次 |
| 升級版 App 重啟 | 載入 1 次額度及 960×680 設定，重複啟動仍只保留第一個程序 |
| 一般解除安裝 | 註冊與整個 package 資料目錄消失，包含仍待處理的合成 journal |
| 重新安裝 | 不復原舊 LocalState；重新取得 3 次本機免費額度及預設視窗 |
| 測試清除 | 本次 App、資料、憑證、私鑰及暫存封裝移除；舊 unpackaged 資料雜湊與檔案集合不變 |

**已知邊界：** 本機免費額度與時鐘回撥保護依賴仍存在的本機資料，不具解除安裝／重裝後的防重置保證。解除安裝也會遺失未完成交易的本機重試紀錄；不能宣稱它會繼續重試。Microsoft 管理的帳戶、訂單與付費餘額並非這些本機檔案，此次沒有真實交易或跨裝置餘額驗證。

這是同程式／同 schema、只提高封裝版號的本機升級驗收，不代表任何未來 schema 遷移都安全。Windows 10 實機、多 DPI、Store 安裝／更新、Reset／保留資料移除選項與 WACK 仍未驗收。回歸測試目前共 57 項通過。

### 雙語介面與原創素材

- [`Strings/en-US/Resources.resw`](src/PaddleDuel.App/Strings/en-US/Resources.resw) 與 [`Strings/zh-TW/Resources.resw`](src/PaddleDuel.App/Strings/zh-TW/Resources.resw) 包含選單、操作說明、設定、暫停、額度、勝負與 Store 狀態文字，以及按鈕的輔助使用名稱／工具提示。品牌名維持 Paddle Duel。
- 透過 Windows 資源選擇機制依使用者語言偏好解析，預設／最終回退語言為 `en-US`，不修改 Windows 語言、不加入 App 語言切換設定。Windows 可能將 `zh-TW` 正規化為 `zh-Hant-TW`；語言偏好變更後重新啟動 App。
- XAML 使用 `x:Uid`，動態文字由 `AppStrings` 讀取相同 PRI；缺少資源明確拋錯，不以空字串或資源鍵掩蓋缺漏。日期仍明確顯示 UTC；Store 商品名稱與 formatted price 不自行翻譯或硬編碼。
- [`New-AppAssets.ps1`](scripts/New-AppAssets.ps1) 是素材的可重建來源：原創雙球拍與方形球，不含字體或第三方圖片。從根目錄執行 `.\scripts\New-AppAssets.ps1` 可產生 100／125／150／200／400% 圖示與啟動畫面，以及 16／24／32／48／256 像素的深淺工作列圖示和多尺寸 ICO。
- Manifest 指定 splash 素材；遊戲同一視窗也在首次額度讀取期間顯示該圖形，不依賴桌面封裝一定顯示系統 splash。讀取完成或失敗顯示既有錯誤提示後立即移除，不新增動畫、固定延遲或額外視窗。快速啟動時圖形可能一閃而過或來不及顯示。

**2026-10-15 驗證：** 56 項測試通過（含雙語鍵值／格式參數對齊、XAML property 與動態資源覆蓋）。正式 identity 的本機 MSIX 簽署、安裝及 en-US／zh-TW UI Automation 通過，核對選單、輔助使用名稱、動態難度／顯示模式、操作說明、暫停、放棄不扣次及繁中 Store unavailable；已檢視兩種語言的實際畫面。測試 App、語言覆寫資料與憑證已隨測試清除；Windows 全域語言與舊開發資料不變。這不是上架審核或真實交易驗證。

本機 `dotnet` 現在預設選到 SDK 11 RC，初次 App build 在既有 collection-expression 程式碼回報兩個 `CsWinRT1032` 警告；未為此次資源工作改動 Store 邏輯。Visual Studio MSIX 建置仍有既有 symbols 工具警告。

### 先前開發 identity 驗證紀錄

**2026-10-08 驗證：**

- 53 項 .NET regression tests 通過，solution Debug／Release x64 均零警告、零錯誤。
- Release MSIX 通過產物檢查；檢查器也確實拒絕錯誤最低版本、錯誤 identity 與缺少 payload 的測試產物。
- 本機 SHA-256 簽章驗證、安裝、具 package identity 的可見遊戲視窗、windowed 首次啟動、overlay 與單一程序驗證通過。
- Package LocalState 額度／視窗設定保存及重啟載入、單人／雙人、最小化暫停、繼續、放棄不扣次及正常退出通過。
- 舊 unpackaged 資料逐位元組未變；測試安裝、信任憑證與不可匯出私鑰已清除。留下的測試 MSIX 不再受本機信任。
- MSIX 建置仍有既有 `mspdbcmf.exe` symbols 警告；Windows 10 實機、多 DPI、正式升級／解除安裝資料語義、Store 購買與 WACK 尚未驗收。

### 明確選用 unpackaged 開發模式

僅在需要舊開發資料或不安裝 MSIX 時使用；這不是 Store 驗證方式。輸出另放 `bin\Unpackaged`，不覆蓋預設 packaged 執行檔：

```powershell
dotnet build .\src\PaddleDuel.App\PaddleDuel.App.csproj -c Debug -p:Platform=x64 `
    -p:WindowsPackageType=None -p:OutputPath=bin\Unpackaged\ `
    -p:GenerateAppxPackageOnBuild=false
& .\src\PaddleDuel.App\bin\Unpackaged\PaddleDuel.App.exe
```

此模式保留 `%LocalAppData%\Arcade1972` 與既有互斥鎖；已重新驗證建置、啟動、舊額度載入及正常退出。測試後還原原有資料。2026-10-15 擁有者明確選擇繼續保留此模式，因此 identity 偵測、舊路徑 fallback 與互斥鎖仍有用途，不以完成 packaged 驗收為由移除。

### Paddle Duel 改名與相容性

改名後請開啟 `PaddleDuel.sln`，執行檔為 `PaddleDuel.App.exe`。App 視窗標題與檔案產品資訊為 `Paddle Duel`，自訂標題列與主選單為 `PADDLE DUEL`；spike 為 `PaddleDuel.PackagingSpike`，仍使用獨立測試 identity。

以下不是顯示名稱，刻意不隨專案改名：

- `%LocalAppData%\Arcade1972`：既有 unpackaged 免費額度、視窗設定與 pending fulfillment journal；直接改路徑會讓既有資料看似遺失。
- `Local\Arcade1972.SingleInstance`：保留與舊版的互斥關係，避免新舊 App 同時寫入相同資料。
- `arcade1972.play.1`／`arcade1972.play.10`：原有開發占位商品 ID，待 Store runtime 整合項目替換為已取得的真實 Store ID；封裝 identity 關聯不自動啟用交易。
- `Classic1972Rules` 與 golden regression 名稱：描述歷史規則，不是產品名稱。

2026-10-08 改名驗證：53 項 .NET 測試通過（含改名前 JSON 額度資料的回歸測試），solution Debug／Release x64 均零警告、零錯誤。Release App 已透過 UI Automation 驗證名稱、既有額度載入、舊互斥鎖、新版重複啟動、資訊／設定 overlay、全螢幕切換、單人／雙人開局、最小化暫停、繼續、放棄不扣次與正常退出；測試後還原原有本機檔案。Escape 鍵自動化受焦點影響，未列為此次通過項目，未為此改動輸入邏輯。

改名後 spike 也已重新完成 MSIX 建置、SHA-256 簽署、安裝、package identity／視窗驗證與清除。既有 `mspdbcmf.exe` symbols 警告仍在，不影響此次 spike；不代表正式遊戲已轉成 packaged 或 Store 驗收完成。

### Visual Studio 封裝工具鏈

使用 Visual Studio 2026，在 Visual Studio Installer 的「匯入組態」選擇 repository 根目錄的 [`.vsconfig`](.vsconfig)，安裝 Windows App SDK C# 支援、MSIX Packaging 與 Windows SDK 26100。需要系統管理員權限；此組態只列出封裝前置元件，不取代 .NET 10 SDK 或專案的 NuGet 參考。

安裝後可在 PowerShell 驗證同一個 Visual Studio instance 具有所有必要元件：

```powershell
$config = Get-Content .\.vsconfig -Raw | ConvertFrom-Json
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -products '*' -requires $config.components -property installationPath
dotnet build .\src\PaddleDuel.App\PaddleDuel.App.csproj -c Debug -p:Platform=x64
```

`vswhere` 應回傳安裝路徑；沒有輸出代表尚未找到具備全部元件的 instance。另確認該路徑下的 `MSBuild\Microsoft\DesktopBridge\Microsoft.DesktopBridge.targets`，以及 `%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.26100.0\x64` 下的 `makeappx.exe` 與 `signtool.exe` 存在。

2026-10-08 已在 Visual Studio Enterprise 2026 18.10.3 驗證上述元件、MakeAppx／SignTool 可執行。後續遊戲 MSIX 轉換已將 target SDK 設為 26100，但最終 Windows.Desktop 最低版本仍為 19045；target SDK 與最低支援 OS 是不同設定。

### 獨立 Packaged WinUI 3 spike

[`spikes/PaddleDuel.PackagingSpike`](spikes/PaddleDuel.PackagingSpike) 是不加入遊戲 solution 的最小封裝實驗，使用測試 identity，不連線 Store、不讀寫遊戲資料，也不共用遊戲的單一程序鎖。圖示為本專案產生的黑白幾何圖形，並非正式商店素材。

- .NET 10、Windows App SDK 2.5.1、x64；.NET 與 Windows App SDK 皆為 self-contained。
- Spike 的 target framework 為 `net10.0-windows10.0.26100.0`，`TargetPlatformMinVersion` 與最終 MSIX 的 Windows Desktop 最低版本為 `10.0.19045.0`；遊戲 App 的後續轉換也採用相同版本設定。
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
    .\spikes\PaddleDuel.PackagingSpike\PaddleDuel.PackagingSpike.csproj `
    -restore -t:Rebuild -p:Configuration=Release -p:Platform=x64 `
    -p:GenerateAppxPackageOnBuild=true -p:UapAppxPackageBuildMode=SideloadOnly
if ($LASTEXITCODE -ne 0) { throw 'MSIX 建置失敗。' }
```

實際簽署、安裝與 UI Automation 驗證：

```powershell
$package = (Resolve-Path .\spikes\PaddleDuel.PackagingSpike\AppPackages\PaddleDuel.PackagingSpike_1.0.0.0_x64_Test\PaddleDuel.PackagingSpike_1.0.0.0_x64.msix).Path
& .\spikes\PaddleDuel.PackagingSpike\Verify-Package.ps1 -PackagePath $package
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
- Spike 本身不驗證遊戲狀態；遊戲 App 的 packaged smoke check 結果另見上節。Windows 10 22H2、多 DPI、Store runtime 與 WACK 仍待驗收。

## 專案結構

- `src/PaddleDuel.Core`：不依賴 Windows UI 的 deterministic 遊戲規則。
- `src/PaddleDuel.Infrastructure`：可測試的檔案持久化與平台邊界實作。
- `src/PaddleDuel.App`：WinUI 3 視窗、輸入與 XAML 畫面。
- `tests/PaddleDuel.Tests`：物理、勝負、固定步進與 AI 測試。
- `spikes/PaddleDuel.PackagingSpike`：獨立的本機 MSIX 簽署、安裝與 WinUI 啟動驗證，不屬於遊戲 solution。
- `docs/game-reference.md`：歷史規則依據與目前 deterministic baseline。
- `docs/dev-knowledge.md`：開發中經驗證且可重用的技術知識，會隨專案演進汰舊更新。
