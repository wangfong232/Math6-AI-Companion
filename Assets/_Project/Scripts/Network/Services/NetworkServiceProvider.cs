// Assets/_Project/Scripts/Network/Services/NetworkServiceProvider.cs
using UnityEngine;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;

namespace Math6Companion.Core.Services
{
    /// <summary>
    /// Bộ định tuyến dịch vụ tập trung (Service Provider).
    /// Cho phép chuyển đổi linh hoạt giữa Dịch vụ thực (Google Sheets + Gemini AI) và Dịch vụ Mock.
    /// </summary>
    public static class NetworkServiceProvider
    {
        private static INetworkService customNetworkService;
        private static IAITutorService customAITutorService;

        public static bool UseRealServices { get; set; } = true;

        public static INetworkService NetworkService
        {
            get
            {
                if (customNetworkService != null) return customNetworkService;

                if (UseRealServices)
                {
                    var real = GoogleSheetsNetworkService.Instance;
                    if (real != null) return real;
                }

                return MockNetworkService.Instance;
            }
            set => customNetworkService = value;
        }

        public static IAITutorService AITutorService
        {
            get
            {
                if (customAITutorService != null) return customAITutorService;

                if (UseRealServices)
                {
                    var real = GeminiAITutorService.Instance;
                    if (real != null) return real;
                }

                return MockNetworkService.Instance;
            }
            set => customAITutorService = value;
        }
    }
}
