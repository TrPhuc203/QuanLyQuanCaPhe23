using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace QuanLyQuanCaPhe23.Extensions
{
    public static class SessionExtensions
    {
        // Phương thức để lưu đối tượng vào session dưới dạng JSON
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        // Phương thức để lấy đối tượng từ session bằng cách giải tuần tự hóa chuỗi JSON
        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
