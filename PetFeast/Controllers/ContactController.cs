using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Contacts;

namespace PetFeast.Controllers
{
    public class ContactController : Controller
    {
        private readonly IContactRepository contactRepository;
        public ContactController(
            IContactRepository contactRepository)
        {
            this.contactRepository = contactRepository;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.CreatedAt = DateTime.Now;
                contact.IsRead = false;

                contactRepository.SendContact(contact);

                return RedirectToAction("ContactComplete");
            }
            return View(contact);
        }
        public IActionResult ContactComplete()
        {
            return View();
        }
    }
}