using API.Domain.DTOs;
using API.Domain.Request.ProductRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;

namespace MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private class ErrorResponse
        {
            public string message { get; set; }
        }

        private string ExtractMessage(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<ErrorResponse>(json);
                return obj?.message ?? "Có lỗi xảy ra.";
            }
            catch
            {
                return "Có lỗi xảy ra.";
            }
        }

        private async Task LoadDropdownsAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var categories = await client.GetFromJsonAsync<List<CategoryDto>>("category");
            var brands = await client.GetFromJsonAsync<List<BrandDto>>("brand");

            ViewBag.Categories = categories?.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            ViewBag.Brands = brands?.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            }).ToList();
        }
        public async Task<IActionResult> Index()
        {
            var a = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(a))
                return RedirectToAction("Login", "MVCAuth");
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync("product");

            if (!response.IsSuccessStatusCode)
                return View(new List<ProductDto>());

            var content = await response.Content.ReadAsStringAsync();
            var products = JsonConvert.DeserializeObject<List<ProductDto>>(content);

            return View(products);
        }

        // GET: Admin/Products/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var a = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(a))
                return RedirectToAction("Login", "MVCAuth");
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"product/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var content = await response.Content.ReadAsStringAsync();
            var product = JsonConvert.DeserializeObject<ProductDto>(content);

            return View(product);
        }
        public async Task<IActionResult> Create()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "MVCAuth");
            await LoadDropdownsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductRequest request)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "MVCAuth");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(request);
            }

            // 🔹 Validate các trường trước khi gửi lên API
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                ModelState.AddModelError("", "Tên sản phẩm không được để trống.");
                await LoadDropdownsAsync();
                return View(request);
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 500)
            {
                ModelState.AddModelError("", "Mô tả không được vượt quá 500 ký tự.");
                await LoadDropdownsAsync();
                return View(request);
            }

            if (request.CategoryId == Guid.Empty)
            {
                ModelState.AddModelError("", "Chọn danh mục sản phẩm hợp lệ.");
                await LoadDropdownsAsync();
                return View(request);
            }

            if (request.BrandId == Guid.Empty)
            {
                ModelState.AddModelError("", "Chọn thương hiệu sản phẩm hợp lệ.");
                await LoadDropdownsAsync();
                return View(request);
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            var form = new MultipartFormDataContent
            {
                { new StringContent(request.Name ?? ""), "Name" },
                { new StringContent(request.Description ?? ""), "Description" },
                { new StringContent(((int)request.Gender).ToString()), "Gender" },
                { new StringContent(request.CategoryId.ToString()), "CategoryId" },
                { new StringContent(request.BrandId.ToString()), "BrandId" }
            };

            var response = await client.PostAsync("product", form);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Thêm sản phẩm thất bại: " + ExtractMessage(error);
                await LoadDropdownsAsync();
                return View(request);
            }

            TempData["Success"] = "Thêm sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "MVCAuth");

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"product/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm: " + await response.Content.ReadAsStringAsync();
                return RedirectToAction("Index");
            }

            var content = await response.Content.ReadAsStringAsync();
            var product = JsonConvert.DeserializeObject<ProductDto>(content);

            var updateRequest = new UpdateProductRequest
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Gender = product.Gender,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId
            };

            await LoadDropdownsAsync();
            return View(updateRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateProductRequest request)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "MVCAuth");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại!";
                return View(request);
            }

            // 🔹 Validate các trường giống create
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                ModelState.AddModelError("", "Tên sản phẩm không được để trống.");
                await LoadDropdownsAsync();
                return View(request);
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 500)
            {
                ModelState.AddModelError("", "Mô tả không được vượt quá 500 ký tự.");
                await LoadDropdownsAsync();
                return View(request);
            }

            if (request.CategoryId == Guid.Empty)
            {
                ModelState.AddModelError("", "Chọn danh mục sản phẩm hợp lệ.");
                await LoadDropdownsAsync();
                return View(request);
            }

            if (request.BrandId == Guid.Empty)
            {
                ModelState.AddModelError("", "Chọn thương hiệu sản phẩm hợp lệ.");
                await LoadDropdownsAsync();
                return View(request);
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            var form = new MultipartFormDataContent
            {
                { new StringContent(request.Name ?? ""), "Name" },
                { new StringContent(request.Description ?? ""), "Description" },
                { new StringContent(((int)request.Gender).ToString()), "Gender" },
                { new StringContent(request.CategoryId.ToString()), "CategoryId" },
                { new StringContent(request.BrandId.ToString()), "BrandId" }
            };

            var response = await client.PutAsync($"product/{id}", form);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Cập nhật sản phẩm thất bại: " + ExtractMessage(error);
                await LoadDropdownsAsync();
                return View(request);
            }

            TempData["Success"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Import(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn file Excel để upload.");
                return View();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);

            // Đặt content type đúng cho file excel
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "File", file.FileName);

            // EntityName truyền vào API là "Product" (hoặc tên entity bạn muốn import)
            content.Add(new StringContent("Product"), "EntityName");

            // Nếu bạn muốn ignoreFields thì thêm, ví dụ:
            // content.Add(new StringContent("[\"Field1\",\"Field2\"]"), "ignoreFields");

            var response = await client.PostAsync("Import/upload", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Import Excel thành công.";
                return RedirectToAction("Index");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Import thất bại: {error}");
                return RedirectToAction("Index");
            }
        }
        public async Task<IActionResult> Export()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            // Gọi API export
            var response = await client.GetAsync("Import/export?entityName=Product");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Export thất bại: " + await response.Content.ReadAsStringAsync();
                return RedirectToAction("Index");
            }

            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = $"ProductExport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
