# Gmail Thread Transcript (Cleaned)

## Thread Metadata
- Subject: Về việc xin thực hiện Đồ án 1 học kỳ 2 năm học 2025-2026
- Participants:
  - Hồ Nguyên Minh <23520923@gm.uit.edu.vn>
  - Trần Thị Hồng Yến <yentth@uit.edu.vn>
- Reported message count: 15
- Source: Converted from PDF/OCR-like raw extraction

---

## Message 01
**From:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**To:** yentth@uit.edu.vn  
**Date:** Sun, Dec 28, 2025 at 9:42 AM

Em kính chào cô,

Em là Hồ Nguyên Minh (23520923), em hiện đang học lớp Công nghệ Web và ứng dụng của cô (SE347.Q14).

Em gửi thư này xin phép cô được thực hiện Đồ án 1 kỳ tới (kỳ 2 2025-2026) với cô.

Em có dự tính sẽ thực hiện một dự án stream nhạc trực tuyến (Spotify, Youtube Music), backend .NET và frontend có thể web hoặc native mobile. Em dự tính deploy app lên K8s, với IaC (Terraform), và nếu đủ thời gian sẽ cố gắng phủ test coverage cho unit test.

Tuy nhiên, ý tưởng trên chỉ là dự tính ban đầu, nếu cô có góp ý em có thể thay đổi đồ án ạ.

Em biết hiện đã có nhiều bạn khác mail cô xin đăng ký đồ án từ trước, nhưng dù nếu cô không nhận em cũng mong được nhận ý kiến của cô về đồ án.

Em xin cảm ơn cô,

Hồ Nguyên Minh

---

## Message 02
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Wed, Dec 31, 2025 at 12:16 PM

Chào Minh,

Dự án thể hiện tư duy kỹ thuật hiện đại, bám sát các tiêu chuẩn công nghiệp khi kết hợp sức mạnh của .NET với hệ sinh thái Cloud Native. Việc sử dụng Kubernetes (K8s) và Terraform (IaC) không chỉ giúp hệ thống vận hành ổn định, dễ mở rộng mà còn khẳng định năng lực triển khai hạ tầng chuyên nghiệp của sinh viên.

Định hướng phát triển:

- Hạ tầng: Hoàn thiện quy trình CI/CD tự động từ khâu đẩy code đến triển khai lên K8s nhằm tối ưu hóa vòng đời sản phẩm.
- Tính năng: Nghiên cứu giao thức phát trực tuyến như HLS để tối ưu tốc độ truyền tải nhạc và trải nghiệm người dùng.
- Chất lượng: Ưu tiên phủ Unit Test cho các logic quan trọng và tích hợp các công cụ giám sát (Prometheus/Grafana) để theo dõi sức khỏe hệ thống trên K8s.

Đây là nền tảng vững chắc để phát triển thành một hệ thống microservices hoàn chỉnh trong tương lai khi phát triển lên Khóa luận tốt nghiệp.

Lộ trình xây dựng và phát triển gợi ý:

- Đồ án 1: Xây dựng nền tảng (Foundation)
  - Mục tiêu là hoàn thiện luồng nghiệp vụ cơ bản và làm quen với hạ tầng Cloud Native.
  - Backend & DB: Thiết kế Database (SQL Server/PostgreSQL), xây dựng API CRUD cho bài hát, nghệ sĩ và danh sách phát bằng .NET (Clean Architecture).
  - Streaming: Triển khai stream nhạc đơn giản bằng cách trả về luồng dữ liệu (stream) từ Object Storage (MinIO).
  - DevOps: Đóng gói ứng dụng bằng Docker. Sử dụng Terraform để dựng cụm K8s (local hoặc cloud) và deploy thủ công.
  - Frontend: Web cơ bản (React) để phát nhạc và quản lý thư viện cá nhân.

- Đồ án 2: Tối ưu hóa và hệ thống hóa (Optimization)
  - Tập trung vào hiệu suất, trải nghiệm người dùng và tự động hóa vận hành.
  - Streaming nâng cao: Chuyển đổi sang giao thức HLS (HTTP Live Streaming) để nhạc phát mượt mà, hỗ trợ thay đổi chất lượng theo tốc độ mạng.
  - Caching & Search: Tích hợp Redis để cache các bài hát phổ biến và Elasticsearch để tìm kiếm nhanh theo tên, lời bài hát.
  - CI/CD: Xây dựng luồng tự động (GitHub Actions/GitLab CI) để tự động chạy Unit Test và deploy lên K8s mỗi khi có thay đổi code.
  - Frontend: Nâng cấp giao diện chuyên nghiệp hơn hoặc phát triển bản Native Mobile (Flutter/React Native).

- Khóa luận tốt nghiệp: Quy mô và trí tuệ nhân tạo (Scalability & AI)
  - Đây là giai đoạn đưa dự án lên tầm một sản phẩm thực tế với độ phức tạp cao.
  - Microservices: Tách các dịch vụ (Auth, Streaming, Payment, Notification) thành các microservices độc lập để dễ dàng mở rộng.
  - Hệ thống gợi ý (Recommendation System): Áp dụng Machine Learning để gợi ý nhạc theo sở thích người dùng (User-based/Content-based Filtering).
  - Observability: Cài đặt Prometheus & Grafana để giám sát hiệu năng hệ thống trên K8s; tích hợp ELK Stack để quản lý log.
  - Security: Triển khai API Gateway (Ocelot hoặc Kong), bảo mật bằng OAuth2/OpenID Connect và quản lý Secrets qua Vault.

