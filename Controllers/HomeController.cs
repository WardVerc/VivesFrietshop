using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Vives_FrietShop.Core;
using Vives_FrietShop.DataAccess;
using Vives_FrietShop.Models;
using Vives_FrietShop.ViewModels;

namespace Vives_FrietShop.Controllers
{
    public class HomeController : Controller
    {
        //Front end database
        private readonly IDatabase _database;
        
        //In-memory database
        private readonly DatabaseContext _databaseContext;

        public HomeController(IDatabase database, DatabaseContext databaseContext)
        {
            _database = database;
            _databaseContext = databaseContext;
        }
        
        public IActionResult Index()
        {
            ShopItemsWinkelmandjeViewModel model = new ShopItemsWinkelmandjeViewModel()
            {
                ShopItems = _databaseContext.ShopItems,
                Winkelmandje = _database.Winkelmandje
            };
            
            return View(model);
        }

        [HttpPost]
        public IActionResult Toevoegen(int id)
        {
            //zoek de gekozen shopItem in de in-memory db
            var databaseItem = _databaseContext.ShopItems.SingleOrDefault(a => a.Id == id);
            
            //voeg deze shopItem toe aan winkelmandje
            _database.Winkelmandje.Add(databaseItem);
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Verwijderen(int id)
        {
            //zoek de gekozen shopItem om te verwijderen uit winkelmandje
            //indien er meerdere dezelfde shopItem in winkelmandje
            //wordt enkel de eerst gevonden shopItem verwijderd
            var mandjeItem = _database.Winkelmandje.FirstOrDefault(a => a.Id == id);
            _database.Winkelmandje.RemoveAt(_database.Winkelmandje.IndexOf(mandjeItem));
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Betalen()
        {
            return View(_database.Winkelmandje);
        }

        [HttpPost]
        public IActionResult Einde()
        {
            //check eerst dat alle items in Winkelmand nog bestaan in ShopItems
            //zonder check krijg je later bij .SaveChanges() een SQL/DB-error:
            //indien items in winkelmand ligt
            //en item wordt verwijderd uit productbeheer
            //en dan wordt order geplaatst -> SQL/DB-error
            //want ShopItem bestaat niet meer

            foreach (var item in _database.Winkelmandje)
            {
                var databaseitem = _databaseContext.ShopItems.SingleOrDefault(a => a.Id == item.Id);
                if (databaseitem == null)
                {
                    Console.Write("Onbekend item in winkelmand!");
                    _database.Winkelmandje.Clear();
                    return RedirectToAction("Index");
                }
            }
            
            //Indien alle items winkelmand bestaan in In-memory database-ShopItems
            //wordt order gemaakt 
            Order order = new Order();
            _databaseContext.Add(order);
            _databaseContext.SaveChanges();

            //orderlines maken van winkelmand-items voor dit order
            foreach (var item in _database.Winkelmandje)
            {
                Orderline orderline = new Orderline()
                {
                    OrderId = order.Id,
                    ShopItemId = item.Id
                };

                _databaseContext.Orderlines.Add(orderline);
                _databaseContext.SaveChanges();
            }
            
            return View();
        }

        [HttpPost]
        public IActionResult LeegWinkelmandje()
        {
            _database.Winkelmandje.Clear();
            return RedirectToAction("Index");
        }
        
    }
}