# Microsoft Store 提交資料

保存可重用的商店文案、素材及設定核對結果。這些是 repository 內的紀錄，不會自動同步 Partner Center；本機 commit 不代表已上傳、送審或發布。

## 維護方式

- 處理每個欄位時同步更新對應語言文案；完成一個可核對的階段後提交文件及 [TODO](../../todo.md)。
- 明確區分「建議值」「擁有者確認已填入」「已儲存」「已發布」。以擁有者回報或實際畫面為依據，不將建議當成已完成。
- 真正製作素材時再加入此目錄下的 `assets`，按語言／用途整理；記錄檔名、像素尺寸、App 版本、擷取日期與上傳狀態。只保存精選原始素材或可重建腳本，不保存整批測試暫存圖。
- 遊戲截圖必須來自實際 App；不以示意圖冒充。封裝圖示與 Store logos 分別驗證規格。
- 不提交測試成員 email、帳戶畫面、IARC 憑證 ID、稅務／銀行資料、登入資訊或簽署憑證。MSIX／upload 仍放在 Git 忽略的輸出目錄。

## 文案與素材

- [en-US 草稿](en-US.md)：Description／Product features 已填入，整頁儲存狀態待確認。
- zh-TW：待新增語言與文案。
- Screenshots：已收到重拍的 1920×1080 英文全螢幕主選單原圖，格式與尺寸通過；見 [英文素材紀錄](en-US.md#screenshot-candidates)，尚未上傳。
- Store logos：待製作及上傳。
- Trailers：尚未決定是否製作。

## 2026-10-16 主 App 提交進度

產品為 Paddle Duel，Store ID `9NJ180X2R7TF`。正式 identity 與附加商品識別見 [專案 README](../../README.md#partner-center-產品識別)。

### Pricing and availability

擁有者確認已儲存且顯示 Complete：

- 主 App：Free，免費下載；不影響付費附加商品價格。
- Audience：Private audience，選用 `Internal Testers` 群組；不記錄成員。
- `Make this product public on`：依操作流程保持未勾選。
- Discoverability：保留 available and discoverable，受私人對象限制；不代表對所有人公開。
- 主 App 的市場範圍未在此次逐項核對，不套用附加商品的全球市場紀錄。

目前採尚未公開 App 的 Private audience 路徑，不把它與已發布 App 的 Package flights 混為一談。

### Properties

以下是協作過程中的填寫建議與回報，**尚未取得整頁儲存成功／Complete 的確認**：

| 區塊 | 建議／填寫紀錄 |
| --- | --- |
| Category | 建議 Classics；實際選取值待最後核對 |
| Display mode | 4K、HDR、VRR、Mixed Reality 不宣告；尚未完成相關驗證 |
| Game settings | PC single player；local multiplayer 最少／最多 2 人；不宣告 Xbox、split screen、online、cross-platform、co-op 或 Broadcasting |
| Product declarations | 不使用外部金流、不宣告已通過完整無障礙測試；允許其他磁碟安裝；建議停用 OneDrive 自動備份與 Windows 錄影／廣播宣告；沒有生成式 AI 功能 |
| System requirements | 擁有者回報完成此區；Keyboard minimum、Mouse recommended；其他硬體不勾，Memory／DirectX／Video memory 保持 Not specified，Processor／Graphics 留空 |

這些宣告不是額外硬體、無障礙、備份還原或錄影功能的驗收結果。主 App 的隱私權／支援聯絡欄位仍待核對。

### Age ratings

擁有者確認已完成問卷並儲存：

- Rating Type：IARC Rating；IARC Version：10.3。
- 擁有者概述各地結果多為 3+ 或 All Ages；未取得逐地區完整清單，不自行推定 ESRB／PEGI 等確切級別。
- Interactive Elements：僅 `In-Game Purchases`。
- 數位購買選 Yes，勾選 Purchases of digital goods；不含現金兌換獎勵、NFT、隨機購買或玩家間物品交易。
- 已檢視的暴力、恐怖、性內容、賭博、冒犯語言、管制物質、粗俗幽默、聊天／內容交換、精確位置分享等題目為 No。
- 未收到分級結果的逐項完整截圖；不以本紀錄替代正式 IARC 憑證。若內容或交易設計改變，重新核對問卷。

### Store listings

擁有者確認 en-US Description 與 5 項 Product features 已填入，原文見 [英文草稿](en-US.md)。圖片、繁中與其他欄位未完成；不宣稱 listing 已 Complete。

## 提交前仍需完成

- 確認 Properties 儲存狀態、主 App 政策／支援資訊及其他必填欄位。
- 完成中英文文案與實際遊戲截圖、Store logos；核對整頁儲存狀態。
- 接入正式附加商品識別，驗證購買與 fulfillment，再重建／核對提交封裝。現有 bundle 不是已完成交易功能的上市版本。
- 確認私人測試者能在附加商品公開排程前取得商品；不能由主 App 的 Private audience 推定。
- Partner Center 曾顯示稅務／收款資料待更新；由擁有者在平台內處理，不收集或存放敏感資料。
- 所有項目未完成前，不按 `Submit for certification`。尚未提交主 App、發布或驗證真實交易。

## 官方參考

- [Private audience 與 Package flights](https://learn.microsoft.com/windows/apps/publish/beta-testing-and-targeted-distribution)
- [Known user groups](https://learn.microsoft.com/windows/apps/publish/create-customer-groups)
- [Properties](https://learn.microsoft.com/windows/apps/publish/publish-your-app/enter-app-properties?pivots=store-installer-msix)
- [Product declarations](https://learn.microsoft.com/windows/apps/publish/publish-your-app/product-declarations)
