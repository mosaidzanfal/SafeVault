using Microsoft.AspNetCore.Mvc;
using SafeVault.Data;
using SafeVault.Models;

namespace SafeVault.Controllers
{
    public class UserController : Controller
    {
        private readonly SafeVault.Data.IUserRepository _userRepository;

        public UserController(SafeVault.Data.IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(UserInput model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ViewBag.SuccessMessage = "Input validated successfully.";
            return View(model);
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View(new SafeVault.Models.SearchViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(SafeVault.Models.SearchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Validation failed; do not call repository.
                return View(model);
            }

            bool exists = await _userRepository.UserExistsAsync(model.Username);

            ViewBag.Message = exists
                ? "User found."
                : "User not found.";

            return View(model);
        }
    }
}