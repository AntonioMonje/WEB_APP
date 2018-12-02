using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.AspNetCore.Hosting;


// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CSUSM.Hackathon.Controllers
{

    public class CutenessController : Controller
    {
        private IConfiguration _configuration;
        private IHostingEnvironment _environment;

        public CutenessController(IConfiguration configuration, IHostingEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("[controller]/UploadImage")]
        public async Task<IActionResult> UploadImageAsync(IFormFile file)
        {
            // full path to file in temp location
            var filePath = _environment.WebRootPath + "/images/upload/" + file.FileName;

            if (file.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            var cuteness = await GetCutenessAsync(filePath);
            ViewData["Cuteness"] = cuteness;
            ViewData["Image"] = "/images/upload/" + file.FileName;
            return View("Result");
        }

        // GET: /<controller>/
        public async Task<double> GetCutenessAsync(string imageToRate)
        {
            var visionClient = new VisionServiceClient(_configuration["Vision"], "https://westcentralus.api.cognitive.microsoft.com/vision/v2.0");

            AnalysisResult analysisResult;
            var features = new VisualFeature[] { VisualFeature.Tags, VisualFeature.Description };

            using (var fs = new FileStream(imageToRate, FileMode.Open))
            {
                analysisResult = await visionClient.AnalyzeImageAsync(fs, features);
            }

            var cutenessScore = 0.0;

            var cuteTagResource = new List<KeyValuePair<string, int>> {
                new KeyValuePair<string, int>("small", 10),
                new KeyValuePair<string, int>("little",10),
                new KeyValuePair<string, int>("dog",10),
                new KeyValuePair<string, int>("cat",10),
                new KeyValuePair<string, int>("grass",10),
                new KeyValuePair<string, int>("outdoor",10),
                new KeyValuePair<string, int>("puppy",30),
                new KeyValuePair<string, int>("kitten",30),
                new KeyValuePair<string, int>("animal",20),
                new KeyValuePair<string, int>("cute",100),
                new KeyValuePair<string, int>("person",100)
            };

            var cuteness = analysisResult.Tags
                                     .Where(t => cuteTagResource
                                     .Any(r => r.Key == t.Name))
                                     .Select(c => c.Confidence * cuteTagResource.First(r => r.Key == c.Name).Value);

            foreach (var value in cuteness)
            {
                cutenessScore += value;
            }

            return cutenessScore;
        }
    }
}
