using System;
using System.Text.RegularExpressions;

namespace ASM.Helpers
{
    public static class Utilities
    {
        /// <summary>
        /// Chuyển chuỗi tiếng Việt có dấu thành không dấu và viết liền (để tìm kiếm thông minh)
        /// Ví dụ: "Cà phê Đen Đá" -> "caphedenda"
        /// </summary>
        public static string ToUnaccent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = text.ToLower().Trim();
            
            // Bảng mã ký tự tiếng Việt
            string[] combined = {
                "aáàảãạâấầẩẫậăắằẳẵặ",
                "eéèẻẽẹêếềểễệ",
                "iíìỉĩị",
                "oóòỏõọôốồổỗộơớờởỡợ",
                "uúùủũụưứừửữự",
                "yýỳỷỹỵ",
                "dđ"
            };

            foreach (var group in combined)
            {
                char baseChar = group[0];
                for (int i = 1; i < group.Length; i++)
                {
                    text = text.Replace(group[i], baseChar);
                }
            }

            // Loại bỏ hoàn toàn khoảng trắng để hỗ trợ tìm kiểu "denda" khớp "đen đá"
            return Regex.Replace(text, @"\s+", "");
        }
    }
}