Em tham khảo lộ trình gợi ý trên hoặc đề xuất lộ trình khác phù hợp thời gian, nhân lực và năng lực để cô góp ý thêm nhé!

Cô Yến.

*Quoted text hidden in original thread view.*

---

## Message 03
**From:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**To:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**Date:** Thu, Jan 1, 2026 at 5:54 PM

Em chào cô.

Em chưa có ý định làm khóa luận tốt nghiệp nên tạm thời kế hoạch của em không bao gồm phần đó mà chỉ xoay quanh DA1 và DA2.

Em dự tính DA1 sẽ làm hệ thống backend monolith (cố tình không microservice), tuy nhiên các service dễ bottleneck như stream nhạc, statistics cơ bản... sẽ được tách ra để dễ scale trên K8s.

Backend của em sẽ stream nhạc qua các protocol chuyên stream từ ban đầu (có thể là HLS) chứ không chỉ serve file tĩnh qua object storage gửi HTTP raw, do em có dự tính mở rộng stream cả MV đi kèm bài hát.

Em sẽ làm CI/CD từ đầu với GitOps, và các best practice như quản secrets bằng vault... nếu kịp em sẽ thêm observability qua OpenTelemetry. .NET Aspire trông khá mới, nên em sẽ xem qua thử, nhưng em chưa rõ sẽ dùng không do công nghệ khá mới.

Hiện tại em chưa tìm được ứng dụng khác cho Redis ngoài việc invalidate token, em không muốn cache file trên đó do sẽ có dự định dùng CDN.

Với DA2 em dự tính refactor backend thành microservice để theo đúng hướng phát triển thông thường của các dự án thực tế, chuyển sang microservice khi nghiệp vụ phức tạp hóa. Sau đó, em sẽ tìm cách stream nhạc từ CDN (có quản quyền qua token để tránh lỗi bảo mật), và thêm hệ thống statistics cụ thể bằng các tool như Kafka/Spark...

Nếu kịp em cũng muốn làm hệ thống lyrics có hỗ trợ hiệu ứng karaoke (hoặc có thể dùng các lib mở như LibLRC). Có thể em sẽ tham khảo implementation của extension BetterLyrics cho Youtube Music.

Phía trên là dự tính của em ạ. Em cảm ơn cô.

*Quoted text hidden in original thread view.*

---

## Message 04
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Sat, Jan 3, 2026 at 12:04 AM

Chào Minh,

- Về chiến lược kiến trúc (ĐA1 -> ĐA2):
  - Hướng đi Monolith -> Microservices là quyết định đúng.
  - Kế hoạch không làm Khóa luận tốt nghiệp (KLTN): tùy em quyết định. Tuy nhiên, khối lượng công việc em hoạch định cho ĐA2 (Microservices + Kafka/Spark + CDN security) thực chất tương đương KLTN. Nếu em làm tốt ĐA2 thì sản phẩm đó đủ chất lượng để bảo vệ KLTN. Em nên cân nhắc lại thêm về việc thực hiện KLTN.

- Nhận xét ĐA1:
  - Về Streaming Protocol (HLS):
    - Điểm cộng: Việc chọn HLS (HTTP Live Streaming) thay vì gửi file raw là điểm cộng lớn nhất. Nó cho phép Adaptive Bitrate Streaming (tự điều chỉnh chất lượng mạng).
    - Lưu ý kỹ thuật: Để làm được HLS, backend .NET của em sẽ phải tích hợp FFmpeg để transcode file upload (mp3/wav/mp4) thành các file .m3u8 và .ts. Quá trình này rất tốn CPU, nên hãy xử lý bất đồng bộ (Background Job). Khi user upload xong, đẩy job vào hàng đợi (Queue), worker xử lý xong mới báo "Ready". Đừng bắt user chờ transcode xong mới được lưu.

  - Về DevOps (K8s, GitOps, Vault):
    - Cảnh báo: Em đang ôm đồm quá nhiều thứ về Infra cho ĐA1. Setup K8s + Vault + GitOps (ArgoCD?) chạy mượt mà tốn rất nhiều thời gian. Hãy chắc chắn Application (logic code) chạy tốt trên Docker Compose trước. K8s chỉ là nơi deploy. Đừng để việc cấu hình K8s chiếm 80% thời gian làm đồ án trong khi tính năng nghe nhạc lại sơ sài.
    - .NET Aspire: Em nên thử để giải quyết bài toán orchestration cho .NET cloud-native apps. Nó sẽ giúp em visualize logs, traces, metrics giữa các service (Backend, Redis, Postgres) ngay trên local mà không cần setup Prometheus/Grafana phức tạp từ đầu.

  - Về Redis:
    - Em nói: "chưa tìm được ứng dụng khác cho Redis ngoài việc invalidate token". Đây là một sự lãng phí tài nguyên lớn.
    - Redis không chỉ để cache file (binary), mà để cache metadata.
    - Khi user mở app, họ load danh sách "Top 100 bài hát", "Thông tin Artist", "Album detail". Những dữ liệu này là text/json, truy xuất từ SQL Server/PostgreSQL rất tốn kém nếu có hàng nghìn request.
    - Hãy cache các query database nặng vào Redis (VD: JSON của Home Screen). Với chiến lược cache-aside sẽ giúp backend "nhẹ gánh" và scale được.
    - Ngoài ra, Redis cũng dùng cho Rate Limiting (chống spam API), Pub/Sub (cho tính năng realtime lyrics sau này).

