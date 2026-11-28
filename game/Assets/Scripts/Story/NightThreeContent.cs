namespace BaGio12.Story
{
    // Text representations until the team's approved image/audio assets arrive.
    public static class NightThreeContent
    {
        public static readonly string[] VaultIds={"intruder","album","rules","journal","knock","note","inner"};
        public static readonly string[] VaultNames={"Kẻ xâm nhập","Album 312","Quy tắc an toàn","Nhật ký","go_cua.m4a","Ghi chú 9/11","Phần trong (khóa)"};
        public const string History="2023: Bong oi ve an com\n08/2025: [Ảnh miếng vải thêu con cá bống chỉ cam, thêu dở]\nNgân: mẹ thêu xong để lót ốp cho con nhaaaa\n16–22/11, 10:00–16:00 · Ngân: Dạ. Con vẫn ổn. Mẹ đừng lo.";
        public const string HoaProfile="Hòa Nguyễn\n[Ảnh đại diện: đồng phục siêu thị]\n24/11: Ai thấy con gái tôi xin báo giúp. Nguyễn Thảo Ngân, 19 tuổi.";
        public static string VaultText(string id)
        {
            switch(id)
            {
                case "intruder":return "[Ảnh camera trước rất tối, chỉ thấy nửa dưới khuôn mặt sáng lờ mờ bởi màn hình.]";
                case "album":return "Album 312 · 14 ảnh\n03/10–31/10 · 03:12 · Rino 9\n[Ngân ngủ, góc thấp từ chân giường.]";
                case "rules":return "[Ảnh tờ ghi chú quy tắc an toàn]\nKhông để chìa ngoài hộp công tơ.\nBáo thức 3:12: dậy kiểm tra cửa.\nCó Linh ngủ lại thì tắt báo thức, tắt camera.";
                case "journal":return "05/10: Có người chụp mình ngủ. Máy Rino 9.\nChú Thành thay khóa. Linh không có lý do gì cả.\nNhờ Na canh cầu thang.\n21/10: Na mất tích.\n22/10: [Ảnh đôi dép đỏ trên kệ nhà chú Thành]\n27/10: Lại gõ cửa lúc 3:05.\n31/10: Mai thi giữa kỳ. Ngủ sớm.";
                case "knock":return "go_cua.m4a · 0:12\n[Bản chữ thay âm thanh: Cốc. ... Cốc cốc. Tiếng chốt cửa mở.]";
                case "note":return "9/11: Phần còn lại khóa bằng mã máy. Nếu con không về thì mẹ đưa cả cái máy cho công an.";
                default:return "Nhập mã khóa màn hình để mở. Còn 3 lần.";
            }
        }
    }
}
