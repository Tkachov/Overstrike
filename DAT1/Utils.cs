namespace DAT1 {
	public class Utils {
        public static string Normalize(string data)
        {
            string result = data.ToLower().Replace('\\', '/');
            string replaced = "";
            bool slash = false;

            foreach (var c in result)
            {
                if (c == '/')
                {
                    if (slash) continue;
                    slash = true;
                }
                else
                {
                    slash = false;
                }

                replaced += c;
            }

            return replaced;
        }
    }
}
