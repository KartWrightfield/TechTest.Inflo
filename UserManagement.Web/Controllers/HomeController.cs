namespace UserManagement.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public ViewResult Index() => View();
}
