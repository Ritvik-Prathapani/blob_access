using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using blob_access.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace blob_access.Controllers
{
    public class StudentController : Controller
    {
        private readonly string connectionString;
        private readonly string containerName;
        private readonly string blobFileName;

        public StudentController(IConfiguration configuration)
        {
            connectionString = configuration["AzureStorage:ConnectionString"];
            containerName = configuration["AzureStorage:ContainerName"];
            blobFileName = configuration["AzureStorage:BlobFileName"];
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Save(Student student)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            BlobClient blobClient = containerClient.GetBlobClient(blobFileName);

            List<Student> students;

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadAsync();
                using (var reader = new StreamReader(download.Value.Content))
                {
                    string content = await reader.ReadToEndAsync();
                    students = JsonSerializer.Deserialize<List<Student>>(content) ?? new List<Student>();
                }
            }
            else
            {
                students = new List<Student>();
            }

            students.Add(student);

            using (MemoryStream ms = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(ms, students);
                ms.Position = 0;
                await blobClient.UploadAsync(ms, overwrite: true);
            }

            ViewBag.Message = "Student data saved successfully!";
            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ViewStudents()
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobFileName);

            List<Student> students = new List<Student>();

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadAsync();
                using (var reader = new StreamReader(download.Value.Content))
                {
                    string content = await reader.ReadToEndAsync();
                    students = JsonSerializer.Deserialize<List<Student>>(content) ?? new List<Student>();
                }
            }

            return View(students);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await GetStudentById(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Student updatedStudent)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobFileName);

            List<Student> students = new List<Student>();

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadAsync();
                using (var reader = new StreamReader(download.Value.Content))
                {
                    string content = await reader.ReadToEndAsync();
                    students = JsonSerializer.Deserialize<List<Student>>(content) ?? new List<Student>();
                }
            }

            var studentIndex = students.FindIndex(s => s.RollNumber == updatedStudent.RollNumber);
            if (studentIndex != -1)
            {
                students[studentIndex] = updatedStudent;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(ms, students);
                ms.Position = 0;
                await blobClient.UploadAsync(ms, overwrite: true);
            }

            return RedirectToAction("ViewStudents");
        }

        private async Task<Student?> GetStudentById(int id)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobFileName);

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadAsync();
                using (var reader = new StreamReader(download.Value.Content))
                {
                    string content = await reader.ReadToEndAsync();
                    var students = JsonSerializer.Deserialize<List<Student>>(content) ?? new List<Student>();
                    return students.FirstOrDefault(s => s.RollNumber == id);
                }
            }
            return null;
        }
    }
}
