using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EVI_App.Models;
using evi_app.Data;
using System.Net.Http;
using System.IO;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using evi_app.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;

namespace EVI_App.Controllers
{
    [Authorize]
    public class ValidationEngineController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly string _rootDirectory;

        public ValidationEngineController(ApplicationDbContext context, IHttpClientFactory clientFactory, UserManager<IdentityUser> userManager, IHostingEnvironment env)
        {
            _context = context;
            _clientFactory = clientFactory;
            _userManager = userManager;
            _rootDirectory = env.ContentRootPath;
        }

        // GET: ValidationEngine
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["TimestampSortParm"] = sortOrder == "Timestamp" ? "timestamp_desc" : "Timestamp";
            ViewData["CurrentFilter"] = searchString;

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            var messages = from m in _context.MessagesItems
                           select m;

            if (!String.IsNullOrEmpty(searchString))
            {
                messages = messages.Where(m => m.FiscalTaxId.Contains(searchString)
                                       || m.UserId.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "Timestamp":
                    messages = messages.OrderBy(m => m.Timestamp);
                    break;
                case "timestamp_desc":
                    messages = messages.OrderByDescending(m => m.Timestamp);
                    break;
                default:
                    messages = messages.OrderByDescending(m => m.Timestamp);
                    break;
            }

            int pageSize = 3;

            return View(await PaginatedList<Message>.CreateAsync(messages.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public IActionResult Service()
        {
            return View();
        }

        public IActionResult Pool(List<string>? valids, string error)
        {
            ViewBag.valids = valids;
            ViewBag.error = error;

            //Get all the uploaded certificates
            var user_id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var uploaded_files = _context.UploadedCertificates.Where(x => x.UserId == user_id).ToList();
            return View(uploaded_files);
        }

        public IActionResult UploadError(List<string> errors, List<string> valids)
        {
            ViewBag.errors = errors;
            ViewBag.valids = valids;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> UploadCertificate(List<IFormFile> certificates)
        {
            bool hasErrors = false;
            var errors = new List<string>();
            var valids = new List<string>();
            long size = certificates.Sum(f => f.Length);
            var basePath = Path.Combine(_rootDirectory + "\\wwwroot\\certificates\\");
            var filePaths = new List<string>();
            foreach (var cert in certificates)
            {
                if (cert.Length > 0)
                {
                    //Check the file name
                    string regex = @"^\d{10}(?:\d{2})?.(PEM|pem)$";
                    if (!Regex.IsMatch(cert.FileName, regex))
                    {
                        hasErrors = true;
                        errors.Add($"The file {cert.FileName} does not respect the format xxxxxxxxxx.pem");
                        continue;
                    }

                    var filePath = Path.Combine(basePath, cert.FileName);
                    filePaths.Add(filePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await cert.CopyToAsync(stream);
                    }

                    //Process the saved file
                    var user_id = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    //Send to TLS server
                    var endpoint = $"http://193.230.14.53:4000/api/upload";
                    var client = new HttpClient();
                    var content = new MultipartFormDataContent();
                    using (var ms = new MemoryStream())
                    {
                        cert.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        var fileContent = new ByteArrayContent(fileBytes);
                        content.Add(fileContent, "file", cert.FileName);
                    }
                    var result = client.PostAsync(endpoint, content);
                    if (result.Result.IsSuccessStatusCode)
                    {

                        var file_model = new UploadedCertificate
                        {
                            UserId = user_id,
                            FileName = cert.FileName,
                            Timestamp = DateTime.Now,
                            FilePath = filePath
                        };

                        //Check if was uploaded
                        var previous = _context.UploadedCertificates.Where(x => x.FileName == cert.FileName).FirstOrDefault();

                        if (previous != null)
                        {
                            _context.UploadedCertificates.Remove(previous);
                            await _context.SaveChangesAsync();
                        }
                        await _context.UploadedCertificates.AddAsync(file_model);
                        valids.Add($"The file {cert.FileName} was added to the pool");

                    }
                    else
                    {
                        hasErrors = true;
                        errors.Add($"An error occured when sending {cert.FileName} to the pool");
                        continue;
                    }
                }
            }
            if (hasErrors)
            {
                //We had errors with one of the files
                ViewBag.error = "We had errors with one of the files. Please make sure you have 10 digits and .pem";

                if(valids.Count > 0)
                {
                    //Send the refresh pool signal
                    var refresh_endpoint = $"http://193.230.14.53:9000/hooks/refresh-pool";
                    var refresh_client = new HttpClient();
                    var refresh_result = refresh_client.GetAsync(refresh_endpoint);
                    if (refresh_result.Result.IsSuccessStatusCode)
                    {
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        ViewBag.refresh_error = "There was a problem refreshing the pool in the server. The certificates were not uploaded.";
                    }
                }

                return RedirectToAction("UploadError", "ValidationEngine", new { errors = errors, valids = valids });
            }
            else
            {
                //Send the refresh pool signal
                var refresh_endpoint = $"http://193.230.14.53:9000/hooks/refresh-pool";
                var refresh_client = new HttpClient();
                var refresh_result = refresh_client.GetAsync(refresh_endpoint);
                if (refresh_result.Result.IsSuccessStatusCode)
                {
                    await _context.SaveChangesAsync();
                }
                else
                {
                    ViewBag.refresh_error = "There was a problem refreshing the pool in the server. The certificates were not uploaded.";
                    return RedirectToAction("UploadError", "ValidationEngine", new { errors = errors, valids = valids });
                }
                return RedirectToAction("Pool", "ValidationEngine", new { valids = valids });
            }
        }

        public async Task<ActionResult> RemoveCert(string file_name)
        {
            var current_user_id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var certificate = _context.UploadedCertificates.Where(x => x.UserId == current_user_id && x.FileName == file_name).FirstOrDefault();

            if (certificate == null)
            {
                return RedirectToAction("Pool", "ValidationEngine");
            }

            //Remove from Pool
            var endpoint = $"http://193.230.14.53:4000/api/upload/{certificate.FileName}";
            var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                //Send the refresh pool signal
                var refresh_endpoint = $"http://193.230.14.53:9000/hooks/refresh-pool";
                var refresh_client = new HttpClient();
                var refresh_result = refresh_client.GetAsync(refresh_endpoint);
                if (refresh_result.Result.IsSuccessStatusCode)
                {
                    //Remove from EVI
                    System.IO.File.Delete(certificate.FilePath);
                    _context.UploadedCertificates.Remove(certificate);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Pool", "ValidationEngine");
                }
                else
                {
                    return RedirectToAction("Pool", "ValidationEngine", new { error = "The pool could not be refreshed." });
                }
            }
            return RedirectToAction("Pool", "ValidationEngine", new { error = "The file could not be deleted" });
        }

        public IActionResult WrongInput(string error)
        {
            ViewBag.error = error;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> DownloadProfile0(string nui, string destAmef)
        {
            //Validate NUI on backend
            if(!Regex.IsMatch(nui, @"^\d{10}$"))
            {
                return RedirectToAction("WrongInput", "ValidationEngine", new { error = "The fiscal number must be a 10 digit number." });
            }

            var user_evi_id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user_corespondence_object = _context.UserCorespondence.Where(x => x.EviId == user_evi_id).FirstOrDefault();

            string user = user_corespondence_object.SavtaId;

            var endpoint = $"http://193.230.14.53:4000/api/request-profile?nui={nui}&nrminute=10&tipP=0&urlz=https://193.230.14.53:8443/XML&reset=0&data={DateTime.Now}&destAmef={destAmef}&user={user}";

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var file_endpoint = $"http://193.230.14.53:3001/profiles/Profile_{nui}.p7b";
                return Redirect(file_endpoint);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> DownloadProfile1(string nui, string nrminute, string destAmef)
        {
            //Validate NUI on backend
            if (!Regex.IsMatch(nui, @"^\d{10}$"))
            {
                return RedirectToAction("WrongInput", "ValidationEngine", new { error = "The fiscal number must be a 10 digit number." });
            }

            var user_evi_id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user_corespondence_object = _context.UserCorespondence.Where(x => x.EviId == user_evi_id).FirstOrDefault();

            string user = user_corespondence_object.SavtaId;

            var endpoint = $"http://193.230.14.53:4000/api/request-profile?nui={nui}&nrminute={nrminute}&tipP=1&urlz=https://193.230.14.53:8443/XML&reset=0&data={DateTime.Now}&destAmef={destAmef}&user={user}";

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var file_endpoint = $"http://193.230.14.53:3001/profiles/Profile_{nui}.p7b";
                return Redirect(file_endpoint);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> DownloadProfileR(string data)
        {
            //Validate data on backend
            if (data == null)
            {
                return RedirectToAction("WrongInput", "ValidationEngine", new { error = "The data must not be null" });
            }

            var user_evi_id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user_corespondence_object = _context.UserCorespondence.Where(x => x.EviId == user_evi_id).FirstOrDefault();

            string user = user_corespondence_object.SavtaId;

            var endpoint = $"http://193.230.14.53:4000/api/request-profile?nui=1234567890&nrminute=10&tipP=0&urlz=https://193.230.14.53:8443/XML&reset=1&data={data}&destAmef=1&user={user}";

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var file_endpoint = $"http://193.230.14.53:3001/profiles/mReset.p7b";
                return Redirect(file_endpoint);
            }

            return NotFound();
        }

        public IActionResult LiveFeed()
        {
            return View();
        }

        public IActionResult SendRetryTest(string fiscal_number, int nr_m)
        {
            bool wrong_fiscal_id = false;
            bool wrong_number = false;

            //Validate NUI on backend
            if (!Regex.IsMatch(fiscal_number, @"^\d{10}$"))
            {
                wrong_fiscal_id = true;
            }
            if(nr_m <= 0)
            {
                wrong_number = true;
            }

            if(wrong_fiscal_id || wrong_number)
            {
                if(wrong_fiscal_id && !wrong_number)
                    return RedirectToAction("WrongInput", "ValidationEngine", new { error = "The fiscal number must be a 10 digit number." });
                if (wrong_fiscal_id && wrong_number)
                    return RedirectToAction("WrongInput", "ValidationEngine", new { error = "The fiscal number must be a 10 digit number. The nrM value must be a positive integer" });
                if (!wrong_fiscal_id && wrong_number)
                    return RedirectToAction("WrongInput", "ValidationEngine", new { error = "The nrM value must be a positive integer" });
            }

            ViewBag.fiscal_number = fiscal_number;
            ViewBag.nr_m = nr_m;
            return View();
        }

        // GET: ValidationEngine/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.MessagesItems
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        private bool MessageExists(long id)
        {
            return _context.MessagesItems.Any(e => e.Id == id);
        }
    }
}
