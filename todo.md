# TODO

依照順序一次處理一個項目。每完成一項：

1. 執行該項目的測試或建置驗證。
2. 將項目從「進行中／待辦」移到「已完成」，並整理失效或重複的內容。
3. 將程式碼、測試、文件與本檔案放在同一個 Git commit。
4. 除非明確要求，不自動 push。

## 進行中

- [ ] 完成 `plays1`／`plays10` 的 Store listings 草稿，確認銷售市場、可見性與發行排程。
  - 擁有者已回報隱私權政策與 USD 售價設定完成；下一步填入商品名稱、說明與語言。
  - 目前不送審或公開發布；正式商品查詢、購買與 fulfillment 仍須後續 Store runtime 驗證。

## 待辦

### Packaged App 轉換

- [ ] 將目前 unpackaged WinUI 3 App 轉為 Packaged MSIX，設定 Windows Desktop `MinVersion=10.0.19045.0`。
- [ ] 關聯 Partner Center identity，加入正式圖示、啟動畫面與 zh-TW／en-US resources。
- [ ] 驗證 packaged LocalState 分支、安裝、啟動、升級與解除安裝；移除不再需要的 unpackaged workaround。
- [ ] 產生 Release x64 MSIX bundle，且不提交開發憑證或 AppPackages 產物。

### Store 與發行驗收

- [ ] 透過 Partner Center private flight 驗證 1／10 units、取消、重複購買、斷線恢復及跨裝置餘額。
- [ ] 以可退款的小額真實交易驗證 Microsoft 帳戶禮品卡可由 Store checkout 使用；App 不接觸卡號。
- [ ] 完成 IARC 分級，並將已部署的聯絡／政策網址及非官方產品聲明填入適用的 Store 欄位。
- [ ] 核對正式版資料處理與已發布隱私權政策一致，加入 App 政策入口並完成 Store 欄位驗收。
- [ ] 製作 Store 圖示、螢幕截圖、商品文案及價格／市場設定。
- [ ] 建立 Windows CI：restore、tests、Release x64 build 與 MSIX bundle artifact；簽署資訊只存於受保護 secrets。
- [ ] 在 Windows 10 22H2、Windows 11 與 100／150／200% DPI 執行安裝、升級與操作驗收。
- [ ] 執行 Windows App Certification Kit 與至少 2 小時 soak test，涵蓋連續賽局、暫停、視窗切換、音效與 fulfillment。

## 已完成

- [x] 在 Partner Center 建立 `plays1`／`plays10` Store-managed consumable，確認 Quantity 1／10 並設定售價。
  - Store ID 分別為 `9NGSHZT4WSR1`／`9MZ6KPM0339W`；Content type 為 `Electronic software download`。
  - 2026-10-08 擁有者回報售價設定為 0.99 USD／1.99 USD，分別是整包 1 次／10 次的價格；不代表已發布或可購買。
- [x] 將已發布的政策網址填入附加商品隱私權設定。
  - 2026-10-08 擁有者回報 `plays1`／`plays10` 均已設定；`plays10` 截圖顯示 Yes 與 `https://project.poychang.net/paddle-duel/privacy.html`。
  - 主 App 的政策欄位、App 政策入口與正式版本資料處理驗收仍待完成。
- [x] 統一 Paddle Duel 專案名稱：`PaddleDuel.sln`、`PaddleDuel.*` 專案／目錄／namespace／assembly、App 顯示名稱與 packaging spike；同步文件與驗證腳本。
  - 保留舊本機資料目錄、互斥鎖與交易占位識別，避免改名造成資料遺失或跨版本多開；新增舊 JSON 額度讀取回歸測試。
  - 53 項 .NET tests 通過，Debug／Release x64 建置零警告、零錯誤；Release App UI Automation 驗證單人／雙人、最小化暫停、繼續、放棄不扣次、設定與正常關閉。
  - 改名後 spike 的簽署、安裝與啟動通過，測試憑證與安裝已清除；仍有既有 symbols 警告。Escape 鍵自動化受焦點影響，未列為通過項目；遊戲輸入邏輯未變更。
- [x] 確認自訂網域官網與隱私權政策已公開發布；首頁、政策、CSS 與 favicon 皆回應 HTTP 200 且與本機內容一致。正式政策網址為 `https://project.poychang.net/paddle-duel/privacy.html`。
- [x] 配合 GitHub repository 更名為 `poychang/paddle-duel`，更新本機 origin 與文件中的 Pages 網址。
- [x] 建立 `sites` 靜態官網與隱私權政策頁，包含操作說明、聯絡資訊與非 Atari 官方產品聲明；提供只發布 `sites` 的 GitHub Pages workflow。
  - 本機網站測試通過，已驗證 320／390／768／1440 像素版面、政策導覽、鍵盤跳轉與 project 子路徑；擁有者已完成公開部署。