- Nhận xét ĐA2:
  - Big Data (Kafka/Spark):
    - Kafka: Hợp lý để làm Message Broker xử lý lượt nghe (logs) với lưu lượng lớn.
    - Spark: Nếu dữ liệu chưa đến mức hàng triệu record/ngày thì Spark là quá nặng nề (cần cluster riêng, RAM lớn). Em có thể dùng Kafka Streams hoặc đơn giản là Time-series Database (như InfluxDB hoặc TimescaleDB) để làm hệ thống Statistics/Analytics. Nó nhẹ hơn và phù hợp với quy mô Đồ án 2 hơn Spark.

  - CDN & Security:
    - Từ khóa em cần tìm hiểu là Signed URLs (hoặc Signed Cookies) của CloudFront (AWS) hoặc Cloudflare. Backend sẽ ký một link có hạn sử dụng (ví dụ 1 tiếng) để frontend play.

  - Lyrics & Karaoke:
    - Dùng file .lrc là chuẩn. Logic sync thời gian giữa client (Frontend) và nhạc đang chạy là một thử thách về xử lý bất đồng bộ trên Frontend (JS/Native), không liên quan nhiều đến Backend.

- Đánh giá mức độ khả thi:
  - Đánh giá: Tốt về mặt định hướng.
  - Rủi ro: Em tập trung quá nhiều vào hạ tầng (K8s, Vault, Terraform) và kiến trúc (Microservices) mà có thể quên mất trải nghiệm người dùng (UX) và tính năng nghiệp vụ (Playlist, Search, Recommendation).

- Lời khuyên:
  - ĐA1: Tập trung làm mượt tính năng HLS Streaming và metadata caching (Redis). Infra chỉ cần Docker Compose hoặc K8s cơ bản; khoan làm Vault/GitOps nếu thấy không đủ thời gian.
  - Tận dụng Redis để cache dữ liệu JSON (metadata), đừng chỉ dùng để revoke token.
  - Thử nghiệm .NET Aspire để đơn giản hóa việc setup môi trường dev.
  - Bổ sung một số tính năng nghiệp vụ để lấy điểm cộng (Playlist, Search, Recommendation).

Em suy nghĩ cân nhắc và có thể ghép nhóm với 1 thành viên khác (nếu muốn) để cùng thực hiện đề tài nhé.

Cô Yến.

*Quoted text hidden in original thread view.*

---

## Message 05
**From:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**To:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**Date:** Sat, Jan 3, 2026 at 4:59 PM

Em chào cô.

Trước hết, em xin cảm ơn cô đã cho em thêm nhận xét về đồ án và hướng đi.

Về KLTN, do em học trễ DA1 (kỳ 2 năm 3 mới học), nên nếu làm KLTN em sẽ buộc phải ra trường sau đúng 4 năm (8 kỳ), dù em đã đủ điều kiện ra trường sau kỳ 1 năm 4. Chính vì thế em quyết định không làm/nộp KLTN mà học 3 môn thay thế. Nếu em có thể đăng ký KLTN cùng lúc với DA2 (có thể hơi khó), em sẽ cân nhắc thực hiện ạ.

Về vấn đề quá chú trọng infra trong DA1, em đã hiểu ý cô và em sẽ đẩy bớt về đồ án 2.

Ở đồ án 1, em sẽ chỉ deploy bằng Aspire lên dịch vụ cloud như Azure chứ không can thiệp sâu vào hạ tầng. Em sẽ tập trung vào nghiệp vụ nghe nhạc hơn, cụ thể là:

