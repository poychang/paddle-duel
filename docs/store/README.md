# Microsoft Store 提交資料

保存可重用的商店文案、素材及設定核對結果。這些是 repository 內的紀錄，不會自動同步 Partner Center；本機 commit 不代表已上傳、送審或發布。

## 維護方式

- 處理每個欄位時同步更新對應語言文案；完成一個可核對的階段後提交文件及 [TODO](../../todo.md)。
- 明確區分「建議值」「擁有者確認已填入」「已儲存」「已發布」。以擁有者回報或實際畫面為依據，不將建議當成已完成。
- 真正製作素材時再加入此目錄下的 `assets`，按語言／用途整理；記錄檔名、像素尺寸、App 版本、擷取日期與上傳狀態。只保存精選原始素材或可重建腳本，不保存整批測試暫存圖。
- 遊戲截圖必須來自實際 App；不以示意圖冒充。封裝圖示與 Store logos 分別驗證規格。
- 不提交測試成員 email、帳戶畫面、IARC 憑證 ID、稅務／銀行資料、登入資訊或簽署憑證。MSIX／upload 仍放在 Git 忽略的輸出目錄。

## 文案與素材

- [en-US 草稿](en-US.md)：Description／Product features 已填入，2026-10-16 提交概況截圖確認 Store listings 為 Complete；未發布。
- zh-TW：待新增語言與文案。
- Screenshots：擁有者選定主選單與對戰兩張 1920×1080 PNG，並於 2026-10-16 回報已上傳及確認預覽；Store listings 已顯示 Complete。保留現有檔名，見 [英文素材紀錄](en-US.md#selected-screenshots)。
- Store logos：已保存官網 favicon 轉出的 Poster／Box PNG，擁有者更新成品並回報處理完成；[素材紀錄](en-US.md#store-logos) 註明腳本與手動調整版本差異、Box art 底部覆蓋風險。Store listings 已顯示 Complete，最終 Store 呈現仍待確認。
- Trailers：尚未決定是否製作。

## 2026-10-16 主 App 提交進度

產品為 Paddle Duel，Store ID `9NJ180X2R7TF`。正式 identity 與附加商品識別見 [專案 README](../../README.md#partner-center-產品識別)。

10:23 提交概況截圖確認：Pricing and availability、Properties、Age ratings、Store listings 均為 **Complete**；Packages 為 **Not started**，Submission options 為 **Recommended**，整體仍為 **In draft**。Xbox Creators Program 為 Not started；目前產品為 Windows PC、無 Xbox services 整合，不為完成清單而啟用。Complete 僅代表提交欄位狀態，不代表通過認證或所有功能已驗收。

11:21 擁有者回報新版封裝上傳完成，截圖顯示 Packages 為 **Complete**，上傳檔案狀態為 **Validated**。Pricing and availability／Age ratings 仍為 Complete，但 **Properties／Store listings 改為 Incomplete**；整體仍 In draft。須開啟各頁確認實際缺漏，不能僅憑概況推定是語言、政策或宣告欄位造成；Validated 不等於通過 certification。

### Pricing and availability

擁有者確認已儲存且顯示 Complete：

- 主 App：Free，免費下載；不影響付費附加商品價格。
- Audience：Private audience，選用 `Internal Testers` 群組；不記錄成員。
- `Make this product public on`：依操作流程保持未勾選。
- Discoverability：保留 available and discoverable，受私人對象限制；不代表對所有人公開。
- 主 App 的市場範圍未在此次逐項核對，不套用附加商品的全球市場紀錄。

目前採尚未公開 App 的 Private audience 路徑，不把它與已發布 App 的 Package flights 混為一談。

### Properties

2026-10-16 10:23 本頁為 Complete；11:21 上傳封裝後的最新截圖顯示 **Incomplete**，原因待查看頁面提示。以下保留協作過程中的填寫建議與回報；概況狀態不取代各欄位最終值的核對：

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

擁有者確認 en-US Description 與 5 項 Product features 已填入，原文見 [英文草稿](en-US.md)；兩張截圖已上傳，logos 已回報處理完成。2026-10-16 概況截圖確認 Store listings 為 **Complete**。繁中尚未製作；沒有以 Complete 推定額外語言也已完成。

11:21 最新概況改為 **Incomplete**；尚未展開 Languages supported in packages 或各語言清單，不能推定是哪個語言或素材缺漏。

## 2026-10-16 私人測試候選封裝

- 正式 `plays1`／`plays10` ID 已接入；商品名稱／價格仍由 Store 提供，沒有本機付費餘額。
- 修復實際鍵盤焦點，雙人與單人控制、overlay、失焦、暫停與全螢幕切換通過真實按鍵驗收；測試使用 unpackaged App，未安裝新的 MSIX。
- 付費完成先保存 journal，再向 Store 扣一單位；開局前重試未結清紀錄，並行重試序列化，損毀紀錄保留並報錯。73 項 .NET 測試通過，商務情境使用 fake gateway。
- Release x64 `1.0.0.0` 未簽署 bundle／msixupload 皆通過實際 payload 檢查；位於 Git 忽略的 `BundleArtifacts\Release-x64\704aea306d5a416dab4a7fa0d43423f5\`。
- 上傳候選檔為該目錄的 `PaddleDuel.App_1.0.0.0_x64_bundle.msixupload`；SHA-256：`7AE0ED5EC7F29173DDB658CEEE74019ABC87CBCF1BB77A442090AFB526AE613E`。不要使用前一批含占位商品 ID 的封裝。
- 11:21 擁有者確認上傳完成，概況顯示檔名 `PaddleDuel.App_1.0.0.0_x64_bundle.msixupload`、Validated 與 Packages Complete；截圖未提供遠端檔案雜湊。未送審、未發布、未執行真實購買。建置只有既有缺少 `mspdbcmf.exe` 的 symbols 警告。

## 提交前仍需完成

- 核對主 App 政策／支援資訊及各欄位最終值，不能僅以 Properties Complete 推定正確。
- 繁中 listing 待製作；提交前核對英文文案、截圖與最終程式一致，並檢查 logos 的最終呈現。
- 先查看 Properties 的 Incomplete 提示並處理，再查看 Store listings 的缺漏；不把上傳封裝 Validated 當成送審通過。
- 確認私人測試者能在附加商品公開排程前取得商品；不能由主 App 的 Private audience 推定。
- Partner Center 曾顯示稅務／收款資料待更新；由擁有者在平台內處理，不收集或存放敏感資料。
- 私人測試提交前先核對封裝、政策、測試對象與 Submission options，並取得擁有者明確同意；目前不按 `Submit for certification`。真實 Store 交易驗收需要可由 Store 取得的測試版本，因此在私人測試發佈後執行，不誤列為首次私人提交前必須已通過。正式公開上市前仍須完成交易及其餘發行驗收。

## 官方參考

- [Private audience 與 Package flights](https://learn.microsoft.com/windows/apps/publish/beta-testing-and-targeted-distribution)
- [Known user groups](https://learn.microsoft.com/windows/apps/publish/create-customer-groups)
- [Properties](https://learn.microsoft.com/windows/apps/publish/publish-your-app/enter-app-properties?pivots=store-installer-msix)
- [Product declarations](https://learn.microsoft.com/windows/apps/publish/publish-your-app/product-declarations)
