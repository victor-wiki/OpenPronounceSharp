using SharpUtility.Helper;
using TorchSharp;
using static TorchSharp.torch;

namespace OpenPronounceSharp
{
    public class Utility
    {
        public static Device get_device()
        {
            var name = Environment.GetEnvironmentVariable("OPENPRONOUNCE_DEVICE");

            if (name is null)
            {
                name = torch.cuda.is_available() ? "cuda" : "cpu";
            }

            var device = torch.device(name);

            LogHelper.LogInfo($"Using device {device}");

            return device;
        }

        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;

            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }

            int[,] dp = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
            {
                dp[i, 0] = i;
            }

            for (int j = 0; j <= m; j++)
            {
                dp[0, j] = j;
            }                

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    int delete = dp[i - 1, j] + 1;      
                    int insert = dp[i, j - 1] + 1;     
                    int replace = dp[i - 1, j - 1] + cost; 

                    dp[i, j] = Math.Min(Math.Min(delete, insert), replace);
                }
            }

            return dp[n, m];
        }

    }
}
