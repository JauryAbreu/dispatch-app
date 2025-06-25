namespace dispatch_app.Models.User
{
    public class UserGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ApplicationUser> Users { get; set; }
    }
}
