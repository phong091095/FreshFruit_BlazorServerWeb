﻿
using ASM_C6.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace ASM_C6.Components.Pages.StorePage
{
    public partial class Index
    {
        [Inject]
        private HttpClient HttpClient { get; set; }

        [Inject]
        private IOptions<ApiSetting> ApiSettingOptions { get; set; }

        private ApiSetting _apiSetting;
        private IEnumerable<ASM_C6.Model.Food> foods = new List<Food>();
        private IEnumerable<ASM_C6.Model.FoodCategory> foodCategories = new List<FoodCategory>();
        List<ASM_C6.Model.Food> vegets = new List<Food>();
        List<ASM_C6.Model.Food> topsale = new List<Food>();

        private bool _isRenderCompleted;
        public string apiUrl;

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        private IJSObjectReference jmodule;
        private string activeTabId;

        protected override async Task OnInitializedAsync()
        {
            _apiSetting = ApiSettingOptions.Value;
            await LoadCate();
            await LoadDb();
            await LoadVegetable();
            await LoadTopSale();
        }

        private async Task LoadCate()
        {
            try
            {
                var cateUrl = $"{_apiSetting.BaseUrl}/categories";
                var caterespone = await HttpClient.GetAsync(cateUrl);
                if (caterespone.IsSuccessStatusCode)
                {
                    foodCategories = await caterespone.Content.ReadFromJsonAsync<IEnumerable<FoodCategory>>();
                    if (foodCategories.Any())
                    {
                        activeTabId = GetTabId(foodCategories.First().CategoryName);
                    }
                }
                else
                {
                    var errorContent = await caterespone.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to load categories. Status Code: {caterespone.StatusCode}");
                    Console.WriteLine($"Response Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task LoadDb()
        {
            try
            {
                var apiUrl = $"{_apiSetting.BaseUrl}/foods";
                var response = await HttpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    foods = await response.Content.ReadFromJsonAsync<IEnumerable<Food>>();
                    string rootPath = @"wwwroot\";

                    foreach (var item in foods)
                    {
                        int rootIndex = item.Image.IndexOf(rootPath);
                        if (rootIndex != -1)
                        {
                            string relativePath = item.Image.Substring(rootIndex + rootPath.Length).Replace("\\", "/");
                            item.Image = relativePath;
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to load foods. Status Code: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {errorContent}");
                    await jmodule.InvokeVoidAsync("show", "Fail to upload data.");
                    NavigationManager.NavigateTo("/", true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await jmodule.InvokeVoidAsync("show", "Fail to upload data.");
                NavigationManager.NavigateTo("/", true);
            }
        }

        private string GetTabId(string categoryName)
        {
            return categoryName.Replace(" ", "").ToLower(); // Ensure tab ID is unique and valid
        }

        private IEnumerable<Food> GetFoodsByCategory(Guid cateid)
        {
            return foods.Where(x => x.FCategoryCode == cateid);
        }

        private string GetCategoryName(Guid categoryCode)
        {
            var category = foodCategories?.FirstOrDefault(c => c.FCategoryCode == categoryCode);
            return category != null ? category.CategoryName : "Unknown";
        }

        private string GetActiveTabClass(Guid categoryCode)
        {
            var currentTabId = GetTabId(GetCategoryName(categoryCode));
            return currentTabId == activeTabId ? "active show" : string.Empty;
        }
        private async Task LoadVegetable()
        {
            ASM_C6.Model.FoodCategory vegetcate = foodCategories.FirstOrDefault(c => c.CategoryName == "Vegetable");

            if (vegetcate != null)
            {
                vegets = foods.Where(x => x.FCategoryCode == vegetcate.FCategoryCode).ToList();
            }
            else
            {
                await jmodule.InvokeVoidAsync("show", "Fail to upload vegetables data.");
                NavigationManager.NavigateTo("/", true);

            }
        }
        private async Task LoadTopSale()
        {
            topsale = foods
                .OrderByDescending(x => x.Sold)
                .Take(6)
                .ToList();
        }

        public List<OrderItem> items = [];
        private Food addfood = new Food();
        private async Task AddToCart(Guid id)
        {
            try
            {
                var apiUrl = $"{_apiSetting.BaseUrl}/foods/{id}";
                var response = await HttpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    addfood = await response.Content.ReadFromJsonAsync<ASM_C6.Model.Food>();
                    string rootPath = @"wwwroot\";

                    
                        int rootIndex = addfood.Image.IndexOf(rootPath);
                        string relativePath = addfood.Image.Substring(rootIndex + rootPath.Length - 1).Replace("\\", "/");
                        addfood.Image = relativePath;
                    }
                    // Lấy danh sách từ session
                    if (await sessionStorageService.GetItemListAsync<OrderItem>("cart") == null)
                    {
                        items = new List<OrderItem>();
                    }
                    else
                    {
                        items = await sessionStorageService.GetItemListAsync<OrderItem>("cart");
                    }
                    // Kiểm tra và cập nhật danh sách
                    var existingItem = items.FirstOrDefault(item => item.FoodCode == id);
                    if (existingItem != null)
                    {
                    existingItem.Quantity++;
                    }
                    else
                    {
                        var neworder = new OrderItem
                        {
                            FoodCode = id,
                            Quantity = 1,
                            UnitPrice = addfood.CurrentPrice
                        };
                        await sessionStorageService.AddItemToListAsync<OrderItem>("cart", neworder);
                    }

                
            }
            catch (Exception ex)
            {

            }
            }




        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {

            }
        }

    }
}