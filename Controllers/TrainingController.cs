using BelotWebApp.BelotClasses.Training;
using BelotWebApp.Models.Training;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BelotWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TrainingController : Controller
    {
        private readonly BelotGameSimulator _trainer;
        private readonly SimulationResult _result;

        public TrainingController(BelotGameSimulator trainer, SimulationResult result)
        {
            _trainer = trainer;
            _result = result;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new TrainingConfigViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(TrainingConfigViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _ = Task.Run(() => _trainer.SimulateGames(model.PopulationSize, model.NumGenerations));

            TempData["Message"] = "Training started!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetTrainingProgress()
        {
            return Json(_result); // automatically returns current training status as JSON
        }
    }
}
