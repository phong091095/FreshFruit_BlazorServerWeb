using ASM_C6.Components.Pages.CustomerPage;
using ASM_C6.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Text;

namespace ASM_C6.Components.Pages.StorePage
{
    public partial class Login :ComponentBase
    {
        [Inject]
        private HttpClient HttpClient { get; set; }

        [Inject]
        private IOptions<ApiSetting> ApiSettingOptions { get; set; }

        private ApiSetting _apiSetting;
        private ASM_C6.Model.Customer _customer = new Model.Customer();
        private bool _isRenderCompleted;
        public string apiUrl;
        private IEnumerable<ASM_C6.Model.Customer> customers = new List<ASM_C6.Model.Customer>();

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        private IJSObjectReference jmodule;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _isRenderCompleted = true;
                jmodule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/script.js");
            }
        }

        private async Task CheckLogin()
        {
            var apiUrl = $"{_apiSetting.BaseUrl}/customers/login";
            StringContent content = new StringContent(JsonConvert.SerializeObject(_customer), Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                // Đọc phản hồi từ API, giả sử API trả về chuỗi "true" hoặc "false"
                var result = await response.Content.ReadAsStringAsync();
                bool loginSuccess = JsonConvert.DeserializeObject<bool>(result);

                if (loginSuccess)
                {
                    // Đăng nhập thành công
                    // Lưu thông tin người dùng vào session storage
                    await sessionStorageService.SaveEncryptedItemAsModelAsync<ASM_C6.Model.Customer>("isLoggedIn", _customer);

                    // Điều hướng đến trang khác sau khi đăng nhập thành công
                    NavigationManager.NavigateTo("/");
                }
                else
                {
                    // Xử lý khi đăng nhập không thành công
                    await JSRuntime.InvokeVoidAsync("alert", "Login failed. Please check your email and password.");
                }
            }
            else
            {
                // Xử lý lỗi khi gọi API thất bại
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to login. Status Code: {response.StatusCode}");
                Console.WriteLine($"Response Content: {errorContent}");

                // Hiển thị thông báo lỗi cho người dùng
                await JSRuntime.InvokeVoidAsync("alert", "An error occurred. Please try again later.");
            }
        }

    }
}