- [x] 檢視現有本機資料與 Store 呼叫，草擬 Paddle Duel 隱私權政策；內容已確認並發布，附加商品政策設定已完成。
- [x] 在 Partner Center 建立 App identity，取得 Publisher、Package identity 與 Store ID。
  - 2026-10-08 由產品擁有者提供 Paddle Duel 的四個識別欄位，原值保存於 [README](README.md#partner-center-產品識別)。
  - 遊戲 App 關聯、封裝上傳及 Store runtime 驗證仍由後續項目處理；附加商品的建立與草稿設定另列追蹤。
- [x] 建立最小 Packaged WinUI 3 spike，驗證 .NET 10、Windows App SDK、x64 簽署、安裝與啟動。
  - 獨立 spike 使用 .NET 10／Windows App SDK 2.5.1 self-contained MSIX，不修改遊戲 App 或 solution。
  - 已檢查最終 manifest 的 Windows.Desktop 最低版本 19045；於 OS build 26300 完成本機 SHA-256 簽章、安裝與 UI Automation，runtime 為 `.NET 10.0.12 | X64`。
  - 驗證腳本與重跑步驟已保存；測試安裝、憑證與私鑰已移除，未提交封裝或憑證。建置有不影響此 spike 的 `mspdbcmf.exe` symbols 警告；Windows 10／Store 驗收仍待後續項目。
- [x] 透過 Visual Studio Installer 安裝並驗證 Windows App SDK C#、MSIX Packaging 與 Windows SDK 26100。
  - Visual Studio 2026 18.10.3 安裝成功且不需重開機；以 `.vsconfig` 保存可重用的元件 ID。
  - 已驗證 WinUI 範本、DesktopBridge targets、x64 MakeAppx／SignTool 可執行及 App Debug／Release x64 建置（零警告、零錯誤）。
  - 目前 catalog 不提供舊 SDK 19041；使用 SDK 26100 工具且保留既有 App 目標版本。實際 MSIX 簽署、安裝與啟動由下一個 spike 驗證。
- [x] 將 pending fulfillment retry 接入 App 啟動與回前景 lifecycle；Store 不可用時不阻塞啟動，journal 保留待下次重試。
- [x] 實作賽後 consumable fulfillment、固定 tracking ID 與 pending journal；失敗紀錄可在 coordinator 重建後重試。
- [x] 付費局開始前查詢 Store balance，免費耗盡才選擇付費 pool，並將選定 Store ID 保存於 entitlement session。
- [x] 將購買協調器接入 Store UI，商品名稱／formatted price 由 Store 提供；取消、錯誤與 Store unavailable 狀態安全呈現且不自行加值。
- [x] 實作購買協調器核心，以原子 busy guard 防止重入，映射 Store 錯誤並允許失敗後重試。
- [x] 實作 `StoreContext` adapter，將 Store 商品名稱、formatted price、餘額、購買與 fulfillment status 轉成共用 gateway contract；真實 Store runtime 仍待 package identity／private flight。
- [x] 以 fake clock／gateway 覆蓋免費／付費切換、餘額不足、取消、Store error、重啟 session、pending retry 與重複 callback。
- [x] 實作 `IPlayEntitlementService`，免費額度優先，付費 fallback 依序選擇 1-play 與 10-play pool，並以固定 tracking ID 回報完成。
- [x] 定義 Store 商品／餘額／購買／fulfillment 結果模型、`IStoreGateway` 與 fake gateway，含商品查詢錯誤狀態與 tracking ID 冪等測試。
- [x] 建立 `Classic1972Rules` golden regression baseline。
- [x] 建立統一設定 overlay，整合 AI 難度與 Windowed／Full Screen 顯示模式。
- [x] 加入 Easy／Medium／Hard AI 難度選擇；只調整 controller 反應與瞄準參數，不改變共用物理規則。
- [x] 保存並還原視窗大小、位置與玩家明確選擇的顯示模式，依目前 DisplayArea 工作區校正 DPI／螢幕變更。
- [x] 將 completed-match 歷史限制在最近 400 個 UTC 日，保留回撥基準日期與跨午夜 session 語義。
- [x] 以 per-user named mutex 限制 unpackaged App 單一程序，避免多開程序同時操作免費額度 LocalState。
- [x] 視窗失焦或最小化時沿用暫停 overlay、清除 held input，回到前景後必須明確按 Continue。
- [x] 實作 Escape 暫停、繼續與放棄比賽；放棄會捨棄 quota session 且不扣次。
- [x] 額度耗盡時停用單人／雙人開局，並顯示不含假價格或假交易的 Store 占位畫面。
- [x] 在主選單顯示今日剩餘免費次數與下一個 00:00 UTC 重置日期。
- [x] 建立可汰舊的 `docs/dev-knowledge.md`，並將關鍵學習紀錄納入每項工作的完成檢查。
- [x] 將免費額度接到單人與雙人開局及賽果流程；開局建立 session，完成比賽才扣除。
- [x] 實作 Windows LocalState 額度儲存，使用原子替換並隔離損毀或無效的 JSON 狀態。
- [x] 實作每日免費 3 次的純核心模型，涵蓋 UTC 重置、完成才扣、跨日歸屬、回撥保護與冪等完成。
- [x] 建立 repository 專用 `AGENTS.md`，固定架構、產品、驗證與 Git 工作流程。
- [x] 建立 .NET solution、遊戲核心、WinUI 3 App 與 xUnit 測試專案。
- [x] 實作 120 Hz 固定步進、11 分勝負、八段反射、回擊加速與失分重設。
- [x] 實作本機雙人模式與具反應延遲的單人 AI。
- [x] 建立預設非全螢幕的自訂標題列視窗，以及玩家主動切換全螢幕功能。
- [x] 加入資訊按鈕、Player 1／Player 2 操作說明與閱讀時自動暫停。
- [x] 完成 Debug／Release 建置、核心測試與 WinUI 視窗啟動驗證。
