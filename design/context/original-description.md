Nội dung đề tài:
1.	Lý do chọn đề tài:
-	Từ năm 2010 trở đi, sau sự bùng nổ của định dạng âm thanh kỹ thuật số và mạng Internet, âm nhạc được tiêu thụ chuyển dần từ phương tiện vật lý (CD-Audio, đĩa vinyl, băng cassette,…) sang các phương tiện điện tử.
-	Thuở ban đầu, việc tiêu thụ nhạc qua phương tiện kỹ thuật số được phổ biến phần lớn bởi Napster (phần mềm chia sẻ nhạc P2P miễn phí) và việc chia sẻ các file .mp3. Tuy nhiên, do tính chất miễn phí, dễ sao chép từ phương tiện vật lý có bản quyền, và mức độ dễ dàng phát tán, âm thanh kỹ thuật số thường được sử dụng để phục vụ cho mục đích phát tán bất hợp pháp nhạc đã đăng ký bản quyền (thuật ngữ thường dùng là “pirate/piracy”) mà không thông qua bên đăng ký bản quyền (thường là các record label giữ bản quyền cho nghệ sĩ, thay mặt họ sản xuất và phát tán phương tiện vật lý chứa nhạc). Từ đó, nhạc kỹ thuật số được giới record label coi là xấu, hủy hoại nền âm nhạc bấy thời.
-	Sau khi các dịch vụ streaming hợp pháp ra đời, xuất phát từ các dịch vụ Radio/Podcast Internet, và sau đó là stream nhạc on-demand như Spotify, xu hướng tiêu thụ nhạc của đa số mọi người chuyển sang stream nhạc on-demand, cho phép chọn bài hát muốn nghe, bất cứ lúc nào, bất cứ ở đâu, khác với mô hình truyền thống trước đó phải nghe theo list nhạc chọn sẵn của đài phát thanh,… Mô hình mới này rất được lòng người tiêu dùng do tính tiện lợi, và những cái tên nổi tiếng nhất chính là Spotify, Apple Music,… Tuy nhiên, khi đi vào mô hình kinh doanh của Spotify,… các nền tảng này được xây dựng trên một nền móng là những ký ước hợp đồng bản quyền rất một chiều nghiêng về phía các record label lớn và shareholder, khiến phần tiền chia cho các nghệ sĩ độc lập hoặc nhỏ hơn rất nhỏ.
-	Đồng thời, do tính tiện dụng của việc nghe nhạc mà on-demand audio streaming mang lại, âm nhạc biến dần từ một tác phẩm nghệ thuật mà người nghe phải chọn lọc và thưởng thức thành một món hàng tiêu thụ hàng loạt, đồng thời do chỉ tiêu thụ nhạc qua hình thức streaming, người nghe không sở hữu một bản ghi cá nhân của bài nhạc (ownership/possession). Nếu các dịch vụ stream nhạc dừng hoạt động, người nghe không có cách nào để nghe lại bản nhạc đó nữa.
-	Hiện tại, thay thế cho Spotify, có Youtube Music đang kết hợp mô hình của Spotify với tính chất UGC (user-generated content – tập trung vào nội dùng người dùng tạo ra) nhưng không có cách để sở hữu nhạc, đồng thời cũng có Bandcamp cung cấp mô hình mua/bán nhạc trực tiếp bởi nghệ sĩ với chia lợi nhuận theo số lượng bán được, là một mô hình tương đối công bằng cho nghệ sĩ, tuy nhiên nền tảng chưa thật sự tập trung vào hướng streaming khiến việc tiêu thụ nhạc cho mặt bằng chung người dùng ít hấp dẫn hơn, đồng thời ít nhạc hơn.
-	Từ những vấn đề thực tiễn trên, sinh viên lựa chọn đề tài: “Xây dựng hệ thống phát nhạc trực tuyến trên nền tảng Cloud Native sử dụng .NET và Kubernetes” với mục tiêu trước mắt là xây dựng một nền tảng streaming, cho phép đăng tải trực tiếp không thông qua distributor, đồng thời hỗ trợ và tích hợp tính năng mua/bán nhạc.
2.	Mục tiêu:
-	Xây dựng website streaming nhạc: Phát triển website cho phép phát nhạc, tìm nhạc/nhạc sĩ, thêm và quản lý danh sách phát, xem lời bài hát (có thể có lời time-synced từ nhiều nguồn khác nhau) với giao diện dễ dùng.
-	Cho phép đăng tải nhạc trực tiếp mà không thông qua distributor, có thể thêm trạng thái “Verified” để tránh tràn lan nhạc kém chất lượng.
-	Deploy lên một nền tảng cloud qua Terraform, Kubernetes và áp dụng CI/CD vào quá trình phát triển.
-	Tích hợp CDN, caching để tránh quá tải server. Hỗ trợ transcode trước sang các định dạng codec khác nhau để hỗ trợ nhiều loại thiết bị/tình trạng mạng.
-	Xây dựng nền tảng kỹ thuật ổn định: Phát triển core backend bằng ASP.NET Core Minimal API.
3.	Phạm vi:
-	Nền tảng hỗ trợ: Triển khai trên Website.
-	Chức năng:
+ Chức năng dành cho người nghe:
o	Tìm kiếm nhạc/nghệ sĩ/album/playlist.
o	Stream nghe nhạc/album, tùy chọn chất lượng. Hỗ trợ stream lossless FLAC.
o	Danh sách chờ (playing next queue).
o	Mua nhạc/album.
o	Nhận recommend theo mix nghệ sĩ, thể loại, playlist, trending và một/nhiều playlist discovery.
o	Upload file nhạc lên thư viện cá nhân.
o	Thêm bài nhạc vào playlist hoặc/và yêu thích.
o	Report/khiếu nại nhạc.
+ Chức năng dành cho nhạc sĩ (artist):
o	Xem bài hát đã đăng.
o	Đăng bài nhạc/album mới để nghe/bán. Đánh dấu nhạc Explicit.
o	Xem số liệu về bài nhạc/album/...
o	Rút tiền lợi nhuận bán.
+ Chức năng dành cho nhạc sĩ chưa duyệt (unverified artist):
o	Tương tự như nhạc sĩ nhưng chưa được phép bán/lợi nhuận, và có độ hiện diện thấp hơn (kết quả tìm kiếm thấp hơn, ít hiển thị lên recommend hơn,…)
o	Có thể apply lên nhạc sĩ chính thức sau khi đạt được một mốc chỉ tiêu nhất định.
+ Chức năng dành cho người đại diện (label/group):
o	Quản lý nhiều artist với tính năng tương tự được liệt kê phía trên
o	Xem số liệu thống kê tổng hợp của cả label/group.
+ Chức năng dành cho người quản trị:
o	Quản lý nhạc: Quản trị viên có thể thêm, chỉnh sửa, ẩn, xóa nhạc/album.
o	Quản lý label: Transfer nghệ sĩ cho các label khác nhau quản lý hoặc tách độc lập. Tạo tài khoản label mới và quản lý các tài khoản label.
o	Quản lý artist: Chuyển trạng thái artist thành verified. Xem danh sách artist unverified đang apply.
o	Xem thống kê về hoạt động nghe/bán,…
o	Xử lý report vi phạm (khi bài hát bị dính một số lượng report nhất định)
4.	Đối tượng sử dụng:
-	Listener: Người nghe, sử dụng nền tảng để tìm kiếm, mua và stream nhạc nghe trực tiếp.
-	Artist: Nghệ sĩ độc lập đăng bài hát để stream (và bán), thu lợi nhuận từ bán.
-	Unverified artist: Nghệ sĩ chưa duyệt, có thể đăng bài hát nhưng không được bán và sẽ có giới hạn thêm.
-	Label/group: Nhóm đại diện hoặc record label, quản lý một/nhiều nghệ sĩ khác nhau.
-	Quản trị viên Hệ thống: Người có quyền cấu hình và vận hành hệ thống, bao gồm quản lý tài khoản người dùng, phân quyền cơ bản, theo dõi hoạt động hệ thống và xử lý các vấn đề kỹ thuật phát sinh trong quá trình vận hành,…
5.	Phương pháp thực hiện
-	 Tìm hiểu công nghệ: ASP.NET Minimal API, C#, PostgreSQL, Next.JS & TypeScript.
-	Tìm hiểu các công cụ hỗ trợ: Figma.
-	Quản lý code: GitHub.
-	Thu thập yêu cầu thông qua:
o	Khảo sát nhu cầu của người nghe (bản thân, bạn bè).
o	Tìm hiểu các sản phẩm tương tự hiện có trên thị trường.
-	Phân tích và xác định yêu cầu
-	Thiết kế:
o	Thiết kế đối tượng
o	Thiết kế dữ liệu
o	Thiết kế giao diện
o	Thiết kế hệ thống
-	Cài đặt
-	Kiểm thử
-	Hoàn thiện sản phẩm
6.	Nền tảng công nghệ:
-	Frontend: Nextjs, TypeScript.
-	Backend: .NET Web API.
-	Công cụ thiết kế UI/UX: Figma.
-	Cơ sở dữ liệu: PostgreSQL.
-	Authentication: JWT + Email verification.
-	Công cụ kiểm thử API: Scalar, OpenAPI spec.
-	Dịch vụ triển khai: Terraform, Kubernetes, ArgoCD,…
-	Dịch vụ thông báo: Firebase Cloud Messaging.
-	Thư viện sử dụng: Các thư viện và công nghệ trong hệ sinh thái .NET và Web.
7. Kết quả mong đợi:
-	Về chức năng:
+	Hệ thống stream nhạc ổn định, không bị giật, khó chịu.
+	Sử dụng protocol streaming để stream nhạc, không chỉ sử dụng HTTP file request.
+	UI mượt, không bị khựng, đẹp. Nếu có thể, áp dụng dynamic colors dựa trên hình album hiện tại.
+	Đáp ứng đủ các tính năng theo liệt kê ban đầu.
-	Về hiệu suất:
+	Thời gian tải trang trung bình không quá 2 giây.
+	Cho phép SEO tốt (site render sẵn cho các trang như artist, album, EP bài hát)
+	Khả năng chịu tải tối thiểu 500 lượt truy cập và stream đồng thời.
-	Về chất lượng:
+	Sản phẩm cuối cùng đáp ứng đầy đủ các tiêu chuẩn chất lượng về mã nguồn, bảo mật, và trải nghiệm người dùng.
+	Tuân thủ GDPR và bảo mật dữ liệu cá nhân.
+	Giao diện responsive trên hai hướng thiết bị chính: desktop và mobile.
+	Nếu đủ thời gian, đảm bảo phủ Unit Test càng nhiều càng tốt.
8. Hướng phát triển đề tài:
+	Hoàn thiện các chức năng quản trị nâng cao cho website.
+	Mở rộng recommend system.
+	Thêm các gói người dùng, ví dụ paywall tính năng stream lossless.
+	Phát triển ứng dụng mobile native thay vì phụ thuộc vào web.
+	Tách một vài phần cần thiết ra service riêng để dễ đường scale.
Bổ sung tính năng “thuê” nhạc, thuê đủ lần sẽ tự động coi là đã mua.
