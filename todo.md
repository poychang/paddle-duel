# TODO

依照順序一次處理一個項目。每完成一項：

1. 執行該項目的測試或建置驗證。
2. 將項目從「進行中／待辦」移到「已完成」，並整理失效或重複的內容。
3. 將程式碼、測試、文件與本檔案放在同一個 Git commit。
4. 除非明確要求，不自動 push。

## 進行中

- [ ] 確認並發布 Paddle Duel 隱私權政策，取得可公開閱讀的 HTTPS 網址，完成附加商品隱私權聲明。
  - 使用者於 2026-10-08 優先處理此項，以解除附加商品 Properties 的必填欄位阻塞。
  - [草稿](docs/privacy-policy.md) 已依現有資料處理程式整理；聯絡信箱由擁有者提供。待擁有者確認營運承諾、選定發布位置與生效日期，尚未發布或判定 Yes／No。

## 待辦

### MSIX 與 Microsoft Store 外部前置

- [ ] 建立 1 次與 10 次 Store-managed consumable 測試商品，確認 10 次商品可設定 10 units。
  - `plays1` Store ID：`9NGSHZT4WSR1`；`plays10` Store ID 待提供。
  - 截圖已確認 `ManagedConsumable` 與 Quantity 1／10，Content type 為 `Electronic software download`；仍待成功儲存確認，目前暫停至隱私權聲明處理完成。

### Packaged App 轉換

- [ ] 將目前 unpackaged WinUI 3 App 轉為 Packaged MSIX，設定 Windows Desktop `MinVersion=10.0.19045.0`。
- [ ] 關聯 Partner Center identity，加入正式圖示、啟動畫面與 zh-TW／en-US resources。
- [ ] 驗證 packaged LocalState 分支、安裝、啟動、升級與解除安裝；移除不再需要的 unpackaged workaround。
- [ ] 產生 Release x64 MSIX bundle，且不提交開發憑證或 AppPackages 產物。

### Store 與發行驗收

- [ ] 透過 Partner Center private flight 驗證 1／10 units、取消、重複購買、斷線恢復及跨裝置餘額。
- [ ] 以可退款的小額真實交易驗證 Microsoft 帳戶禮品卡可由 Store checkout 使用；App 不接觸卡號。
- [ ] 建立支援頁、IARC 分級與非 Atari 官方產品聲明。
- [ ] 核對正式版資料處理與已發布隱私權政策一致，加入 App 政策入口並完成 Store 欄位驗收。
- [ ] 製作 Store 圖示、螢幕截圖、商品文案及價格／市場設定。
- [ ] 建立 Windows CI：restore、tests、Release x64 build 與 MSIX bundle artifact；簽署資訊只存於受保護 secrets。
- [ ] 在 Windows 10 22H2、Windows 11 與 100／150／200% DPI 執行安裝、升級與操作驗收。
- [ ] 執行 Windows App Certification Kit 與至少 2 小時 soak test，涵蓋連續賽局、暫停、視窗切換、音效與 fulfillment。

## 已完成

- [x] 檢視現有本機資料與 Store 呼叫，草擬 Paddle Duel 隱私權政策；政策確認、發布與 Store 聲明仍在進行中。
- [x] 在 Partner Center 建立 App identity，取得 Publisher、Package identity 與 Store ID。
  - 2026-10-08 由產品擁有者提供 Paddle Duel 的四個識別欄位，原值保存於 [README](README.md#partner-center-產品識別)。
  - 僅完成 identity 取得與記錄；遊戲 App 關聯、商品建立、封裝上傳及 Store runtime 驗證仍由後續項目處理。
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
