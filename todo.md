# TODO

依照順序一次處理一個項目。每完成一項：

1. 執行該項目的測試或建置驗證。
2. 將項目從「進行中／待辦」移到「已完成」，並整理失效或重複的內容。
3. 將程式碼、測試、文件與本檔案放在同一個 Git commit。
4. 除非明確要求，不自動 push。

## 進行中

- [ ] 保存並還原視窗大小、位置與玩家明確選擇的顯示模式，並處理螢幕／DPI 改變。

## 待辦

### 遊戲與體驗

- [ ] 加入自行合成的球拍、牆面與得分音效；Core 只發出事件，App 負責播放。
- [ ] 加入靜音／音量設定與本機保存。
- [ ] 加入 AI 對手難易度設定，僅調整反應間隔、瞄準誤差等 controller 參數，不改變共用物理規則。
- [ ] 加入 Xbox 控制器支援與裝置拔除自動暫停。
- [ ] 建立統一設定介面，整合音量、AI 難度、控制方式與顯示模式。
- [ ] 完成主選單、暫停、設定與 Store 畫面的鍵盤導覽、高對比及 Narrator 驗收。
- [ ] 量測可信的原機參考資料，更新 `Classic1972Rules` 與 golden regression tests。

### MSIX 與 Microsoft Store 外部前置

- [ ] 透過 Visual Studio Installer 安裝並驗證 Windows App SDK、MSIX Packaging 與 Windows SDK 工作負載。
- [ ] 建立最小 Packaged WinUI 3 spike，驗證 .NET 10、Windows App SDK、x64 簽署、安裝與啟動。
- [ ] 在 Partner Center 建立 App identity，取得 Publisher、Package identity 與 Store ID。
- [ ] 建立 1 次與 10 次 Store-managed consumable 測試商品，確認 10 次商品可設定 10 units。

### Packaged App 轉換

- [ ] 將目前 unpackaged WinUI 3 App 轉為 Packaged MSIX，設定 Windows Desktop `MinVersion=10.0.19045.0`。
- [ ] 關聯 Partner Center identity，加入正式圖示、啟動畫面與 zh-TW／en-US resources。
- [ ] 驗證 packaged LocalState 分支、安裝、啟動、升級與解除安裝；移除不再需要的 unpackaged workaround。
- [ ] 產生 Release x64 MSIX bundle，且不提交開發憑證或 AppPackages 產物。

### Store 購買架構

- [ ] 定義商品／餘額／購買／fulfillment 結果模型，以及 `IStoreGateway` 與 fake gateway。
- [ ] 實作 `IPlayEntitlementService`／`MatchChargeCoordinator`，免費額度優先，免費耗盡才選擇付費商品來源。
- [ ] 以 fake clock／gateway 測試免費與付費切換、餘額不足、取消、離線、重啟及重複 callback。
- [ ] 實作 `StoreContext` adapter，顯示 Store 回傳的商品名稱與 formatted price。
- [ ] 實作購買防重入及取消、離線、未登入、網路和伺服器錯誤介面；成功後重新查詢餘額，不自行加值。
- [ ] 付費局開始前必須連線確認 Store 餘額，並將選中的 Store ID 寫入 match session。
- [ ] 實作賽後 consumable fulfillment、固定 tracking ID 與 pending journal。
- [ ] 啟動、回前景與恢復網路時，以同一 tracking ID 冪等重試 pending fulfillment。

### Store 與發行驗收

- [ ] 透過 Partner Center private flight 驗證 1／10 units、取消、重複購買、斷線恢復及跨裝置餘額。
- [ ] 以可退款的小額真實交易驗證 Microsoft 帳戶禮品卡可由 Store checkout 使用；App 不接觸卡號。
- [ ] 建立隱私權政策、支援頁、IARC 分級與非 Atari 官方產品聲明。
- [ ] 製作 Store 圖示、螢幕截圖、商品文案及價格／市場設定。
- [ ] 建立 Windows CI：restore、tests、Release x64 build 與 MSIX bundle artifact；簽署資訊只存於受保護 secrets。
- [ ] 在 Windows 10 22H2、Windows 11 與 100／150／200% DPI 執行安裝、升級與操作驗收。
- [ ] 執行 Windows App Certification Kit 與至少 2 小時 soak test，涵蓋連續賽局、暫停、視窗切換、音效與 fulfillment。

## 已完成

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