- Core feature nghe phát, playlist, share, search; phân theo artist, thể loại...
- Lyrics (có thể tích hợp lời tĩnh từ đầu trước).
- Hệ thống transcode và stream nhạc. Nếu có thể và băng thông cho phép, stream trực tiếp FLAC để hỗ trợ nhạc lossless. Transcode async trên một service riêng với RabbitMQ tránh nghẽn server chính.
- Phủ test; coverage càng cao càng tốt.
- Hệ recommendation cơ bản, có thể gồm:
  - Trending
  - Discovery (chưa có logic cụ thể ngoài random bài hát)
  - Radio bài hát, recommend thêm theo thể loại, artist...
  - Mix generated playlist (history, related, other people's history)

Em chỉ dự tính tạm hệ RS cơ bản sẽ là như thế, tuy nhiên em muốn chuyển sang RS nâng cao hơn, ví dụ dùng recommender open-source hoặc keras-rs. Hơi tiếc phần này em chưa có kinh nghiệm nên có thể sẽ không kịp, phải để lại DA2.

Sau đó, ở đồ án 2, em sẽ đưa deploy qua mô hình on-premise chạy cluster K8s thủ công, và mở rộng thêm:

- Tách thành microservice.
- Thêm lyrics time-synced.
- Stream CDN.
- Statistics và report cụ thể hơn.
- RS nâng cao hơn như đề cập ở trên.
- Listen together (sync playback của nhiều người, queue chung...) và hệ thống social/comment.

Tuy nhiên, em kẹt ở một nghiệp vụ: upload nhạc.

Nếu em cho phép tất cả mọi người up nhạc (giống Soundcloud) thì nền tảng sẽ rất hỗn hợp tạp nham, và rất nhiều "bài hát" chất lượng thấp.

Nếu em chỉ cho phép record label đăng nhạc (Spotify...) thì sẽ rất khó tiếp cận thị trường indie và artist nhỏ, khó mở rộng thư viện.

Em đang suy nghĩ sẽ hybrid: cho phép record label đã xác minh (admin verify thủ công) đăng nhạc trực tiếp, nhưng cho phép artist lẻ (lập tài khoản thoải mái, chỉ cần verify danh tính) đăng nhạc với mức độ ưu tiên thấp hơn các bài nhạc official bởi label. Sau khi đạt đủ lượt nghe/follow nhất định có thể apply lấy dấu tick để có lượt ưu tiên ngang với label. Cô nghĩ sao về cách xử lý này?

Em cảm ơn cô,

Hồ Nguyên Minh

*Quoted text hidden in original thread view.*

---

## Message 06
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Tue, Jan 6, 2026 at 10:43 PM

Hi Minh,

Em tiếp tục thực hiện Đồ án 1, 2 và học 3 môn thay thế theo kế hoạch mà không thực hiện KLTN cũng được.

Giải quyết bài toán quy trình upload nhạc (Hybrid Model): em đang đi đúng hướng với mô hình hybrid. Việc cho phép cả label và indie artist hoạt động là xu thế bắt buộc (giống Spotify đã mở Spotify for Artists, hay Soundcloud chuyển mình). Tuy nhiên, khái niệm "mức độ ưu tiên thấp" cần được cụ thể hóa bằng logic kỹ thuật và thuật toán.

A. Phân loại account (User Roles)

Em nên thiết kế hệ thống role rõ ràng:

- Listener: Chỉ nghe, tạo playlist cá nhân.
- Unverified Artist (Indie/Newbie): Đăng ký tự do, cần verify email/SĐT.
- Verified Artist: Đã đạt KPI hoặc được duyệt thủ công (có tích xanh).
- Label/Admin: Quản lý nhiều nghệ sĩ, độ tin cậy tuyệt đối.

B. Cơ chế Sandbox (Hộp cát) thay vì ưu tiên thấp

Thay vì chỉ nói là ưu tiên thấp, em hãy áp dụng cơ chế Sandbox & Visibility Scope:

- Đối với Label/Verified Artist:
  - Nhạc upload lên -> Transcode -> Public ngay lập tức.
  - Index vào hệ thống Search chính.
  - Được đề xuất bởi thuật toán Recommendation System (RS) ngay lập tức.
  - Thông báo đến người theo dõi (Push Notification).

- Đối với Unverified Artist (Indie mới):
  - Nhạc upload lên -> Transcode -> Trạng thái: Discover Mode (hoặc Unlisted).
  - Giới hạn tiếp cận: Bài hát vẫn nghe được, có link chia sẻ, nhưng không xuất hiện trên trang chủ, không hiện trên top trending chung, và không được thuật toán RS gợi ý cho user đại trà.
  - Cách thoát Sandbox: Bài hát phải tự kiếm được lượng traffic tự nhiên (artist tự share link cho fan, bạn bè). Khi đạt ngưỡng (ví dụ >1000 lượt nghe unique, >50 like), hệ thống tự động trigger job chuyển trạng thái sang Public; lúc này mới được RS quét và đề xuất.

Nếu chọn cách này em có thể:

- Chống rác hệ thống RS: Recommendation System không bị nhiễu bởi các bài test, nhạc lỗi, nhạc kém chất lượng của user mới.
- Tạo động lực cho artist: Buộc artist mới phải tự đi promote nhạc của mình ban đầu (giống thực tế).
- Giải quyết vấn đề tạp nham: Người nghe bình thường vào trang chủ sẽ chỉ thấy nhạc chất lượng (từ label hoặc indie đã qua kiểm chứng bằng view thật).

C. Vấn đề bản quyền (Copyright) - Simplified cho Đồ án

Em không thể làm hệ thống quét bản quyền như Content ID của YouTube (quá khó cho ĐA1), nhưng có thể xử lý ở mức UI/UX và policy:

- Khi artist upload, bắt buộc tick vào checkbox: "Tôi cam kết sở hữu bản quyền bài hát này. Nếu vi phạm, tài khoản sẽ bị khóa vĩnh viễn."
- Thêm chức năng Report ở trình nghe nhạc. Nếu một bài hát của indie artist bị report quá N lần -> tự động ẩn bài hát và gửi cảnh báo cho admin xem xét.

Lời khuyên cho ĐA1:

- Streaming FLAC (Lossless) tốn băng thông lớn: Nên giới hạn bitrate (ví dụ tối đa 320kbps cho user thường, FLAC cho VIP/Premium giả lập) để demo tính năng phân quyền.
- Storage Tier: Khi deploy lên Azure, chú ý chi phí. Set policy cho Azure Blob Storage: nhạc gốc (FLAC) ở Cool Tier, nhạc stream (MP3/AAC) ở Hot Tier.
- Unit Test: Với logic hybrid upload, có nhiều case hay để viết unit test (ví dụ kiểm tra trạng thái Sandbox, kiểm tra trigger promote khi đạt ngưỡng view).
- Documentation: Vẽ flowchart rõ ràng cho quy trình upload này và đưa vào báo cáo. Đồ án sẽ được đánh giá cao nếu có quy trình kiểm soát chất lượng nội dung (Quality Control Workflow).

Cách xử lý Hybrid + Sandbox như cô gợi ý sẽ giúp em cân bằng giữa làm giàu thư viện nhạc và giữ sạch nền tảng. Nó khả thi để code trong ĐA1 mà không cần AI phức tạp.

Cô Yến.

*Quoted text hidden in original thread view.*

---

## Message 07
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Sun, Feb 1, 2026 at 6:44 PM

Em làm gấp Đề cương chi tiết Đồ án 1 theo mẫu quy định chung cho ĐA1, 2 và KLTN, rồi gửi cô xét duyệt nội dung trước khi thực hiện nha.

*Quoted text hidden in original thread view.*

**Attachment:** Đề cương chi tiết khóa luận tốt nghiệp.pdf (200K)

---

## Message 08
**From:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**To:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**Date:** Tue, Feb 3, 2026 at 3:29 AM

Kính chào cô,

Em xin lỗi cô vì nộp đề cương chậm trễ ạ. Mong cô thông cảm.

Em có đính kèm trong mail này bản nháp đề cương của em ạ.

Em cảm ơn cô,

Hồ Nguyên Minh

*Quoted text hidden in original thread view.*

**Attachment:** SE121-De-cuong-chi-tiet.docx (50K)

---

## Message 09
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Tue, Feb 3, 2026 at 11:44 PM

Chào Minh,

Cô có một số trao đổi như sau:

- Streaming Audio: Đề cương chưa nói rõ cơ chế phát nhạc. Sẽ dùng Range Requests (HTTP 206) đơn giản hay HLS/DASH (Adaptive Streaming)? Nếu chỉ upload file mp3 lên rồi tải về nghe thì chưa gọi là "hệ thống phát nhạc" đúng nghĩa.
- Bảng kế hoạch thực hiện:
  - Giai đoạn "Cài đặt xử lý nghiệp vụ" (23/03 - 25/04): 1 tháng để code toàn bộ backend + frontend logic nghe nhạc + playlist + search. Khá gấp gáp nếu làm một mình.
  - Giai đoạn "Triển khai" (06/05 - 10/05): Chỉ có 5 ngày để deploy production và cấu hình CI/CD. Việc dựng pipeline CI/CD để build Docker image và push vào K8s cluster chạy ổn định thường tốn nhiều thời gian hơn thế.
  - Em xem phân bố thêm thời gian hợp lý nha.

Cô Yến.

*Quoted text hidden in original thread view.*

---

## Message 10
**From:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**To:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**Date:** Wed, Feb 4, 2026 at 1:15 PM

Em chào cô,

Về streaming audio, em đã thêm vào đề cương yêu cầu chức năng stream nhạc qua adaptive streaming.

Về bảng kế hoạch:

- Thứ nhất, em đã sửa timeline chung lại cho phân bố thêm thời gian vào hai mục thiết kế UI/UX (nhằm giảm thiểu thời gian code frontend thật sự; em không sử dụng AI "vibe code" nên một file Figma bài bản sẽ giúp em code frontend nhanh hơn).
- Thứ hai, em đã có kinh nghiệm triển khai môi trường K8s-like (ACA, Terraform) ở môn học cloud kỳ trước nên em đã có sẵn nền/template cần thiết để deploy. Em nghĩ 5 ngày để adapt sang đồ án mới này là có thể. Đồng thời, em đã có tên miền riêng và SSL certificate wildcard nên phân đoạn deploy sẽ không cần chờ cấp. Chính vì thế em nghĩ 5 ngày để deploy là đủ.

Em xin đính kèm gửi cô file đã cập nhật ạ. Em cảm ơn cô,

Hồ Nguyên Minh.

*Quoted text hidden in original thread view.*

**Attachment:** SE121-De-cuong-chi-tiet.docx (55K)

---

## Message 11
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Wed, Feb 4, 2026 at 4:04 PM

Đề cương OK rồi. Em triển khai thực hiện luôn nhé!

Cô Yến.

*Quoted text hidden in original thread view.*

---

## Message 12
**From:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**To:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**Date:** Tue, Mar 10, 2026 at 9:53 PM

Em chào cô,

Trước hết, em quên mất về việc báo cáo 2 tuần một lần trong tuần lễ tết do em tưởng tuần nghỉ không tính vào báo cáo, em có hỏi bạn và hiện giờ em mới gửi mail đầu tiên ạ, em xin lỗi cô.

Đây sẽ là mail đầu tiên báo cáo tiến độ của em, mong cô đọc và cho em ý kiến ạ.

Về tiến độ hiện tại, trong hai tuần vừa rồi sau tết, em đã tìm hiểu nền công nghệ cần thiết cho đồ án và các sản phẩm liên quan.

Thứ nhất, về nền công nghệ: do nền chính là web, em đang tìm hiểu các thư viện adaptive streaming phù hợp.

Hiện industry-standard có hai protocol chính là HLS và DASH, tuy nhiên thông tin cụ thể về hai protocol và tài liệu so sánh vẫn còn thiếu hụt, không cụ thể hoặc outdated. Theo em rút ra được, HLS lâu đời hơn và được hỗ trợ rộng rãi hơn nhưng nền tảng chính vẫn là Apple; còn DASH là chuẩn chung hơn, codec-agnostic nên dễ hỗ trợ các format mới hơn (FLAC hoặc đồng thời cả OPUS, OggVorbis...), tuy nhiên lại không được hỗ trợ bởi thiết bị Apple (chủ yếu ảnh hưởng nếu mở rộng app mobile native sau này).

Em vẫn phân vân giữa hai thư viện. Cả hai đều có thư viện client web tương ứng (hls.js và dash.js), tuy nhiên hls.js có vẻ được sử dụng nhiều hơn (khoảng 4 triệu download/tuần so với 500k của dash.js trên npm). Em sẽ hoàn thành MVP thử nghiệm stream qua hai protocol trong khoảng 2 ngày tới.

Đồng thời, em cũng đang tìm hiểu và xây dựng hạ tầng cho app. Em đã tạo thành công tài khoản Oracle Cloud, cho phép hạ tầng 4 OCPU và 24GB RAM, và đang triển khai một cluster K3s trên phần cứng đó (tạm thời 1 node, sẽ thêm node về cuối dự án để tăng khả năng chịu tải). Do đang kẹt phần ingress và tích hợp load balancer của Oracle nên vẫn chưa hoàn thiện hết; dự tính hạ tầng cơ bản xong vào ngày 11/3.

Thứ hai, về các sản phẩm liên quan và tính năng cụ thể:

- Hai sản phẩm tham khảo chính: Youtube Music (streaming side) và Bandcamp (selling side).
- Không lấy Spotify làm tham chiếu vì lý do cá nhân.
- Về YT Music: các tính năng UI và recommendation đã tốt; kết hợp extension Better Lyrics cho UI rất đẹp, theo hướng minimalism/clean.
- Nhận xét: player mặc định thiếu normalize âm lượng (tránh tăng/giảm âm lượng đột ngột khi chuyển bài). Em muốn thêm tính năng này.

Ngoài ra, em nghĩ ra một tính năng mở rộng queue truyền thống: một trang UI chuyên cho việc chọn bài cho session hiện tại.

- Trung tâm là các Album/Mixtape/EP từ thư viện nhạc.
- Có tỷ lệ nhỏ bài hát mới lạ được recommend (nếu kịp).
- Người dùng có 5-10 giây nghe thử và quyết định thêm vào danh sách chờ.

Mục tiêu là giúp người dùng chủ động chọn nội dung phù hợp mood hiện tại, thay vì phụ thuộc playlist cố định hoặc random hoàn toàn.

Về UI, em cân nhắc:

- Phương án 1: UI 3D mô phỏng đĩa nhạc xếp chồng, lật dần như lật trang sách; quẹt phải để chọn, quẹt trái để bỏ.
- Phương án 2: Giao diện hai list cuộn dọc; list trái chọn nhạc (vẫn dùng visual đĩa nhạc), list phải là danh sách đã chọn.

Phương án 2 có tính khả thi triển khai cao hơn.

Em cũng phân vân giữa:

- Minimalist design giống YTM (an toàn, quen thuộc).
- Phong cách phá cách hơn (Cyberpunk/Retro/Old school/Classical Victorian), lấy cảm hứng từ indie web, ZZZ, Reverse:1999, Star Guardian campaign.

Design mới có thể ấn tượng hơn nhưng rủi ro trễ hạn cao vì em mạnh backend/hạ tầng hơn frontend.

Thứ ba, về thanh toán và payout:

Do app có role người bán nên cần hệ thống payout (chi hộ từ tài khoản chung). Tuy nhiên các cổng thanh toán ở VN cho cá nhân/sinh viên còn nhiều rào cản (pháp nhân, phí, tính ổn định, sandbox...).

Trường hợp gần nhất là PayOS nhưng có các vướng mắc về chi phí, trải nghiệm ví nền, điều kiện thẻ, và hạn chế thử nghiệm.

Em đề xuất 1 trong 2 phương án:

- Dùng mock payment gateway tự viết để emulate API gateway phổ biến.
- Dùng PayPal sandbox (giới hạn Visa/Mastercard/PayPal).

Cuối cùng, về hệ khuyến nghị:

Do hệ recommendation thường yêu cầu runtime đặc biệt (GPU, phần cứng mạnh, stack Hadoop/Spark/Kafka/Airflow) và cần dữ liệu train ban đầu, mà đây chưa phải chuyên môn em đã rõ từ trước, nên khả năng cao em sẽ không làm sâu/kịp trong đồ án này. Em chưa rõ hướng đi phù hợp, mong cô góp ý.

Em cảm ơn cô,

Hồ Nguyên Minh.

*Quoted text hidden in original thread view.*

---

## Message 13
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Fri, Mar 13, 2026 at 4:20 PM

Chào Minh,

Cô đã xem báo cáo tiến độ 2 tuần đầu Đồ án 1 của em. Đề tài "Xây dựng hệ thống phát nhạc trực tuyến trên nền tảng Cloud Native sử dụng .NET và Kubernetes" là một đề tài khó, đòi hỏi kiến thức hạ tầng và backend vững. Báo cáo em viết có chiều sâu, thể hiện tư duy nghiên cứu công nghệ bài bản và kỹ năng nhận diện rủi ro tốt.

Dưới đây là nhận xét chi tiết và định hướng để giúp em tháo gỡ các vướng mắc hiện tại:

- Về giao thức streaming (HLS vs DASH):
  - Em tự tìm hiểu và so sánh chi tiết ưu/nhược điểm HLS và DASH rất tốt.
  - Kế hoạch viết MVP để test thử cả hai giao thức trong 2 ngày tới là thực tế.
  - Nếu định hướng hỗ trợ nhạc chất lượng cao (Lossless/Hi-Res), DASH nhỉnh hơn.
  - Nếu ưu tiên độ ổn định trên trình duyệt và tốc độ triển khai, hls.js là lựa chọn an toàn.

- Về cổng thanh toán và payout:
  - Em phân tích đúng các khó khăn thực tế ở Việt Nam đối với cá nhân/sinh viên (pháp nhân, phí, API).
  - Trong phạm vi Đồ án 1, mục tiêu chính là chứng minh luồng xử lý logic hệ thống, không phải vận hành thương mại thật.
  - Cô đồng ý với đề xuất dùng PayPal Sandbox hoặc tự viết mock payment gateway.
  - Cả hai cách đều minh chứng được khả năng xử lý giao dịch, webhook trả về, và tính toán chia tiền cho seller/creator.

- Về hệ thống khuyến nghị:
  - Em đánh giá đúng giới hạn phần cứng, thời gian và hạ tầng.
  - Không nên bỏ tính năng này, mà nên giảm độ phức tạp.
  - Gợi ý:
    - Content-based filtering: gợi ý theo thể loại, ca sĩ, BPM bằng truy vấn database.
    - ML.NET: dùng Matrix Factorization (collaborative filtering) trên tập dữ liệu nhỏ, chạy CPU bằng C# mà không cần Big Data stack.

- Về định hướng UI:
  - Nên chọn hướng an toàn cho kiến trúc cốt lõi.
  - Dành sự phá cách cho landing page hoặc chi tiết nhỏ (animation, icon) để tránh rủi ro trễ hạn.

Kết luận: Tiến độ 2 tuần đầu là tốt. Em đi đúng hướng khi xác định sớm điểm nghẽn để tìm cách giải quyết. Em hãy tiếp tục hoàn thiện MVP streaming và chọn phương án mock payment để tiếp tục theo kế hoạch, đảm bảo kịp tiến độ.

Cô Yến.

*Quoted text hidden in original thread view.*

---

## Message 14
**From:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**To:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**Date:** Mon, Apr 6, 2026 at 10:37 AM

Em chào cô,

Về thư báo cáo tiến độ tuần vừa rồi của em hơi chậm trễ do yếu tố ngoài, hiện em gửi mail để tóm tắt tiến độ ạ.

Trong quá trình làm MVP (em quyết định dùng DASH) và thiết kế requirement, em đã nghiên cứu qua về nghiệp vụ thực tế của ngành công nghiệp nhạc, và xin trình bày kết quả nghiên cứu cùng các khúc mắc.

Thứ nhất, về actors trong ngành nhạc:

- Consumer (người nghe)
- Label/Group quản lý và đại diện nghệ sĩ
- Indie artist không thuộc label
- Distributor chuyên upload nhạc lên các nền tảng lớn
- Nền tảng streaming làm trung gian giữa các bên

Vì vậy, nền tảng streaming bản chất là B2B2C. Em đề xuất tách thành hai frontend để dễ mở rộng và tối ưu UX:

- business.music.com cho luồng quản lý B2B
- music.com cho luồng nghe nhạc B2C

Một business sẽ có một organization, ít nhất một thành viên quyền admin; các quyền còn lại được phân bởi admin.

Về xác thực/phân quyền:

Một account có thể truy cập nhiều role và nhiều organization khác nhau, nên em đề xuất claims-based authentication:

- Refresh token chỉ lưu sub (user id).
- Access token cấp theo ngữ cảnh cụ thể (organization + claims).
- Redis dùng để lưu blacklist token bị invalidate.
- Backend tin cậy claims trong token để authorize.

Mô hình này phức tạp hơn auth truyền thống nhưng dễ mở rộng và dễ tích hợp social OAuth2 sau này.

Khúc mắc: Em đang cân nhắc có nên implement đầy đủ mô hình phân quyền này trong phạm vi tiến độ hiện tại hay không.

Thứ hai, về data model catalog:

Mô hình artist -> album -> songs là chưa đủ với nghiệp vụ âm nhạc thực tế.

Khái niệm release:

- Là một lần phát hành ở một mốc thời gian cụ thể.
- Có thể là album/EP/single.
- Có thể có nhiều lần phát hành lại (remaster/reissue) cùng tên và tracklist nhưng nội dung audio khác.

Vì vậy xuất hiện khái niệm release group để gom các release liên quan (ví dụ một album có nhiều đợt phát hành ở các năm khác nhau).

Khúc mắc: Có nên model quan hệ release group -> release hay giữ release độc lập để giảm độ phức tạp? Một lựa chọn trung gian là khóa ngoại nullable để chỉ dùng khi cần.

Khái niệm song/recording:

- Song là khái niệm trừu tượng.
- Một song có thể có nhiều recording khác nhau qua nhiều release (pre-release, re-record, live, remix, cover...).

Khúc mắc: Nên model recording -> song rõ ràng hay tạm giữ đơn giản để đảm bảo tiến độ?

Em cũng nhận thấy schema đầy đủ kiểu MusicBrainz quá phức tạp cho mục tiêu đồ án hiện tại, nên dự định chỉ giữ các entity chính, tạm bỏ các entity như series/work/event...

Về streaming pipeline:

- Dùng FFmpeg (có thể tách thành binary service riêng để scale).
- Input: master file upload.
- Output: nhiều rendition, encode fMP4.
- Sinh manifest DASH (.mpd), và có thể cả HLS (.m3u8) nếu mở rộng.
- Upload lên storage/CDN.
- Cấp token truy cập (JWT-like) để lấy nội dung qua Azure SAS hoặc Cloudflare token.

Về hạ tầng, em đang phân vân giữa:

- K3s tại nhà: chi phí thấp, toàn quyền.
- AKS: tích hợp Azure tốt nhưng giới hạn credit sinh viên.
- OKE Oracle: compute free tốt nhưng tích hợp Azure storage kém thuận tiện hơn.

Mong cô xem qua và cho nhận xét.

Hồ Nguyên Minh

*Quoted text hidden in original thread view.*

---

## Message 15
**From:** Trần Thị Hồng Yến <yentth@uit.edu.vn>  
**To:** Hồ Nguyên Minh <23520923@gm.uit.edu.vn>  
**Date:** Wed, Apr 8, 2026 at 3:27 PM

Chào Minh,

Cô có một số nhận xét và định hướng như sau:

- Kiến trúc frontend và xác thực:
  - Việc tách frontend B2B/B2C (business.music.com và music.com) là đúng hướng.
  - Claims-based authentication với JWT + access token theo organization + Redis blacklist là phù hợp tiêu chuẩn công nghiệp.
  - .NET Core Identity hỗ trợ claims-based auth tốt, backend có thể implement hiệu quả.
  - Tuy nhiên ở frontend nên ưu tiên MVP cho consumer trước; business portal có thể làm tối giản hoặc mock bằng Swagger/Postman để tránh trễ timeline.

- Mô hình hóa dữ liệu âm nhạc:
  - Nghiên cứu MusicBrainz của em tốt, nhưng không nên bê nguyên schema phức tạp vào đồ án cloud-native/streaming hiện tại.
  - Đề xuất đơn giản hóa:
    - Giữ entity Release với thuộc tính Type (Album/EP/Single).
    - Thêm ParentReleaseId (nullable) để mô tả remaster/rerelease.
    - Với song/recording: tập trung vào Recording (có thể đặt là Track).
    - Mỗi Track gắn file audio thực tế và thuộc một Release.
    - Nếu cần quản lý cover/remix, thêm OriginalTrackId (nullable) là đủ cho phần lớn use case MVP.

- Streaming pipeline và hạ tầng cloud:
  - Pipeline Master File -> FFmpeg -> fMP4 -> DASH (.mpd) -> Storage -> SAS Token là chuẩn cho hệ thống audio/video streaming hiện đại.
  - Tách FFmpeg thành worker node riêng trên Kubernetes và trigger bằng message queue (RabbitMQ/Kafka) là hướng triển khai tốt.

- Lựa chọn cluster Kubernetes:
  - AKS: tích hợp Azure tốt, nhưng chi phí có thể tiêu hao credit nhanh nếu chạy liên tục.
  - K3s local/home: tiết kiệm và chủ động, nhưng cần tự xử lý expose internet và persistent volume.
  - OKE Oracle: có gói free mạnh, nhưng ARM64 đòi hỏi build image đa kiến trúc (đặc biệt FFmpeg và .NET).

- Định hướng thực tế:
  - Dùng K3s ở nhà làm môi trường dev/test chính hằng ngày để tiết kiệm chi phí.
  - Khi hệ thống ổn định, đóng gói manifests/Helm chart và deploy AKS vào tuần cuối để demo.

Cô Yến.

*Quoted text hidden in original thread view.*
