using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MyFinanceModel;
using Newtonsoft.Json;

namespace MyFinanceBackend.Utils
{
    public static class JsonUtils
    {
        public static bool JsonTryParse<T>(this string json, out T value)
        {
            if (string.IsNullOrEmpty(json))
            {
                value = default(T);
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch
            {
                value = default(T);
                return false;
            }
        }
    }

    public static class StringUtils
    {
        //Encode

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        //Decode

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }

    public static class TokenUtils
    {
        public static string SerializeToken(this BaseCustomToken token)
        {
            var jsonToken = JsonConvert.SerializeObject(token);
            var encryptToken = PasswordUtils.EncryptText(jsonToken);
            var encodedEncryptToken = StringUtils.Base64Encode(encryptToken);
            return encodedEncryptToken;
        }

        public static bool TryDeserializeToken<T>(string value, out T token) where T : BaseCustomToken
        {
            try
            {
                var decodedAction = StringUtils.Base64Decode(value);
                var decryptedAction = PasswordUtils.DecryptText(decodedAction);
                token = decryptedAction.JsonTryParse<T>(out token) ? token : null;
                return true;
            }
            catch
            {
                token = null;
                return false;
            }
        }
    }

    public static class ObjectUtils
    {
        public static T GetCopy<T>(this T source) where T : new()
        {
            var destination = new T();
            CopyFields(source, destination);
            return destination;
        }

        public static void CopyFields<T>(T source, T destination)
        {
            var fields = GetAllFields(source.GetType());
            foreach (var field in fields)
            {
                field.SetValue(destination, field.GetValue(source));
            }
        }

        private static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null)
                return Enumerable.Empty<FieldInfo>();

            return t.GetRuntimeFields().Concat(GetAllFields(t.GetTypeInfo().BaseType));
        }
    }
}
